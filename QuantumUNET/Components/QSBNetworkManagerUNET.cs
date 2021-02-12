using OWML.Logging;
using QuantumUNET.Messages;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace QuantumUNET.Components
{
	public class QSBNetworkManagerUNET : MonoBehaviour
	{
		public static QSBNetworkManagerUNET singleton;
		public static string networkSceneName = "";

		public int networkPort { get; set; } = 7777;
		public int simulatedLatency { get; set; } = 1;
		public bool serverBindToIP { get; set; }
		public bool dontDestroyOnLoad { get; set; } = true;
		public bool runInBackground { get; set; } = true;
		public bool scriptCRCCheck { get; set; } = true;
		public bool autoCreatePlayer { get; set; } = true;
		public bool isNetworkActive;
		public bool useWebSockets { get; set; }
		public bool useSimulator { get; set; }
		public bool clientLoadedScene { get; set; }
		public string serverBindAddress { get; set; } = "";
		public string networkAddress { get; set; } = "localhost";
		public string offlineScene { get; set; } = "";
		public string onlineScene { get; set; } = "";
		public float packetLossPercentage { get; set; }
		public float maxDelay { get; set; } = 0.01f;
		public GameObject playerPrefab { get; set; }
		public List<GameObject> spawnPrefabs { get; } = new List<GameObject>();
		public QSBNetworkClient client;
		public int maxConnections { get; set; } = 4;
		public List<QosType> channels { get; } = new List<QosType>();

		private ConnectionConfig m_ConnectionConfig;
		private GlobalConfig m_GlobalConfig;
		private readonly int m_MaxBufferedPackets = 16;
		private readonly bool m_AllowFragmentation = true;
		private static readonly QSBAddPlayerMessage s_AddPlayerMessage = new QSBAddPlayerMessage();
		private static readonly QSBRemovePlayerMessage s_RemovePlayerMessage = new QSBRemovePlayerMessage();
		private static readonly QSBErrorMessage s_ErrorMessage = new QSBErrorMessage();
		private static AsyncOperation s_LoadingSceneAsync;
		private static QSBNetworkConnection s_ClientReadyConnection;
		private static string s_Address;

		public bool customConfig { get; set; }

		public ConnectionConfig connectionConfig
		{
			get
			{
				if (m_ConnectionConfig == null)
				{
					m_ConnectionConfig = new ConnectionConfig();
				}
				return m_ConnectionConfig;
			}
		}

		public GlobalConfig globalConfig
		{
			get
			{
				if (m_GlobalConfig == null)
				{
					m_GlobalConfig = new GlobalConfig();
				}
				return m_GlobalConfig;
			}
		}

		public int numPlayers
		{
			get
			{
				var num = 0;
				foreach (var networkConnection in QSBNetworkServer.connections)
				{
					if (networkConnection != null)
					{
						foreach (var controller in networkConnection.PlayerControllers)
						{
							if (controller.IsValid)
							{
								num++;
							}
						}
					}
				}
				return num;
			}
		}

		public void Awake() => InitializeSingleton();

		private void InitializeSingleton()
		{
			if (!(singleton != null) || !(singleton == this))
			{
				if (dontDestroyOnLoad)
				{
					if (singleton != null)
					{
						Debug.Log("Multiple NetworkManagers detected in the scene. Only one NetworkManager can exist at a time. The duplicate NetworkManager will not be used.");
						Destroy(gameObject);
						return;
					}
					Debug.Log("NetworkManager created singleton (DontDestroyOnLoad)");
					singleton = this;
					if (Application.isPlaying)
					{
						DontDestroyOnLoad(gameObject);
					}
				}
				else
				{
					Debug.Log("NetworkManager created singleton (ForScene)");
					singleton = this;
				}
				if (networkAddress != "")
				{
					s_Address = networkAddress;
				}
				else if (s_Address != "")
				{
					networkAddress = s_Address;
				}
			}
		}

		internal void RegisterServerMessages()
		{
			QSBNetworkServer.RegisterHandler(QSBMsgType.Connect, OnServerConnectInternal);
			QSBNetworkServer.RegisterHandler(QSBMsgType.Disconnect, OnServerDisconnectInternal);
			QSBNetworkServer.RegisterHandler(QSBMsgType.Ready, OnServerReadyMessageInternal);
			QSBNetworkServer.RegisterHandler(QSBMsgType.AddPlayer, OnServerAddPlayerMessageInternal);
			QSBNetworkServer.RegisterHandler(QSBMsgType.RemovePlayer, OnServerRemovePlayerMessageInternal);
			QSBNetworkServer.RegisterHandler(QSBMsgType.Error, OnServerErrorInternal);
		}

		public bool StartServer() => StartServer(null, -1);

		private bool StartServer(ConnectionConfig config, int maxConnections)
		{
			InitializeSingleton();
			OnStartServer();
			if (runInBackground)
			{
				Application.runInBackground = true;
			}
			QSBNetworkCRC.scriptCRCCheck = scriptCRCCheck;
			QSBNetworkServer.useWebSockets = useWebSockets;
			if (m_GlobalConfig != null)
			{
				NetworkTransport.Init(m_GlobalConfig);
			}
			if (customConfig && m_ConnectionConfig != null && config == null)
			{
				m_ConnectionConfig.Channels.Clear();
				foreach (var channel in channels)
				{
					m_ConnectionConfig.AddChannel(channel);
				}
				QSBNetworkServer.Configure(m_ConnectionConfig, this.maxConnections);
			}
			if (config != null)
			{
				QSBNetworkServer.Configure(config, maxConnections);
			}
			if (serverBindToIP && !string.IsNullOrEmpty(serverBindAddress))
			{
				if (!QSBNetworkServer.Listen(serverBindAddress, networkPort))
				{
					Debug.LogError($"StartServer listen on {serverBindAddress} failed.");
					return false;
				}
			}
			else if (!QSBNetworkServer.Listen(networkPort))
			{
				Debug.LogError("StartServer listen failed.");
				return false;
			}
			RegisterServerMessages();
			Debug.Log($"NetworkManager StartServer port:{networkPort}");
			isNetworkActive = true;
			var name = SceneManager.GetSceneAt(0).name;
			if (!string.IsNullOrEmpty(onlineScene) && onlineScene != name && onlineScene != offlineScene)
			{
				ServerChangeScene(onlineScene);
			}
			else
			{
				QSBNetworkServer.SpawnObjects();
			}
			return true;
		}

		internal void RegisterClientMessages(QSBNetworkClient client)
		{
			client.RegisterHandler(QSBMsgType.Connect, OnClientConnectInternal);
			client.RegisterHandler(QSBMsgType.Disconnect, OnClientDisconnectInternal);
			client.RegisterHandler(QSBMsgType.NotReady, OnClientNotReadyMessageInternal);
			client.RegisterHandler(QSBMsgType.Error, OnClientErrorInternal);
			client.RegisterHandler(QSBMsgType.Scene, OnClientSceneInternal);
			if (playerPrefab != null)
			{
				QSBClientScene.RegisterPrefab(playerPrefab);
			}
			foreach (var gameObject in spawnPrefabs)
			{
				if (gameObject != null)
				{
					QSBClientScene.RegisterPrefab(gameObject);
				}
			}
		}

		public void UseExternalClient(QSBNetworkClient externalClient)
		{
			if (runInBackground)
			{
				Application.runInBackground = true;
			}
			if (externalClient != null)
			{
				client = externalClient;
				isNetworkActive = true;
				RegisterClientMessages(client);
				OnStartClient(client);
			}
			else
			{
				OnStopClient();
				QSBClientScene.DestroyAllClientObjects();
				QSBClientScene.HandleClientDisconnect(client.connection);
				client = null;
				if (!string.IsNullOrEmpty(offlineScene))
				{
					ClientChangeScene(offlineScene, false);
				}
			}
			s_Address = networkAddress;
		}

		public QSBNetworkClient StartClient(ConnectionConfig config, int hostPort)
		{
			InitializeSingleton();
			if (runInBackground)
			{
				Application.runInBackground = true;
			}
			isNetworkActive = true;
			if (m_GlobalConfig != null)
			{
				NetworkTransport.Init(m_GlobalConfig);
			}
			client = new QSBNetworkClient
			{
				hostPort = hostPort
			};
			if (config != null)
			{
				if (config.UsePlatformSpecificProtocols && Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PSP2)
				{
					throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
				}
				client.Configure(config, 1);
			}
			else if (customConfig && m_ConnectionConfig != null)
			{
				m_ConnectionConfig.Channels.Clear();
				foreach (var channel in channels)
				{
					m_ConnectionConfig.AddChannel(channel);
				}
				if (m_ConnectionConfig.UsePlatformSpecificProtocols && Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PSP2)
				{
					throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
				}
				client.Configure(m_ConnectionConfig, maxConnections);
			}
			RegisterClientMessages(client);
			if (string.IsNullOrEmpty(networkAddress))
			{
				ModConsole.OwmlConsole.WriteLine("Must set the Network Address field in the manager");
				return null;
			}
			if (useSimulator)
			{
				client.ConnectWithSimulator(networkAddress, networkPort, simulatedLatency, packetLossPercentage);
			}
			else
			{
				client.Connect(networkAddress, networkPort);
			}
			OnStartClient(client);
			s_Address = networkAddress;
			return client;
		}

		public QSBNetworkClient StartClient() => StartClient(null);

		public QSBNetworkClient StartClient(ConnectionConfig config) => StartClient(config, 0);

		public virtual QSBNetworkClient StartHost(ConnectionConfig config, int maxConnections)
		{
			OnStartHost();
			QSBNetworkClient result;
			if (StartServer(config, maxConnections))
			{
				var networkClient = ConnectLocalClient();
				OnServerConnect(networkClient.connection);
				OnStartClient(networkClient);
				result = networkClient;
			}
			else
			{
				result = null;
			}
			return result;
		}

		public virtual QSBNetworkClient StartHost()
		{
			OnStartHost();
			QSBNetworkClient result;
			if (StartServer())
			{
				var networkClient = ConnectLocalClient();
				OnStartClient(networkClient);
				result = networkClient;
			}
			else
			{
				result = null;
			}
			return result;
		}

		private QSBNetworkClient ConnectLocalClient()
		{
			Debug.Log($"NetworkManager StartHost port:{networkPort}");
			networkAddress = "localhost";
			client = QSBClientScene.ConnectLocalServer();
			RegisterClientMessages(client);
			return client;
		}

		public void StopHost()
		{
			OnStopHost();
			StopServer();
			StopClient();
		}

		public void StopServer()
		{
			if (QSBNetworkServer.active)
			{
				OnStopServer();
				Debug.Log("NetworkManager StopServer");
				isNetworkActive = false;
				QSBNetworkServer.Shutdown();
				if (!string.IsNullOrEmpty(offlineScene))
				{
					ServerChangeScene(offlineScene);
				}
				CleanupNetworkIdentities();
			}
		}

		public void StopClient()
		{
			OnStopClient();
			Debug.Log("NetworkManager StopClient");
			isNetworkActive = false;
			if (client != null)
			{
				client.Disconnect();
				client.Shutdown();
				client = null;
			}
			QSBClientScene.DestroyAllClientObjects();
			if (!string.IsNullOrEmpty(offlineScene))
			{
				ClientChangeScene(offlineScene, false);
			}
			CleanupNetworkIdentities();
		}

		public virtual void ServerChangeScene(string newSceneName)
		{
			if (string.IsNullOrEmpty(newSceneName))
			{
				Debug.LogError("ServerChangeScene empty scene name");
			}
			else
			{
				Debug.Log($"ServerChangeScene {newSceneName}");
				QSBNetworkServer.SetAllClientsNotReady();
				networkSceneName = newSceneName;
				s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
				var msg = new QSBStringMessage(networkSceneName);
				QSBNetworkServer.SendToAll(39, msg);
			}
		}

		private void CleanupNetworkIdentities()
		{
			foreach (var networkIdentity in Resources.FindObjectsOfTypeAll<QSBNetworkIdentity>())
			{
				networkIdentity.MarkForReset();
			}
		}

		internal void ClientChangeScene(string newSceneName, bool forceReload)
		{
			if (string.IsNullOrEmpty(newSceneName))
			{
				Debug.LogError("ClientChangeScene empty scene name");
			}
			else
			{
				Debug.Log($"ClientChangeScene newSceneName:{newSceneName} networkSceneName:{networkSceneName}");
				if (newSceneName == networkSceneName)
				{
					if (!forceReload)
					{
						FinishLoadScene();
						return;
					}
				}
				s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
				networkSceneName = newSceneName;
			}
		}

		private void FinishLoadScene()
		{
			if (client != null)
			{
				if (s_ClientReadyConnection != null)
				{
					clientLoadedScene = true;
					OnClientConnect(s_ClientReadyConnection);
					s_ClientReadyConnection = null;
				}
			}
			else
			{
				Debug.Log("FinishLoadScene client is null");
			}
			if (QSBNetworkServer.active)
			{
				QSBNetworkServer.SpawnObjects();
				OnServerSceneChanged(networkSceneName);
			}
			if (IsClientConnected() && client != null)
			{
				RegisterClientMessages(client);
				OnClientSceneChanged(client.connection);
			}
		}

		internal static void UpdateScene()
		{
			if (!(singleton == null))
			{
				if (s_LoadingSceneAsync != null)
				{
					if (s_LoadingSceneAsync.isDone)
					{
						ModConsole.OwmlConsole.WriteLine($"ClientChangeScene done readyCon:{s_ClientReadyConnection}");
						singleton.FinishLoadScene();
						s_LoadingSceneAsync.allowSceneActivation = true;
						s_LoadingSceneAsync = null;
					}
				}
			}
		}

		public bool IsClientConnected() => client != null && client.isConnected;

		public static void Shutdown()
		{
			if (!(singleton == null))
			{
				s_ClientReadyConnection = null;
				singleton.StopHost();
				singleton = null;
			}
		}

		internal void OnServerConnectInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnServerConnectInternal");
			netMsg.Connection.SetMaxDelay(maxDelay);
			if (m_MaxBufferedPackets != 512)
			{
				for (var i = 0; i < QSBNetworkServer.numChannels; i++)
				{
					netMsg.Connection.SetChannelOption(i, ChannelOption.MaxPendingBuffers, m_MaxBufferedPackets);
				}
			}
			if (!m_AllowFragmentation)
			{
				for (var j = 0; j < QSBNetworkServer.numChannels; j++)
				{
					netMsg.Connection.SetChannelOption(j, ChannelOption.AllowFragmentation, 0);
				}
			}
			if (networkSceneName != "" && networkSceneName != offlineScene)
			{
				var msg = new QSBStringMessage(networkSceneName);
				netMsg.Connection.Send(39, msg);
			}
			OnServerConnect(netMsg.Connection);
		}

		internal void OnServerDisconnectInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnServerDisconnectInternal");
			OnServerDisconnect(netMsg.Connection);
		}

		internal void OnServerReadyMessageInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnServerReadyMessageInternal");
			OnServerReady(netMsg.Connection);
		}

		internal void OnServerAddPlayerMessageInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnServerAddPlayerMessageInternal");
			netMsg.ReadMessage(s_AddPlayerMessage);
			if (s_AddPlayerMessage.msgSize != 0)
			{
				var extraMessageReader = new NetworkReader(s_AddPlayerMessage.msgData);
				OnServerAddPlayer(netMsg.Connection, s_AddPlayerMessage.playerControllerId, extraMessageReader);
			}
			else
			{
				OnServerAddPlayer(netMsg.Connection, s_AddPlayerMessage.playerControllerId);
			}
		}

		internal void OnServerRemovePlayerMessageInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnServerRemovePlayerMessageInternal");
			netMsg.ReadMessage(s_RemovePlayerMessage);
			netMsg.Connection.GetPlayerController(s_RemovePlayerMessage.PlayerControllerId, out var player);
			OnServerRemovePlayer(netMsg.Connection, player);
			netMsg.Connection.RemovePlayerController(s_RemovePlayerMessage.PlayerControllerId);
		}

		internal void OnServerErrorInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnServerErrorInternal");
			netMsg.ReadMessage(s_ErrorMessage);
			OnServerError(netMsg.Connection, s_ErrorMessage.errorCode);
		}

		internal void OnClientConnectInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnClientConnectInternal");
			netMsg.Connection.SetMaxDelay(maxDelay);
			var name = SceneManager.GetSceneAt(0).name;
			if (string.IsNullOrEmpty(onlineScene) || onlineScene == offlineScene || name == onlineScene)
			{
				clientLoadedScene = false;
				OnClientConnect(netMsg.Connection);
			}
			else
			{
				s_ClientReadyConnection = netMsg.Connection;
			}
		}

		internal void OnClientDisconnectInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnClientDisconnectInternal");
			if (!string.IsNullOrEmpty(offlineScene))
			{
				ClientChangeScene(offlineScene, false);
			}
			OnClientDisconnect(netMsg.Connection);
		}

		internal void OnClientNotReadyMessageInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnClientNotReadyMessageInternal");
			QSBClientScene.SetNotReady();
			OnClientNotReady(netMsg.Connection);
		}

		internal void OnClientErrorInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnClientErrorInternal");
			netMsg.ReadMessage(s_ErrorMessage);
			OnClientError(netMsg.Connection, s_ErrorMessage.errorCode);
		}

		internal void OnClientSceneInternal(QSBNetworkMessage netMsg)
		{
			Debug.Log("NetworkManager:OnClientSceneInternal");
			var newSceneName = netMsg.Reader.ReadString();
			if (IsClientConnected() && !QSBNetworkServer.active)
			{
				ClientChangeScene(newSceneName, true);
			}
		}

		public virtual void OnServerConnect(QSBNetworkConnection conn)
		{
		}

		public virtual void OnServerDisconnect(QSBNetworkConnection conn)
		{
			QSBNetworkServer.DestroyPlayersForConnection(conn);
			if (conn.LastError != NetworkError.Ok)
			{
				Debug.LogError($"ServerDisconnected due to error: {conn.LastError}");
			}
		}

		public virtual void OnServerReady(QSBNetworkConnection conn)
		{
			if (conn.PlayerControllers.Count == 0)
			{
				Debug.Log("Ready with no player object");
			}
			QSBNetworkServer.SetClientReady(conn);
		}

		public virtual void OnServerAddPlayer(QSBNetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader) => OnServerAddPlayerInternal(conn, playerControllerId);

		public virtual void OnServerAddPlayer(QSBNetworkConnection conn, short playerControllerId) => OnServerAddPlayerInternal(conn, playerControllerId);

		private void OnServerAddPlayerInternal(QSBNetworkConnection conn, short playerControllerId)
		{
			if (playerPrefab == null)
			{
				ModConsole.OwmlConsole.WriteLine("Error - The PlayerPrefab is empty on the QSBNetworkManager. Please setup a PlayerPrefab object.");
			}
			else if (playerPrefab.GetComponent<QSBNetworkIdentity>() == null)
			{
				ModConsole.OwmlConsole.WriteLine("Error - The PlayerPrefab does not have a QSBNetworkIdentity. Please add a QSBNetworkIdentity to the player prefab.");
			}
			else if (playerControllerId < conn.PlayerControllers.Count && conn.PlayerControllers[playerControllerId].IsValid && conn.PlayerControllers[playerControllerId].Gameobject != null)
			{
				ModConsole.OwmlConsole.WriteLine("Warning - There is already a player at that playerControllerId for this connections.");
			}
			else
			{
				var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
				QSBNetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
			}
		}

		public virtual void OnServerRemovePlayer(QSBNetworkConnection conn, QSBPlayerController player)
		{
			if (player.Gameobject != null)
			{
				QSBNetworkServer.Destroy(player.Gameobject);
			}
		}

		public virtual void OnServerError(QSBNetworkConnection conn, int errorCode)
		{
		}

		public virtual void OnServerSceneChanged(string sceneName)
		{
		}

		public virtual void OnClientConnect(QSBNetworkConnection conn)
		{
			if (!clientLoadedScene)
			{
				QSBClientScene.Ready(conn);
				if (autoCreatePlayer)
				{
					QSBClientScene.AddPlayer(0);
				}
			}
		}

		public virtual void OnClientDisconnect(QSBNetworkConnection conn)
		{
			StopClient();
			if (conn.LastError != NetworkError.Ok)
			{
				Debug.LogError($"ClientDisconnected due to error: {conn.LastError}");
			}
		}

		public virtual void OnClientError(QSBNetworkConnection conn, int errorCode)
		{
		}

		public virtual void OnClientNotReady(QSBNetworkConnection conn)
		{
		}

		public virtual void OnClientSceneChanged(QSBNetworkConnection conn)
		{
			QSBClientScene.Ready(conn);
			if (autoCreatePlayer)
			{
				var flag = QSBClientScene.localPlayers.Count == 0;
				var flag2 = false;
				foreach (var player in QSBClientScene.localPlayers)
				{
					if (player.Gameobject != null)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					flag = true;
				}
				if (flag)
				{
					QSBClientScene.AddPlayer(0);
				}
			}
		}

		public virtual void OnStartHost()
		{
		}

		public virtual void OnStartServer()
		{
		}

		public virtual void OnStartClient(QSBNetworkClient client)
		{
		}

		public virtual void OnStopServer()
		{
		}

		public virtual void OnStopClient()
		{
		}

		public virtual void OnStopHost()
		{
		}
	}
}