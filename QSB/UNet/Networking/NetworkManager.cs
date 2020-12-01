using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;

namespace QSB.UNet.Networking
{
	public class NetworkManager : MonoBehaviour
	{
		public int networkPort
		{
			get
			{
				return m_NetworkPort;
			}
			set
			{
				m_NetworkPort = value;
			}
		}

		public bool serverBindToIP
		{
			get
			{
				return m_ServerBindToIP;
			}
			set
			{
				m_ServerBindToIP = value;
			}
		}

		public string serverBindAddress
		{
			get
			{
				return m_ServerBindAddress;
			}
			set
			{
				m_ServerBindAddress = value;
			}
		}

		public string networkAddress
		{
			get
			{
				return m_NetworkAddress;
			}
			set
			{
				m_NetworkAddress = value;
			}
		}

		public bool dontDestroyOnLoad
		{
			get
			{
				return m_DontDestroyOnLoad;
			}
			set
			{
				m_DontDestroyOnLoad = value;
			}
		}

		public bool runInBackground
		{
			get
			{
				return m_RunInBackground;
			}
			set
			{
				m_RunInBackground = value;
			}
		}

		public bool scriptCRCCheck
		{
			get
			{
				return m_ScriptCRCCheck;
			}
			set
			{
				m_ScriptCRCCheck = value;
			}
		}

		[Obsolete("moved to NetworkMigrationManager")]
		public bool sendPeerInfo
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public float maxDelay
		{
			get
			{
				return m_MaxDelay;
			}
			set
			{
				m_MaxDelay = value;
			}
		}

		public LogFilter.FilterLevel logLevel
		{
			get
			{
				return m_LogLevel;
			}
			set
			{
				m_LogLevel = value;
				LogFilter.currentLogLevel = (int)value;
			}
		}

		public GameObject playerPrefab
		{
			get
			{
				return m_PlayerPrefab;
			}
			set
			{
				m_PlayerPrefab = value;
			}
		}

		public bool autoCreatePlayer
		{
			get
			{
				return m_AutoCreatePlayer;
			}
			set
			{
				m_AutoCreatePlayer = value;
			}
		}

		public PlayerSpawnMethod playerSpawnMethod
		{
			get
			{
				return m_PlayerSpawnMethod;
			}
			set
			{
				m_PlayerSpawnMethod = value;
			}
		}

		public string offlineScene
		{
			get
			{
				return m_OfflineScene;
			}
			set
			{
				m_OfflineScene = value;
			}
		}

		public string onlineScene
		{
			get
			{
				return m_OnlineScene;
			}
			set
			{
				m_OnlineScene = value;
			}
		}

		public List<GameObject> spawnPrefabs
		{
			get
			{
				return m_SpawnPrefabs;
			}
		}

		public List<Transform> startPositions
		{
			get
			{
				return NetworkManager.s_StartPositions;
			}
		}

		public bool customConfig
		{
			get
			{
				return m_CustomConfig;
			}
			set
			{
				m_CustomConfig = value;
			}
		}

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

		public int maxConnections
		{
			get
			{
				return m_MaxConnections;
			}
			set
			{
				m_MaxConnections = value;
			}
		}

		public List<QosType> channels
		{
			get
			{
				return m_Channels;
			}
		}

		public EndPoint secureTunnelEndpoint
		{
			get
			{
				return m_EndPoint;
			}
			set
			{
				m_EndPoint = value;
			}
		}

		public bool useWebSockets
		{
			get
			{
				return m_UseWebSockets;
			}
			set
			{
				m_UseWebSockets = value;
			}
		}

		public bool useSimulator
		{
			get
			{
				return m_UseSimulator;
			}
			set
			{
				m_UseSimulator = value;
			}
		}

		public int simulatedLatency
		{
			get
			{
				return m_SimulatedLatency;
			}
			set
			{
				m_SimulatedLatency = value;
			}
		}

		public float packetLossPercentage
		{
			get
			{
				return m_PacketLossPercentage;
			}
			set
			{
				m_PacketLossPercentage = value;
			}
		}

