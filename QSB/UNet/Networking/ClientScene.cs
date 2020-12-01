using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.UNet.Networking
{
	public class ClientScene
	{
		private static bool hasMigrationPending()
		{
			return ClientScene.s_ReconnectId != -1;
		}

		public static void SetReconnectId(int newReconnectId, PeerInfoMessage[] peers)
		{
			ClientScene.s_ReconnectId = newReconnectId;
			ClientScene.s_Peers = peers;
			if (LogFilter.logDebug)
			{
				Debug.Log("ClientScene::SetReconnectId: " + newReconnectId);
			}
		}

		internal static void SetNotReady()
		{
			ClientScene.s_IsReady = false;
		}

		public static List<PlayerController> localPlayers
		{
			get
			{
				return ClientScene.s_LocalPlayers;
			}
		}

		public static bool ready
		{
			get
			{
				return ClientScene.s_IsReady;
			}
		}

		public static NetworkConnection readyConnection
		{
			get
			{
				return ClientScene.s_ReadyConnection;
			}
		}

		public static int reconnectId
		{
			get
			{
				return ClientScene.s_ReconnectId;
			}
		}

		public static Dictionary<NetworkInstanceId, NetworkIdentity> objects
		{
			get
			{
				return ClientScene.s_NetworkScene.localObjects;
			}
		}

		public static Dictionary<NetworkHash128, GameObject> prefabs
		{
			get
			{
				return NetworkScene.guidToPrefab;
			}
		}

		public static Dictionary<NetworkSceneId, NetworkIdentity> spawnableObjects
		{
			get
			{
				return ClientScene.s_SpawnableObjects;
			}
		}

		internal static void Shutdown()
		{
			ClientScene.s_NetworkScene.Shutdown();
			ClientScene.s_LocalPlayers = new List<PlayerController>();
			ClientScene.s_PendingOwnerIds = new List<ClientScene.PendingOwner>();
			ClientScene.s_SpawnableObjects = null;
			ClientScene.s_ReadyConnection = null;
			ClientScene.s_IsReady = false;
			ClientScene.s_IsSpawnFinished = false;
			ClientScene.s_ReconnectId = -1;
			NetworkTransport.Shutdown();
			NetworkTransport.Init();
		}

		internal static bool GetPlayerController(short playerControllerId, out PlayerController player)
		{
			player = null;
			bool result;
			if ((int)playerControllerId >= ClientScene.localPlayers.Count)
			{
				if (LogFilter.logWarn)
				{
					Debug.Log("ClientScene::GetPlayer: no local player found for: " + playerControllerId);
				}
				result = false;
			}
			else if (ClientScene.localPlayers[(int)playerControllerId] == null)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("ClientScene::GetPlayer: local player is null for: " + playerControllerId);
				}
				result = false;
			}
			else
			{
				player = ClientScene.localPlayers[(int)playerControllerId];
				result = (player.gameObject != null);
			}
			return result;
		}

		internal static void InternalAddPlayer(NetworkIdentity view, short playerControllerId)
		{
			if (LogFilter.logDebug)
			{
				Debug.LogWarning("ClientScene::InternalAddPlayer: playerControllerId : " + playerControllerId);
			}
			if ((int)playerControllerId >= ClientScene.s_LocalPlayers.Count)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("ClientScene::InternalAddPlayer: playerControllerId higher than expected: " + playerControllerId);
				}
				while ((int)playerControllerId >= ClientScene.s_LocalPlayers.Count)
				{
					ClientScene.s_LocalPlayers.Add(new PlayerController());
				}
			}
			PlayerController playerController = new PlayerController
			{
				gameObject = view.gameObject,
				playerControllerId = playerControllerId,
				unetView = view
			};
			ClientScene.s_LocalPlayers[(int)playerControllerId] = playerController;
			ClientScene.s_ReadyConnection.SetPlayerController(playerController);
		}

		public static bool AddPlayer(short playerControllerId)
		{
			return ClientScene.AddPlayer(null, playerControllerId);
		}

		public static bool AddPlayer(NetworkConnection readyConn, short playerControllerId)
		{
			return ClientScene.AddPlayer(readyConn, playerControllerId, null);
		}

		public static bool AddPlayer(NetworkConnection readyConn, short playerControllerId, MessageBase extraMessage)
		{
			bool result;
			if (playerControllerId < 0)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("ClientScene::AddPlayer: playerControllerId of " + playerControllerId + " is negative");
				}
				result = false;
			}
			else if (playerControllerId > 32)
			{
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"ClientScene::AddPlayer: playerControllerId of ",
						playerControllerId,
						" is too high, max is ",
						32
					}));
				}
				result = false;
			}
			else
			{
				if (playerControllerId > 16)
				{
					if (LogFilter.logWarn)
					{
						Debug.LogWarning("ClientScene::AddPlayer: playerControllerId of " + playerControllerId + " is unusually high");
					}
				}
				while ((int)playerControllerId >= ClientScene.s_LocalPlayers.Count)
				{
					ClientScene.s_LocalPlayers.Add(new PlayerController());
				}
				if (readyConn == null)
				{
					if (!ClientScene.s_IsReady)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("Must call AddPlayer() with a connection the first time to become ready.");
						}
						return false;
					}
				}
				else
				{
					ClientScene.s_IsReady = true;
					ClientScene.s_ReadyConnection = readyConn;
				}
				PlayerController playerController;
				if (ClientScene.s_ReadyConnection.GetPlayerController(playerControllerId, out playerController))
				{
					if (playerController.IsValid && playerController.gameObject != null)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("ClientScene::AddPlayer: playerControllerId of " + playerControllerId + " already in use.");
						}
						return false;
					}
				}
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"ClientScene::AddPlayer() for ID ",
						playerControllerId,
						" called with connection [",
						ClientScene.s_ReadyConnection,
						"]"
					}));
				}
				if (!ClientScene.hasMigrationPending())
				{
					AddPlayerMessage addPlayerMessage = new AddPlayerMessage();
					addPlayerMessage.playerControllerId = playerControllerId;
					if (extraMessage != null)
					{
						NetworkWriter networkWriter = new NetworkWriter();
						extraMessage.Serialize(networkWriter);
						addPlayerMessage.msgData = networkWriter.ToArray();
						addPlayerMessage.msgSize = (int)networkWriter.Position;
					}
					ClientScene.s_ReadyConnection.Send(37, addPlayerMessage);
					result = true;
				}
				else
				{
					result = ClientScene.SendReconnectMessage(extraMessage);
				}
			}
			return result;
		}

		public static bool SendReconnectMessage(MessageBase extraMessage)
		{
			bool result;
			if (!ClientScene.hasMigrationPending())
			{
				result = false;
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("ClientScene::AddPlayer reconnect " + ClientScene.s_ReconnectId);
				}
				if (ClientScene.s_Peers == null)
				{
					ClientScene.SetReconnectId(-1, null);
					if (LogFilter.logError)
					{
						Debug.LogError("ClientScene::AddPlayer: reconnecting, but no peers.");
					}
					result = false;
				}
				else
				{
					for (int i = 0; i < ClientScene.s_Peers.Length; i++)
					{
						PeerInfoMessage peerInfoMessage = ClientScene.s_Peers[i];
						if (peerInfoMessage.playerIds != null)
						{
							if (peerInfoMessage.connectionId == ClientScene.s_ReconnectId)
							{
								for (int j = 0; j < peerInfoMessage.playerIds.Length; j++)
								{
									ReconnectMessage reconnectMessage = new ReconnectMessage();
									reconnectMessage.oldConnectionId = ClientScene.s_ReconnectId;
									reconnectMessage.netId = peerInfoMessage.playerIds[j].netId;
									reconnectMessage.playerControllerId = peerInfoMessage.playerIds[j].playerControllerId;
									if (extraMessage != null)
									{
										NetworkWriter networkWriter = new NetworkWriter();
										extraMessage.Serialize(networkWriter);
										reconnectMessage.msgData = networkWriter.ToArray();
										reconnectMessage.msgSize = (int)networkWriter.Position;
									}
									ClientScene.s_ReadyConnection.Send(47, reconnectMessage);
								}
							}
						}
					}
					ClientScene.SetReconnectId(-1, null);
					result = true;
				}
			}
			return result;
		}

		public static bool RemovePlayer(short playerControllerId)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"ClientScene::RemovePlayer() for ID ",
					playerControllerId,
					" called with connection [",
					ClientScene.s_ReadyConnection,
					"]"
				}));
			}
			PlayerController playerController;
			bool result;
			if (ClientScene.s_ReadyConnection.GetPlayerController(playerControllerId, out playerController))
			{
				RemovePlayerMessage removePlayerMessage = new RemovePlayerMessage();
				removePlayerMessage.playerControllerId = playerControllerId;
				ClientScene.s_ReadyConnection.Send(38, removePlayerMessage);
				ClientScene.s_ReadyConnection.RemovePlayerController(playerControllerId);
				ClientScene.s_LocalPlayers[(int)playerControllerId] = new PlayerController();
				UnityEngine.Object.Destroy(playerController.gameObject);
				result = true;
			}
			else
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Failed to find player ID " + playerControllerId);
				}
				result = false;
			}
			return result;
		}

		public static bool Ready(NetworkConnection conn)
		{
			bool result;
			if (ClientScene.s_IsReady)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("A connection has already been set as ready. There can only be one.");
				}
				result = false;
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("ClientScene::Ready() called with connection [" + conn + "]");
				}
				if (conn != null)
				{
					ReadyMessage msg = new ReadyMessage();
					conn.Send(35, msg);
					ClientScene.s_IsReady = true;
					ClientScene.s_ReadyConnection = conn;
					ClientScene.s_ReadyConnection.isReady = true;
					result = true;
				}
				else
				{
					if (LogFilter.logError)
					{
						Debug.LogError("Ready() called with invalid connection object: conn=null");
					}
					result = false;
				}
			}
			return result;
		}

		public static NetworkClient ConnectLocalServer()
		{
			LocalClient localClient = new LocalClient();
			NetworkServer.instance.ActivateLocalClientScene();
			localClient.InternalConnectLocalServer(true);
			return localClient;
		}

		internal static NetworkClient ReconnectLocalServer()
		{
			LocalClient localClient = new LocalClient();
			NetworkServer.instance.ActivateLocalClientScene();
			localClient.InternalConnectLocalServer(false);
			return localClient;
		}

		internal static void ClearLocalPlayers()
		{
			ClientScene.s_LocalPlayers.Clear();
		}

		internal static void HandleClientDisconnect(NetworkConnection conn)
		{
			if (ClientScene.s_ReadyConnection == conn && ClientScene.s_IsReady)
			{
				ClientScene.s_IsReady = false;
				ClientScene.s_ReadyConnection = null;
			}
		}

		internal static void PrepareToSpawnSceneObjects()
		{
			ClientScene.s_SpawnableObjects = new Dictionary<NetworkSceneId, NetworkIdentity>();
			foreach (NetworkIdentity networkIdentity in Resources.FindObjectsOfTypeAll<NetworkIdentity>())
			{
				if (!networkIdentity.gameObject.activeSelf)
				{
					if (networkIdentity.gameObject.hideFlags != HideFlags.NotEditable && networkIdentity.gameObject.hideFlags != HideFlags.HideAndDontSave)
					{
						if (!networkIdentity.sceneId.IsEmpty())
						{
							ClientScene.s_SpawnableObjects[networkIdentity.sceneId] = networkIdentity;
							if (LogFilter.logDebug)
							{
								Debug.Log("ClientScene::PrepareSpawnObjects sceneId:" + networkIdentity.sceneId);
							}
						}
					}
				}
			}
		}

		internal static NetworkIdentity SpawnSceneObject(NetworkSceneId sceneId)
		{
			NetworkIdentity result;
			if (ClientScene.s_SpawnableObjects.ContainsKey(sceneId))
			{
				NetworkIdentity networkIdentity = ClientScene.s_SpawnableObjects[sceneId];
				ClientScene.s_SpawnableObjects.Remove(sceneId);
				result = networkIdentity;
			}
			else
			{
				result = null;
			}
			return result;
		}

		internal static void RegisterSystemHandlers(NetworkClient client, bool localClient)
		{
			if (localClient)
			{
				client.RegisterHandlerSafe((short)1, new NetworkMessageDelegate(ClientScene.OnLocalClientObjectDestroy));
				client.RegisterHandlerSafe((short)13, new NetworkMessageDelegate(ClientScene.OnLocalClientObjectHide));
				client.RegisterHandlerSafe((short)3, new NetworkMessageDelegate(ClientScene.OnLocalClientObjectSpawn));
				client.RegisterHandlerSafe((short)10, new NetworkMessageDelegate(ClientScene.OnLocalClientObjectSpawnScene));
				client.RegisterHandlerSafe((short)15, new NetworkMessageDelegate(ClientScene.OnClientAuthority));
			}
			else
			{
				client.RegisterHandlerSafe((short)3, new NetworkMessageDelegate(ClientScene.OnObjectSpawn));
				client.RegisterHandlerSafe((short)10, new NetworkMessageDelegate(ClientScene.OnObjectSpawnScene));
				client.RegisterHandlerSafe((short)12, new NetworkMessageDelegate(ClientScene.OnObjectSpawnFinished));
				client.RegisterHandlerSafe((short)1, new NetworkMessageDelegate(ClientScene.OnObjectDestroy));
				client.RegisterHandlerSafe((short)13, new NetworkMessageDelegate(ClientScene.OnObjectDestroy));
				client.RegisterHandlerSafe((short)8, new NetworkMessageDelegate(ClientScene.OnUpdateVarsMessage));
				client.RegisterHandlerSafe((short)4, new NetworkMessageDelegate(ClientScene.OnOwnerMessage));
				client.RegisterHandlerSafe((short)9, new NetworkMessageDelegate(ClientScene.OnSyncListMessage));
				client.RegisterHandlerSafe((short)40, new NetworkMessageDelegate(NetworkAnimator.OnAnimationClientMessage));
				client.RegisterHandlerSafe((short)41, new NetworkMessageDelegate(NetworkAnimator.OnAnimationParametersClientMessage));
				client.RegisterHandlerSafe((short)15, new NetworkMessageDelegate(ClientScene.OnClientAuthority));
			}
			client.RegisterHandlerSafe((short)2, new NetworkMessageDelegate(ClientScene.OnRPCMessage));
			client.RegisterHandlerSafe((short)7, new NetworkMessageDelegate(ClientScene.OnSyncEventMessage));
			client.RegisterHandlerSafe((short)42, new NetworkMessageDelegate(NetworkAnimator.OnAnimationTriggerClientMessage));
		}

		internal static string GetStringForAssetId(NetworkHash128 assetId)
		{
			GameObject gameObject;
			string result;
			SpawnDelegate func;
			if (NetworkScene.GetPrefab(assetId, out gameObject))
			{
				result = gameObject.name;
			}
			else if (NetworkScene.GetSpawnHandler(assetId, out func))
			{
				result = func.GetMethodName();
			}
			else
			{
				result = "unknown";
			}
			return result;
		}

		public static void RegisterPrefab(GameObject prefab, NetworkHash128 newAssetId)
		{
			NetworkScene.RegisterPrefab(prefab, newAssetId);
		}

		public static void RegisterPrefab(GameObject prefab)
		{
			NetworkScene.RegisterPrefab(prefab);
		}

		public static void RegisterPrefab(GameObject prefab, SpawnDelegate spawnHandler, UnSpawnDelegate unspawnHandler)
		{
			NetworkScene.RegisterPrefab(prefab, spawnHandler, unspawnHandler);
		}

		public static void UnregisterPrefab(GameObject prefab)
		{
			NetworkScene.UnregisterPrefab(prefab);
		}

		public static void RegisterSpawnHandler(NetworkHash128 assetId, SpawnDelegate spawnHandler, UnSpawnDelegate unspawnHandler)
		{
			NetworkScene.RegisterSpawnHandler(assetId, spawnHandler, unspawnHandler);
		}

		public static void UnregisterSpawnHandler(NetworkHash128 assetId)
		{
			NetworkScene.UnregisterSpawnHandler(assetId);
		}

		public static void ClearSpawners()
		{
			NetworkScene.ClearSpawners();
		}

		public static void DestroyAllClientObjects()
		{
			ClientScene.s_NetworkScene.DestroyAllClientObjects();
		}

		public static void SetLocalObject(NetworkInstanceId netId, GameObject obj)
		{
			ClientScene.s_NetworkScene.SetLocalObject(netId, obj, ClientScene.s_IsSpawnFinished, false);
		}

		public static GameObject FindLocalObject(NetworkInstanceId netId)
		{
			return ClientScene.s_NetworkScene.FindLocalObject(netId);
		}

		private static void ApplySpawnPayload(NetworkIdentity uv, Vector3 position, byte[] payload, NetworkInstanceId netId, GameObject newGameObject)
		{
			if (!uv.gameObject.activeSelf)
			{
				uv.gameObject.SetActive(true);
			}
			uv.transform.position = position;
			if (payload != null && payload.Length > 0)
			{
				NetworkReader reader = new NetworkReader(payload);
				uv.OnUpdateVars(reader, true);
			}
			if (!(newGameObject == null))
			{
				newGameObject.SetActive(true);
				uv.SetNetworkInstanceId(netId);
				ClientScene.SetLocalObject(netId, newGameObject);
				if (ClientScene.s_IsSpawnFinished)
				{
					uv.OnStartClient();
					ClientScene.CheckForOwner(uv);
				}
			}
		}

		private static void OnObjectSpawn(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectSpawnMessage>(ClientScene.s_ObjectSpawnMessage);
			if (!ClientScene.s_ObjectSpawnMessage.assetId.IsValid())
			{
				if (LogFilter.logError)
				{
					Debug.LogError("OnObjSpawn netId: " + ClientScene.s_ObjectSpawnMessage.netId + " has invalid asset Id");
				}
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Client spawn handler instantiating [netId:",
						ClientScene.s_ObjectSpawnMessage.netId,
						" asset ID:",
						ClientScene.s_ObjectSpawnMessage.assetId,
						" pos:",
						ClientScene.s_ObjectSpawnMessage.position,
						"]"
					}));
				}
				NetworkIdentity component;
				GameObject original;
				SpawnDelegate spawnDelegate;
				if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ObjectSpawnMessage.netId, out component))
				{
					ClientScene.ApplySpawnPayload(component, ClientScene.s_ObjectSpawnMessage.position, ClientScene.s_ObjectSpawnMessage.payload, ClientScene.s_ObjectSpawnMessage.netId, null);
				}
				else if (NetworkScene.GetPrefab(ClientScene.s_ObjectSpawnMessage.assetId, out original))
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, ClientScene.s_ObjectSpawnMessage.position, ClientScene.s_ObjectSpawnMessage.rotation);
					if (LogFilter.logDebug)
					{
						Debug.Log(string.Concat(new object[]
						{
							"Client spawn handler instantiating [netId:",
							ClientScene.s_ObjectSpawnMessage.netId,
							" asset ID:",
							ClientScene.s_ObjectSpawnMessage.assetId,
							" pos:",
							ClientScene.s_ObjectSpawnMessage.position,
							" rotation: ",
							ClientScene.s_ObjectSpawnMessage.rotation,
							"]"
						}));
					}
					component = gameObject.GetComponent<NetworkIdentity>();
					if (component == null)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("Client object spawned for " + ClientScene.s_ObjectSpawnMessage.assetId + " does not have a NetworkIdentity");
						}
					}
					else
					{
						component.Reset();
						ClientScene.ApplySpawnPayload(component, ClientScene.s_ObjectSpawnMessage.position, ClientScene.s_ObjectSpawnMessage.payload, ClientScene.s_ObjectSpawnMessage.netId, gameObject);
					}
				}
				else if (NetworkScene.GetSpawnHandler(ClientScene.s_ObjectSpawnMessage.assetId, out spawnDelegate))
				{
					GameObject gameObject2 = spawnDelegate(ClientScene.s_ObjectSpawnMessage.position, ClientScene.s_ObjectSpawnMessage.assetId);
					if (gameObject2 == null)
					{
						if (LogFilter.logWarn)
						{
							Debug.LogWarning("Client spawn handler for " + ClientScene.s_ObjectSpawnMessage.assetId + " returned null");
						}
					}
					else
					{
						component = gameObject2.GetComponent<NetworkIdentity>();
						if (component == null)
						{
							if (LogFilter.logError)
							{
								Debug.LogError("Client object spawned for " + ClientScene.s_ObjectSpawnMessage.assetId + " does not have a network identity");
							}
						}
						else
						{
							component.Reset();
							component.SetDynamicAssetId(ClientScene.s_ObjectSpawnMessage.assetId);
							ClientScene.ApplySpawnPayload(component, ClientScene.s_ObjectSpawnMessage.position, ClientScene.s_ObjectSpawnMessage.payload, ClientScene.s_ObjectSpawnMessage.netId, gameObject2);
						}
					}
				}
				else if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Failed to spawn server object, did you forget to add it to the NetworkManager? assetId=",
						ClientScene.s_ObjectSpawnMessage.assetId,
						" netId=",
						ClientScene.s_ObjectSpawnMessage.netId
					}));
				}
			}
		}

		private static void OnObjectSpawnScene(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectSpawnSceneMessage>(ClientScene.s_ObjectSpawnSceneMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"Client spawn scene handler instantiating [netId:",
					ClientScene.s_ObjectSpawnSceneMessage.netId,
					" sceneId:",
					ClientScene.s_ObjectSpawnSceneMessage.sceneId,
					" pos:",
					ClientScene.s_ObjectSpawnSceneMessage.position
				}));
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ObjectSpawnSceneMessage.netId, out networkIdentity))
			{
				ClientScene.ApplySpawnPayload(networkIdentity, ClientScene.s_ObjectSpawnSceneMessage.position, ClientScene.s_ObjectSpawnSceneMessage.payload, ClientScene.s_ObjectSpawnSceneMessage.netId, networkIdentity.gameObject);
			}
			else
			{
				NetworkIdentity networkIdentity2 = ClientScene.SpawnSceneObject(ClientScene.s_ObjectSpawnSceneMessage.sceneId);
				if (networkIdentity2 == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("Spawn scene object not found for " + ClientScene.s_ObjectSpawnSceneMessage.sceneId);
					}
				}
				else
				{
					if (LogFilter.logDebug)
					{
						Debug.Log(string.Concat(new object[]
						{
							"Client spawn for [netId:",
							ClientScene.s_ObjectSpawnSceneMessage.netId,
							"] [sceneId:",
							ClientScene.s_ObjectSpawnSceneMessage.sceneId,
							"] obj:",
							networkIdentity2.gameObject.name
						}));
					}
					ClientScene.ApplySpawnPayload(networkIdentity2, ClientScene.s_ObjectSpawnSceneMessage.position, ClientScene.s_ObjectSpawnSceneMessage.payload, ClientScene.s_ObjectSpawnSceneMessage.netId, networkIdentity2.gameObject);
				}
			}
		}

		private static void OnObjectSpawnFinished(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectSpawnFinishedMessage>(ClientScene.s_ObjectSpawnFinishedMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log("SpawnFinished:" + ClientScene.s_ObjectSpawnFinishedMessage.state);
			}
			if (ClientScene.s_ObjectSpawnFinishedMessage.state == 0U)
			{
				ClientScene.PrepareToSpawnSceneObjects();
				ClientScene.s_IsSpawnFinished = false;
			}
			else
			{
				foreach (NetworkIdentity networkIdentity in ClientScene.objects.Values)
				{
					if (!networkIdentity.isClient)
					{
						networkIdentity.OnStartClient();
						ClientScene.CheckForOwner(networkIdentity);
					}
				}
				ClientScene.s_IsSpawnFinished = true;
			}
		}

		private static void OnObjectDestroy(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectDestroyMessage>(ClientScene.s_ObjectDestroyMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log("ClientScene::OnObjDestroy netId:" + ClientScene.s_ObjectDestroyMessage.netId);
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ObjectDestroyMessage.netId, out networkIdentity))
			{
				networkIdentity.OnNetworkDestroy();
				if (!NetworkScene.InvokeUnSpawnHandler(networkIdentity.assetId, networkIdentity.gameObject))
				{
					if (networkIdentity.sceneId.IsEmpty())
					{
						UnityEngine.Object.Destroy(networkIdentity.gameObject);
					}
					else
					{
						networkIdentity.gameObject.SetActive(false);
						ClientScene.s_SpawnableObjects[networkIdentity.sceneId] = networkIdentity;
					}
				}
				ClientScene.s_NetworkScene.RemoveLocalObject(ClientScene.s_ObjectDestroyMessage.netId);
				networkIdentity.MarkForReset();
			}
			else if (LogFilter.logDebug)
			{
				Debug.LogWarning("Did not find target for destroy message for " + ClientScene.s_ObjectDestroyMessage.netId);
			}
		}

		private static void OnLocalClientObjectDestroy(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectDestroyMessage>(ClientScene.s_ObjectDestroyMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log("ClientScene::OnLocalObjectObjDestroy netId:" + ClientScene.s_ObjectDestroyMessage.netId);
			}
			ClientScene.s_NetworkScene.RemoveLocalObject(ClientScene.s_ObjectDestroyMessage.netId);
		}

		private static void OnLocalClientObjectHide(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectDestroyMessage>(ClientScene.s_ObjectDestroyMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log("ClientScene::OnLocalObjectObjHide netId:" + ClientScene.s_ObjectDestroyMessage.netId);
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ObjectDestroyMessage.netId, out networkIdentity))
			{
				networkIdentity.OnSetLocalVisibility(false);
			}
		}

		private static void OnLocalClientObjectSpawn(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectSpawnMessage>(ClientScene.s_ObjectSpawnMessage);
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ObjectSpawnMessage.netId, out networkIdentity))
			{
				networkIdentity.OnSetLocalVisibility(true);
			}
		}

		private static void OnLocalClientObjectSpawnScene(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ObjectSpawnSceneMessage>(ClientScene.s_ObjectSpawnSceneMessage);
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ObjectSpawnSceneMessage.netId, out networkIdentity))
			{
				networkIdentity.OnSetLocalVisibility(true);
			}
		}

		private static void OnUpdateVarsMessage(NetworkMessage netMsg)
		{
			NetworkInstanceId networkInstanceId = netMsg.reader.ReadNetworkId();
			if (LogFilter.logDev)
			{
				Debug.Log(string.Concat(new object[]
				{
					"ClientScene::OnUpdateVarsMessage ",
					networkInstanceId,
					" channel:",
					netMsg.channelId
				}));
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(networkInstanceId, out networkIdentity))
			{
				networkIdentity.OnUpdateVars(netMsg.reader, false);
			}
			else if (LogFilter.logWarn)
			{
				Debug.LogWarning("Did not find target for sync message for " + networkInstanceId);
			}
		}

		private static void OnRPCMessage(NetworkMessage netMsg)
		{
			int num = (int)netMsg.reader.ReadPackedUInt32();
			NetworkInstanceId networkInstanceId = netMsg.reader.ReadNetworkId();
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"ClientScene::OnRPCMessage hash:",
					num,
					" netId:",
					networkInstanceId
				}));
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(networkInstanceId, out networkIdentity))
			{
				networkIdentity.HandleRPC(num, netMsg.reader);
			}
			else if (LogFilter.logWarn)
			{
				string cmdHashHandlerName = NetworkBehaviour.GetCmdHashHandlerName(num);
				Debug.LogWarningFormat("Could not find target object with netId:{0} for RPC call {1}", new object[]
				{
					networkInstanceId,
					cmdHashHandlerName
				});
			}
		}

		private static void OnSyncEventMessage(NetworkMessage netMsg)
		{
			int cmdHash = (int)netMsg.reader.ReadPackedUInt32();
			NetworkInstanceId networkInstanceId = netMsg.reader.ReadNetworkId();
			if (LogFilter.logDebug)
			{
				Debug.Log("ClientScene::OnSyncEventMessage " + networkInstanceId);
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(networkInstanceId, out networkIdentity))
			{
				networkIdentity.HandleSyncEvent(cmdHash, netMsg.reader);
			}
			else if (LogFilter.logWarn)
			{
				Debug.LogWarning("Did not find target for SyncEvent message for " + networkInstanceId);
			}
		}

		private static void OnSyncListMessage(NetworkMessage netMsg)
		{
			NetworkInstanceId networkInstanceId = netMsg.reader.ReadNetworkId();
			int cmdHash = (int)netMsg.reader.ReadPackedUInt32();
			if (LogFilter.logDebug)
			{
				Debug.Log("ClientScene::OnSyncListMessage " + networkInstanceId);
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(networkInstanceId, out networkIdentity))
			{
				networkIdentity.HandleSyncList(cmdHash, netMsg.reader);
			}
			else if (LogFilter.logWarn)
			{
				Debug.LogWarning("Did not find target for SyncList message for " + networkInstanceId);
			}
		}

		private static void OnClientAuthority(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<ClientAuthorityMessage>(ClientScene.s_ClientAuthorityMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"ClientScene::OnClientAuthority for  connectionId=",
					netMsg.conn.connectionId,
					" netId: ",
					ClientScene.s_ClientAuthorityMessage.netId
				}));
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_ClientAuthorityMessage.netId, out networkIdentity))
			{
				networkIdentity.HandleClientAuthority(ClientScene.s_ClientAuthorityMessage.authority);
			}
		}

		private static void OnOwnerMessage(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<OwnerMessage>(ClientScene.s_OwnerMessage);
			if (LogFilter.logDebug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"ClientScene::OnOwnerMessage - connectionId=",
					netMsg.conn.connectionId,
					" netId: ",
					ClientScene.s_OwnerMessage.netId
				}));
			}
			PlayerController playerController;
			if (netMsg.conn.GetPlayerController(ClientScene.s_OwnerMessage.playerControllerId, out playerController))
			{
				playerController.unetView.SetNotLocalPlayer();
			}
			NetworkIdentity networkIdentity;
			if (ClientScene.s_NetworkScene.GetNetworkIdentity(ClientScene.s_OwnerMessage.netId, out networkIdentity))
			{
				networkIdentity.SetConnectionToServer(netMsg.conn);
				networkIdentity.SetLocalPlayer(ClientScene.s_OwnerMessage.playerControllerId);
				ClientScene.InternalAddPlayer(networkIdentity, ClientScene.s_OwnerMessage.playerControllerId);
			}
			else
			{
				ClientScene.PendingOwner item = new ClientScene.PendingOwner
				{
					netId = ClientScene.s_OwnerMessage.netId,
					playerControllerId = ClientScene.s_OwnerMessage.playerControllerId
				};
				ClientScene.s_PendingOwnerIds.Add(item);
			}
		}

		private static void CheckForOwner(NetworkIdentity uv)
		{
			int i = 0;
			while (i < ClientScene.s_PendingOwnerIds.Count)
			{
				ClientScene.PendingOwner pendingOwner = ClientScene.s_PendingOwnerIds[i];
				if (pendingOwner.netId == uv.netId)
				{
					uv.SetConnectionToServer(ClientScene.s_ReadyConnection);
					uv.SetLocalPlayer(pendingOwner.playerControllerId);
					if (LogFilter.logDev)
					{
						Debug.Log("ClientScene::OnOwnerMessage - player=" + uv.gameObject.name);
					}
					if (ClientScene.s_ReadyConnection.connectionId < 0)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("Owner message received on a local client.");
						}
						break;
					}
					ClientScene.InternalAddPlayer(uv, pendingOwner.playerControllerId);
					ClientScene.s_PendingOwnerIds.RemoveAt(i);
					break;
				}
				else
				{
					i++;
				}
			}
		}

		private static List<PlayerController> s_LocalPlayers = new List<PlayerController>();

		private static NetworkConnection s_ReadyConnection;

		private static Dictionary<NetworkSceneId, NetworkIdentity> s_SpawnableObjects;

		private static bool s_IsReady;

		private static bool s_IsSpawnFinished;

		private static NetworkScene s_NetworkScene = new NetworkScene();

		private static ObjectSpawnSceneMessage s_ObjectSpawnSceneMessage = new ObjectSpawnSceneMessage();

		private static ObjectSpawnFinishedMessage s_ObjectSpawnFinishedMessage = new ObjectSpawnFinishedMessage();

		private static ObjectDestroyMessage s_ObjectDestroyMessage = new ObjectDestroyMessage();

		private static ObjectSpawnMessage s_ObjectSpawnMessage = new ObjectSpawnMessage();

		private static OwnerMessage s_OwnerMessage = new OwnerMessage();

		private static ClientAuthorityMessage s_ClientAuthorityMessage = new ClientAuthorityMessage();

		public const int ReconnectIdInvalid = -1;

		public const int ReconnectIdHost = 0;

		private static int s_ReconnectId = -1;

		private static PeerInfoMessage[] s_Peers;

		private static List<ClientScene.PendingOwner> s_PendingOwnerIds = new List<ClientScene.PendingOwner>();

		private struct PendingOwner
		{
			public NetworkInstanceId netId;

			public short playerControllerId;
		}
	}
}