		public string matchHost
		{
			get
			{
				return m_MatchHost;
			}
			set
			{
				m_MatchHost = value;
			}
		}

		public int matchPort
		{
			get
			{
				return m_MatchPort;
			}
			set
			{
				m_MatchPort = value;
			}
		}

		public bool clientLoadedScene
		{
			get
			{
				return m_ClientLoadedScene;
			}
			set
			{
				m_ClientLoadedScene = value;
			}
		}

		public int numPlayers
		{
			get
			{
				int num = 0;
				for (int i = 0; i < NetworkServer.connections.Count; i++)
				{
					NetworkConnection networkConnection = NetworkServer.connections[i];
					if (networkConnection != null)
					{
						for (int j = 0; j < networkConnection.playerControllers.Count; j++)
						{
							if (networkConnection.playerControllers[j].IsValid)
							{
								num++;
							}
						}
					}
				}
				return num;
			}
		}

		private void Awake()
		{
			InitializeSingleton();
		}

		private void InitializeSingleton()
		{
			if (!(NetworkManager.singleton != null) || !(NetworkManager.singleton == this))
			{
				int logLevel = (int)m_LogLevel;
				if (logLevel != -1)
				{
					LogFilter.currentLogLevel = logLevel;
				}
				if (m_DontDestroyOnLoad)
				{
					if (NetworkManager.singleton != null)
					{
						if (LogFilter.logDev)
						{
							Debug.Log("Multiple NetworkManagers detected in the scene. Only one NetworkManager can exist at a time. The duplicate NetworkManager will not be used.");
						}
						Destroy(base.gameObject);
						return;
					}
					if (LogFilter.logDev)
					{
						Debug.Log("NetworkManager created singleton (DontDestroyOnLoad)");
					}
					NetworkManager.singleton = this;
					if (Application.isPlaying)
					{
						DontDestroyOnLoad(base.gameObject);
					}
				}
				else
				{
					if (LogFilter.logDev)
					{
						Debug.Log("NetworkManager created singleton (ForScene)");
					}
					NetworkManager.singleton = this;
				}
				if (m_NetworkAddress != "")
				{
					NetworkManager.s_Address = m_NetworkAddress;
				}
				else if (NetworkManager.s_Address != "")
				{
					m_NetworkAddress = NetworkManager.s_Address;
				}
			}
		}

		private void OnValidate()
		{
			if (m_SimulatedLatency < 1)
			{
				m_SimulatedLatency = 1;
			}
			if (m_SimulatedLatency > 500)
			{
				m_SimulatedLatency = 500;
			}
			if (m_PacketLossPercentage < 0f)
			{
				m_PacketLossPercentage = 0f;
			}
			if (m_PacketLossPercentage > 99f)
			{
				m_PacketLossPercentage = 99f;
			}
			if (m_MaxConnections <= 0)
			{
				m_MaxConnections = 1;
			}
			if (m_MaxConnections > 32000)
			{
				m_MaxConnections = 32000;
			}
			if (m_MaxBufferedPackets <= 0)
			{
				m_MaxBufferedPackets = 0;
			}
			if (m_MaxBufferedPackets > 512)
			{
				m_MaxBufferedPackets = 512;
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkManager - MaxBufferedPackets cannot be more than " + 512);
				}
			}
			if (m_PlayerPrefab != null && m_PlayerPrefab.GetComponent<NetworkIdentity>() == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkManager - playerPrefab must have a NetworkIdentity.");
				}
				m_PlayerPrefab = null;
			}
			if (m_ConnectionConfig != null && m_ConnectionConfig.MinUpdateTimeout <= 0U)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkManager MinUpdateTimeout cannot be zero or less. The value will be reset to 1 millisecond");
				}
				m_ConnectionConfig.MinUpdateTimeout = 1U;
			}
			if (m_GlobalConfig != null)
			{
				if (m_GlobalConfig.ThreadAwakeTimeout <= 0U)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkManager ThreadAwakeTimeout cannot be zero or less. The value will be reset to 1 millisecond");
					}
					m_GlobalConfig.ThreadAwakeTimeout = 1U;
				}
			}
		}

		internal void RegisterServerMessages()
		{
			NetworkServer.RegisterHandler(32, new NetworkMessageDelegate(OnServerConnectInternal));
			NetworkServer.RegisterHandler(33, new NetworkMessageDelegate(OnServerDisconnectInternal));
			NetworkServer.RegisterHandler(35, new NetworkMessageDelegate(OnServerReadyMessageInternal));
			NetworkServer.RegisterHandler(37, new NetworkMessageDelegate(OnServerAddPlayerMessageInternal));
			NetworkServer.RegisterHandler(38, new NetworkMessageDelegate(OnServerRemovePlayerMessageInternal));
			NetworkServer.RegisterHandler(34, new NetworkMessageDelegate(OnServerErrorInternal));
		}

		public bool StartServer()
		{
			return StartServer(null, -1);
		}

		private bool StartServer(ConnectionConfig config, int maxConnections)
		{
			InitializeSingleton();
			OnStartServer();
			if (m_RunInBackground)
			{
				Application.runInBackground = true;
			}
			NetworkCRC.scriptCRCCheck = scriptCRCCheck;
			NetworkServer.useWebSockets = m_UseWebSockets;
			if (m_GlobalConfig != null)
			{
				NetworkTransport.Init(m_GlobalConfig);
			}
			if (m_CustomConfig && m_ConnectionConfig != null && config == null)
			{
				m_ConnectionConfig.Channels.Clear();
				for (int i = 0; i < m_Channels.Count; i++)
				{
					m_ConnectionConfig.AddChannel(m_Channels[i]);
				}
				NetworkServer.Configure(m_ConnectionConfig, m_MaxConnections);
			}
			if (config != null)
			{
				NetworkServer.Configure(config, maxConnections);
			}
			else if (m_ServerBindToIP && !string.IsNullOrEmpty(m_ServerBindAddress))
			{
				if (!NetworkServer.Listen(m_ServerBindAddress, m_NetworkPort))
				{
					if (LogFilter.logError)
					{
						Debug.LogError("StartServer listen on " + m_ServerBindAddress + " failed.");
					}
					return false;
				}
			}
			else if (!NetworkServer.Listen(m_NetworkPort))
			{
				if (LogFilter.logError)
				{
					Debug.LogError("StartServer listen failed.");
				}
				return false;
			}
			RegisterServerMessages();
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager StartServer port:" + m_NetworkPort);
			}
			isNetworkActive = true;
			string name = SceneManager.GetSceneAt(0).name;
			if (!string.IsNullOrEmpty(m_OnlineScene) && m_OnlineScene != name && m_OnlineScene != m_OfflineScene)
			{
				ServerChangeScene(m_OnlineScene);
			}
			else
			{
				NetworkServer.SpawnObjects();
			}
			return true;
		}

		internal void RegisterClientMessages(NetworkClient client)
		{
			client.RegisterHandler(32, new NetworkMessageDelegate(OnClientConnectInternal));
			client.RegisterHandler(33, new NetworkMessageDelegate(OnClientDisconnectInternal));
			client.RegisterHandler(36, new NetworkMessageDelegate(OnClientNotReadyMessageInternal));
			client.RegisterHandler(34, new NetworkMessageDelegate(OnClientErrorInternal));
			client.RegisterHandler(39, new NetworkMessageDelegate(OnClientSceneInternal));
			if (m_PlayerPrefab != null)
			{
				ClientScene.RegisterPrefab(m_PlayerPrefab);
			}
			for (int i = 0; i < m_SpawnPrefabs.Count; i++)
			{
				GameObject gameObject = m_SpawnPrefabs[i];
				if (gameObject != null)
				{
					ClientScene.RegisterPrefab(gameObject);
				}
			}
		}

		public void UseExternalClient(NetworkClient externalClient)
		{
			if (m_RunInBackground)
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
				ClientScene.DestroyAllClientObjects();
				ClientScene.HandleClientDisconnect(client.connection);
				client = null;
				if (!string.IsNullOrEmpty(m_OfflineScene))
				{
					ClientChangeScene(m_OfflineScene, false);
				}
			}
			NetworkManager.s_Address = m_NetworkAddress;
		}

		public NetworkClient StartClient(ConnectionConfig config, int hostPort)
		{
			InitializeSingleton();
			if (m_RunInBackground)
			{
				Application.runInBackground = true;
			}
			isNetworkActive = true;
			if (m_GlobalConfig != null)
			{
				NetworkTransport.Init(m_GlobalConfig);
			}
			client = new NetworkClient();
			client.hostPort = hostPort;
			if (config != null)
			{
				if (config.UsePlatformSpecificProtocols && Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PSP2)
				{
					throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
				}
				client.Configure(config, 1);
			}
			else if (m_CustomConfig && m_ConnectionConfig != null)
			{
				m_ConnectionConfig.Channels.Clear();
				for (int i = 0; i < m_Channels.Count; i++)
				{
					m_ConnectionConfig.AddChannel(m_Channels[i]);
				}
				if (m_ConnectionConfig.UsePlatformSpecificProtocols && Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PSP2)
				{
					throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
				}
				client.Configure(m_ConnectionConfig, m_MaxConnections);
			}
			RegisterClientMessages(client);
			if (m_EndPoint != null)
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("NetworkManager StartClient using provided SecureTunnel");
				}
				client.Connect(m_EndPoint);
			}
			else
			{
				if (string.IsNullOrEmpty(m_NetworkAddress))
				{
					if (LogFilter.logError)
					{
						Debug.LogError("Must set the Network Address field in the manager");
					}
					return null;
				}
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"NetworkManager StartClient address:",
						m_NetworkAddress,
						" port:",
						m_NetworkPort
					}));
				}
				if (m_UseSimulator)
				{
					client.ConnectWithSimulator(m_NetworkAddress, m_NetworkPort, m_SimulatedLatency, m_PacketLossPercentage);
				}
				else
				{
					client.Connect(m_NetworkAddress, m_NetworkPort);
				}
			}
			OnStartClient(client);
			NetworkManager.s_Address = m_NetworkAddress;
			return client;
		}

		public NetworkClient StartClient()
		{
			return StartClient(null);
		}

		public NetworkClient StartClient(ConnectionConfig config)
		{
			return StartClient(config, 0);
		}

		public virtual NetworkClient StartHost(ConnectionConfig config, int maxConnections)
		{
			OnStartHost();
			NetworkClient result;
			if (StartServer(config, maxConnections))
			{
				NetworkClient networkClient = ConnectLocalClient();
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

		public virtual NetworkClient StartHost()
		{
			OnStartHost();
			NetworkClient result;
			if (StartServer())
			{
				NetworkClient networkClient = ConnectLocalClient();
				OnStartClient(networkClient);
				result = networkClient;
			}
			else
			{
				result = null;
			}
			return result;
		}

		private NetworkClient ConnectLocalClient()
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager StartHost port:" + m_NetworkPort);
			}
			m_NetworkAddress = "localhost";
			client = ClientScene.ConnectLocalServer();
			RegisterClientMessages(client);
			return client;
		}

		public void StopHost()
		{
			bool active = NetworkServer.active;
			OnStopHost();
			StopServer();
			StopClient();
		}

		public void StopServer()
		{
			if (NetworkServer.active)
			{
				OnStopServer();
				if (LogFilter.logDebug)
				{
					Debug.Log("NetworkManager StopServer");
				}
				isNetworkActive = false;
				NetworkServer.Shutdown();
				if (!string.IsNullOrEmpty(m_OfflineScene))
				{
					ServerChangeScene(m_OfflineScene);
				}
				CleanupNetworkIdentities();
			}
		}

		public void StopClient()
		{
			OnStopClient();
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager StopClient");
			}
			isNetworkActive = false;
			if (client != null)
			{
				client.Disconnect();
				client.Shutdown();
				client = null;
			}
			ClientScene.DestroyAllClientObjects();
			if (!string.IsNullOrEmpty(m_OfflineScene))
			{
				ClientChangeScene(m_OfflineScene, false);
			}
			CleanupNetworkIdentities();
		}

		public virtual void ServerChangeScene(string newSceneName)
		{
			if (string.IsNullOrEmpty(newSceneName))
			{
				if (LogFilter.logError)
				{
					Debug.LogError("ServerChangeScene empty scene name");
				}
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("ServerChangeScene " + newSceneName);
				}
				NetworkServer.SetAllClientsNotReady();
				NetworkManager.networkSceneName = newSceneName;
				NetworkManager.s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
				StringMessage msg = new StringMessage(NetworkManager.networkSceneName);
				NetworkServer.SendToAll(39, msg);
				NetworkManager.s_StartPositionIndex = 0;
				NetworkManager.s_StartPositions.Clear();
			}
		}

		private void CleanupNetworkIdentities()
		{
			foreach (NetworkIdentity networkIdentity in Resources.FindObjectsOfTypeAll<NetworkIdentity>())
			{
				networkIdentity.MarkForReset();
			}
		}

		internal void ClientChangeScene(string newSceneName, bool forceReload)
		{
			if (string.IsNullOrEmpty(newSceneName))
			{
				if (LogFilter.logError)
				{
					Debug.LogError("ClientChangeScene empty scene name");
				}
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("ClientChangeScene newSceneName:" + newSceneName + " networkSceneName:" + NetworkManager.networkSceneName);
				}
				if (newSceneName == NetworkManager.networkSceneName)
				{
					if (!forceReload)
					{
						FinishLoadScene();
						return;
					}
				}
				NetworkManager.s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
				NetworkManager.networkSceneName = newSceneName;
			}
		}

		private void FinishLoadScene()
		{
			if (client != null)
			{
				if (NetworkManager.s_ClientReadyConnection != null)
				{
					m_ClientLoadedScene = true;
					OnClientConnect(NetworkManager.s_ClientReadyConnection);
					NetworkManager.s_ClientReadyConnection = null;
				}
			}
			else if (LogFilter.logDev)
			{
				Debug.Log("FinishLoadScene client is null");
			}
			if (NetworkServer.active)
			{
				NetworkServer.SpawnObjects();
				OnServerSceneChanged(NetworkManager.networkSceneName);
			}
			if (IsClientConnected() && client != null)
			{
				RegisterClientMessages(client);
				OnClientSceneChanged(client.connection);
			}
		}

		internal static void UpdateScene()
		{
			if (!(NetworkManager.singleton == null))
			{
				if (NetworkManager.s_LoadingSceneAsync != null)
				{
					if (NetworkManager.s_LoadingSceneAsync.isDone)
					{
						if (LogFilter.logDebug)
						{
							Debug.Log("ClientChangeScene done readyCon:" + NetworkManager.s_ClientReadyConnection);
						}
						NetworkManager.singleton.FinishLoadScene();
						NetworkManager.s_LoadingSceneAsync.allowSceneActivation = true;
						NetworkManager.s_LoadingSceneAsync = null;
					}
				}
			}
		}

		private void OnDestroy()
		{
			if (LogFilter.logDev)
			{
				Debug.Log("NetworkManager destroyed");
			}
		}

		public static void RegisterStartPosition(Transform start)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"RegisterStartPosition: (",
					start.gameObject.name,
					") ",
					start.position
				}));
			}
			NetworkManager.s_StartPositions.Add(start);
		}

		public static void UnRegisterStartPosition(Transform start)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"UnRegisterStartPosition: (",
					start.gameObject.name,
					") ",
					start.position
				}));
			}
			NetworkManager.s_StartPositions.Remove(start);
		}

		public bool IsClientConnected()
		{
			return client != null && client.isConnected;
		}

		public static void Shutdown()
		{
			if (!(NetworkManager.singleton == null))
			{
				NetworkManager.s_StartPositions.Clear();
				NetworkManager.s_StartPositionIndex = 0;
				NetworkManager.s_ClientReadyConnection = null;
				NetworkManager.singleton.StopHost();
				NetworkManager.singleton = null;
			}
		}

		internal void OnServerConnectInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnServerConnectInternal");
			}
			netMsg.conn.SetMaxDelay(m_MaxDelay);
			if (m_MaxBufferedPackets != 512)
			{
				for (int i = 0; i < NetworkServer.numChannels; i++)
				{
					netMsg.conn.SetChannelOption(i, ChannelOption.MaxPendingBuffers, m_MaxBufferedPackets);
				}
			}
			if (!m_AllowFragmentation)
			{
				for (int j = 0; j < NetworkServer.numChannels; j++)
				{
					netMsg.conn.SetChannelOption(j, ChannelOption.AllowFragmentation, 0);
				}
			}
			if (NetworkManager.networkSceneName != "" && NetworkManager.networkSceneName != m_OfflineScene)
			{
				StringMessage msg = new StringMessage(NetworkManager.networkSceneName);
				netMsg.conn.Send(39, msg);
			}
			OnServerConnect(netMsg.conn);
		}

		internal void OnServerDisconnectInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnServerDisconnectInternal");
			}
			OnServerDisconnect(netMsg.conn);
		}

		internal void OnServerReadyMessageInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnServerReadyMessageInternal");
			}
			OnServerReady(netMsg.conn);
		}

		internal void OnServerAddPlayerMessageInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnServerAddPlayerMessageInternal");
			}
			netMsg.ReadMessage<AddPlayerMessage>(NetworkManager.s_AddPlayerMessage);
			if (NetworkManager.s_AddPlayerMessage.msgSize != 0)
			{
				NetworkReader extraMessageReader = new NetworkReader(NetworkManager.s_AddPlayerMessage.msgData);
				OnServerAddPlayer(netMsg.conn, NetworkManager.s_AddPlayerMessage.playerControllerId, extraMessageReader);
			}
			else
			{
				OnServerAddPlayer(netMsg.conn, NetworkManager.s_AddPlayerMessage.playerControllerId);
			}
		}

		internal void OnServerRemovePlayerMessageInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnServerRemovePlayerMessageInternal");
			}
			netMsg.ReadMessage<RemovePlayerMessage>(NetworkManager.s_RemovePlayerMessage);
			PlayerController player;
			netMsg.conn.GetPlayerController(NetworkManager.s_RemovePlayerMessage.playerControllerId, out player);
			OnServerRemovePlayer(netMsg.conn, player);
			netMsg.conn.RemovePlayerController(NetworkManager.s_RemovePlayerMessage.playerControllerId);
		}

		internal void OnServerErrorInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnServerErrorInternal");
			}
			netMsg.ReadMessage<ErrorMessage>(NetworkManager.s_ErrorMessage);
			OnServerError(netMsg.conn, NetworkManager.s_ErrorMessage.errorCode);
		}

		internal void OnClientConnectInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnClientConnectInternal");
			}
			netMsg.conn.SetMaxDelay(m_MaxDelay);
			string name = SceneManager.GetSceneAt(0).name;
			if (string.IsNullOrEmpty(m_OnlineScene) || m_OnlineScene == m_OfflineScene || name == m_OnlineScene)
			{
				m_ClientLoadedScene = false;
				OnClientConnect(netMsg.conn);
			}
			else
			{
				NetworkManager.s_ClientReadyConnection = netMsg.conn;
			}
		}

		internal void OnClientDisconnectInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnClientDisconnectInternal");
			}
			if (!string.IsNullOrEmpty(m_OfflineScene))
			{
				ClientChangeScene(m_OfflineScene, false);
			}
			OnClientDisconnect(netMsg.conn);
		}

		internal void OnClientNotReadyMessageInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnClientNotReadyMessageInternal");
			}
			ClientScene.SetNotReady();
			OnClientNotReady(netMsg.conn);
		}

		internal void OnClientErrorInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnClientErrorInternal");
			}
			netMsg.ReadMessage<ErrorMessage>(NetworkManager.s_ErrorMessage);
			OnClientError(netMsg.conn, NetworkManager.s_ErrorMessage.errorCode);
		}

		internal void OnClientSceneInternal(NetworkMessage netMsg)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkManager:OnClientSceneInternal");
			}
			string newSceneName = netMsg.reader.ReadString();
			if (IsClientConnected() && !NetworkServer.active)
			{
				ClientChangeScene(newSceneName, true);
			}
		}

		public virtual void OnServerConnect(NetworkConnection conn)
		{
		}

		public virtual void OnServerDisconnect(NetworkConnection conn)
		{
			NetworkServer.DestroyPlayersForConnection(conn);
			if (conn.lastError != NetworkError.Ok)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("ServerDisconnected due to error: " + conn.lastError);
				}
			}
		}

		public virtual void OnServerReady(NetworkConnection conn)
		{
			if (conn.playerControllers.Count == 0)
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("Ready with no player object");
				}
			}
			NetworkServer.SetClientReady(conn);
		}

		public virtual void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
		{
			OnServerAddPlayerInternal(conn, playerControllerId);
		}

		public virtual void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
		{
			OnServerAddPlayerInternal(conn, playerControllerId);
		}

		private void OnServerAddPlayerInternal(NetworkConnection conn, short playerControllerId)
		{
			if (m_PlayerPrefab == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.");
				}
			}
			else if (m_PlayerPrefab.GetComponent<NetworkIdentity>() == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
				}
			}
			else if ((int)playerControllerId < conn.playerControllers.Count && conn.playerControllers[(int)playerControllerId].IsValid && conn.playerControllers[(int)playerControllerId].gameObject != null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("There is already a player at that playerControllerId for this connections.");
				}
			}
			else
			{
				Transform startPosition = GetStartPosition();
				GameObject player;
				if (startPosition != null)
				{
					player = UnityEngine.Object.Instantiate<GameObject>(m_PlayerPrefab, startPosition.position, startPosition.rotation);
				}
				else
				{
					player = UnityEngine.Object.Instantiate<GameObject>(m_PlayerPrefab, Vector3.zero, Quaternion.identity);
				}
				NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
			}
		}

		public Transform GetStartPosition()
		{
			if (NetworkManager.s_StartPositions.Count > 0)
			{
				for (int i = NetworkManager.s_StartPositions.Count - 1; i >= 0; i--)
				{
					if (NetworkManager.s_StartPositions[i] == null)
					{
						NetworkManager.s_StartPositions.RemoveAt(i);
					}
				}
			}
			Transform result;
			if (m_PlayerSpawnMethod == PlayerSpawnMethod.Random && NetworkManager.s_StartPositions.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, NetworkManager.s_StartPositions.Count);
				result = NetworkManager.s_StartPositions[index];
			}
			else if (m_PlayerSpawnMethod == PlayerSpawnMethod.RoundRobin && NetworkManager.s_StartPositions.Count > 0)
			{
				if (NetworkManager.s_StartPositionIndex >= NetworkManager.s_StartPositions.Count)
				{
					NetworkManager.s_StartPositionIndex = 0;
				}
				Transform transform = NetworkManager.s_StartPositions[NetworkManager.s_StartPositionIndex];
				NetworkManager.s_StartPositionIndex++;
				result = transform;
			}
			else
			{
				result = null;
			}
			return result;
		}

		public virtual void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
		{
			if (player.gameObject != null)
			{
				NetworkServer.Destroy(player.gameObject);
			}
		}

		public virtual void OnServerError(NetworkConnection conn, int errorCode)
		{
		}

		public virtual void OnServerSceneChanged(string sceneName)
		{
		}

		public virtual void OnClientConnect(NetworkConnection conn)
		{
			if (!clientLoadedScene)
			{
				ClientScene.Ready(conn);
				if (m_AutoCreatePlayer)
				{
					ClientScene.AddPlayer(0);
				}
			}
		}

		public virtual void OnClientDisconnect(NetworkConnection conn)
		{
			StopClient();
			if (conn.lastError != NetworkError.Ok)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("ClientDisconnected due to error: " + conn.lastError);
				}
			}
		}

		public virtual void OnClientError(NetworkConnection conn, int errorCode)
		{
		}

		public virtual void OnClientNotReady(NetworkConnection conn)
		{
		}

		public virtual void OnClientSceneChanged(NetworkConnection conn)
		{
			ClientScene.Ready(conn);
			if (m_AutoCreatePlayer)
			{
				bool flag = ClientScene.localPlayers.Count == 0;
				bool flag2 = false;
				for (int i = 0; i < ClientScene.localPlayers.Count; i++)
				{
					if (ClientScene.localPlayers[i].gameObject != null)
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
					ClientScene.AddPlayer(0);
				}
			}
		}

		public virtual void OnStartHost()
		{
		}

		public virtual void OnStartServer()
		{
		}

		public virtual void OnStartClient(NetworkClient client)
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

		public virtual void OnDropConnection(bool success, string extendedInfo)
		{
			if (LogFilter.logDebug)
			{
				Debug.LogFormat("NetworkManager OnDropConnection Success:{0}, ExtendedInfo:{1}", new object[]
				{
					success,
					extendedInfo
				});
			}
		}

		public virtual void OnSetMatchAttributes(bool success, string extendedInfo)
		{
			if (LogFilter.logDebug)
			{
				Debug.LogFormat("NetworkManager OnSetMatchAttributes Success:{0}, ExtendedInfo:{1}", new object[]
				{
					success,
					extendedInfo
				});
			}
		}

		[SerializeField]
		private int m_NetworkPort = 7777;

		[SerializeField]
		private bool m_ServerBindToIP;

		[SerializeField]
		private string m_ServerBindAddress = "";

		[SerializeField]
		private string m_NetworkAddress = "localhost";

		[SerializeField]
		private bool m_DontDestroyOnLoad = true;

		[SerializeField]
		private bool m_RunInBackground = true;

		[SerializeField]
		private bool m_ScriptCRCCheck = true;

		[SerializeField]
		private float m_MaxDelay = 0.01f;

		[SerializeField]
		private LogFilter.FilterLevel m_LogLevel = LogFilter.FilterLevel.Info;

		[SerializeField]
		private GameObject m_PlayerPrefab;

		[SerializeField]
		private bool m_AutoCreatePlayer = true;

		[SerializeField]
		private PlayerSpawnMethod m_PlayerSpawnMethod;

		[SerializeField]
		private string m_OfflineScene = "";

		[SerializeField]
		private string m_OnlineScene = "";

		[SerializeField]
		private List<GameObject> m_SpawnPrefabs = new List<GameObject>();

		[SerializeField]
		private bool m_CustomConfig;

		[SerializeField]
		private int m_MaxConnections = 4;

		[SerializeField]
		private ConnectionConfig m_ConnectionConfig;

		[SerializeField]
		private GlobalConfig m_GlobalConfig;

		[SerializeField]
		private List<QosType> m_Channels = new List<QosType>();

		[SerializeField]
		private bool m_UseWebSockets;

		[SerializeField]
		private bool m_UseSimulator;

		[SerializeField]
		private int m_SimulatedLatency = 1;

		[SerializeField]
		private float m_PacketLossPercentage;

		[SerializeField]
		private int m_MaxBufferedPackets = 16;

		[SerializeField]
		private bool m_AllowFragmentation = true;

		[SerializeField]
		private string m_MatchHost = "mm.unet.unity3d.com";

		[SerializeField]
		private int m_MatchPort = 443;

		[SerializeField]
		public string matchName = "default";

		[SerializeField]
		public uint matchSize = 4U;

		private EndPoint m_EndPoint;

		private bool m_ClientLoadedScene;

		public static string networkSceneName = "";

		public bool isNetworkActive;

		public NetworkClient client;

		private static List<Transform> s_StartPositions = new List<Transform>();

		private static int s_StartPositionIndex;

		public static NetworkManager singleton;

		private static AddPlayerMessage s_AddPlayerMessage = new AddPlayerMessage();

		private static RemovePlayerMessage s_RemovePlayerMessage = new RemovePlayerMessage();

		private static ErrorMessage s_ErrorMessage = new ErrorMessage();

		private static AsyncOperation s_LoadingSceneAsync;

		private static NetworkConnection s_ClientReadyConnection;

		private static string s_Address;
	}
}
