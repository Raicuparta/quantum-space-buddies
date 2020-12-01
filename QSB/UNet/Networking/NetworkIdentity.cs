using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;


namespace QSB.UNet.Networking
{
	[AddComponentMenu("Network/NetworkIdentity")]
	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	public sealed class NetworkIdentity : MonoBehaviour
	{
		public bool isClient
		{
			get
			{
				return m_IsClient;
			}
		}

		public bool isServer
		{
			get
			{
				return m_IsServer && NetworkServer.active && m_IsServer;
			}
		}

		public bool hasAuthority
		{
			get
			{
				return m_HasAuthority;
			}
		}

		public NetworkInstanceId netId
		{
			get
			{
				return m_NetId;
			}
		}

		public NetworkSceneId sceneId
		{
			get
			{
				return m_SceneId;
			}
		}

		public bool serverOnly
		{
			get
			{
				return m_ServerOnly;
			}
			set
			{
				m_ServerOnly = value;
			}
		}

		public bool localPlayerAuthority
		{
			get
			{
				return m_LocalPlayerAuthority;
			}
			set
			{
				m_LocalPlayerAuthority = value;
			}
		}

		public NetworkConnection clientAuthorityOwner
		{
			get
			{
				return m_ClientAuthorityOwner;
			}
		}

		public NetworkHash128 assetId
		{
			get
			{
				return m_AssetId;
			}
		}

		internal void SetDynamicAssetId(NetworkHash128 newAssetId)
		{
			if (!m_AssetId.IsValid() || m_AssetId.Equals(newAssetId))
			{
				m_AssetId = newAssetId;
			}
			else if (LogFilter.logWarn)
			{
				Debug.LogWarning("SetDynamicAssetId object already has an assetId <" + m_AssetId + ">");
			}
		}

		internal void SetClientOwner(NetworkConnection conn)
		{
			if (m_ClientAuthorityOwner != null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("SetClientOwner m_ClientAuthorityOwner already set!");
				}
			}
			m_ClientAuthorityOwner = conn;
			m_ClientAuthorityOwner.AddOwnedObject(this);
		}

		internal void ClearClientOwner()
		{
			m_ClientAuthorityOwner = null;
		}

		internal void ForceAuthority(bool authority)
		{
			if (m_HasAuthority != authority)
			{
				m_HasAuthority = authority;
				if (authority)
				{
					OnStartAuthority();
				}
				else
				{
					OnStopAuthority();
				}
			}
		}

		public bool isLocalPlayer
		{
			get
			{
				return m_IsLocalPlayer;
			}
		}

		public short playerControllerId
		{
			get
			{
				return m_PlayerId;
			}
		}

		public NetworkConnection connectionToServer
		{
			get
			{
				return m_ConnectionToServer;
			}
		}

		public NetworkConnection connectionToClient
		{
			get
			{
				return m_ConnectionToClient;
			}
		}

		public ReadOnlyCollection<NetworkConnection> observers
		{
			get
			{
				ReadOnlyCollection<NetworkConnection> result;
				if (m_Observers == null)
				{
					result = null;
				}
				else
				{
					result = new ReadOnlyCollection<NetworkConnection>(m_Observers);
				}
				return result;
			}
		}

		internal static NetworkInstanceId GetNextNetworkId()
		{
			uint value = NetworkIdentity.s_NextNetworkId;
			NetworkIdentity.s_NextNetworkId += 1U;
			return new NetworkInstanceId(value);
		}

		private void CacheBehaviours()
		{
			if (m_NetworkBehaviours == null)
			{
				m_NetworkBehaviours = base.GetComponents<NetworkBehaviour>();
			}
		}

		internal static void AddNetworkId(uint id)
		{
			if (id >= NetworkIdentity.s_NextNetworkId)
			{
				NetworkIdentity.s_NextNetworkId = id + 1U;
			}
		}

		internal void SetNetworkInstanceId(NetworkInstanceId newNetId)
		{
			m_NetId = newNetId;
			if (newNetId.Value == 0U)
			{
				m_IsServer = false;
			}
		}

		public void ForceSceneId(int newSceneId)
		{
			m_SceneId = new NetworkSceneId((uint)newSceneId);
		}

		internal void UpdateClientServer(bool isClientFlag, bool isServerFlag)
		{
			m_IsClient = (m_IsClient || isClientFlag);
			m_IsServer = (m_IsServer || isServerFlag);
		}

		internal void SetNotLocalPlayer()
		{
			m_IsLocalPlayer = false;
			if (!NetworkServer.active || !NetworkServer.localClientActive)
			{
				m_HasAuthority = false;
			}
		}

		internal void RemoveObserverInternal(NetworkConnection conn)
		{
			if (m_Observers != null)
			{
				m_Observers.Remove(conn);
				m_ObserverConnections.Remove(conn.connectionId);
			}
		}

		private void OnDestroy()
		{
			if (m_IsServer && NetworkServer.active)
			{
				NetworkServer.Destroy(base.gameObject);
			}
		}

		internal void OnStartServer(bool allowNonZeroNetId)
		{
			if (!m_IsServer)
			{
				m_IsServer = true;
				if (m_LocalPlayerAuthority)
				{
					m_HasAuthority = false;
				}
				else
				{
					m_HasAuthority = true;
				}
				m_Observers = new List<NetworkConnection>();
				m_ObserverConnections = new HashSet<int>();
				CacheBehaviours();
				if (netId.IsEmpty())
				{
					m_NetId = NetworkIdentity.GetNextNetworkId();
				}
				else if (!allowNonZeroNetId)
				{
					if (LogFilter.logError)
					{
						Debug.LogError(string.Concat(new object[]
						{
							"Object has non-zero netId ",
							netId,
							" for ",
							base.gameObject
						}));
					}
					return;
				}
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"OnStartServer ",
						base.gameObject,
						" GUID:",
						netId
					}));
				}
				NetworkServer.instance.SetLocalObjectOnServer(netId, base.gameObject);
				for (int i = 0; i < m_NetworkBehaviours.Length; i++)
				{
					NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
					try
					{
						networkBehaviour.OnStartServer();
					}
					catch (Exception ex)
					{
						Debug.LogError("Exception in OnStartServer:" + ex.Message + " " + ex.StackTrace);
					}
				}
				if (NetworkClient.active && NetworkServer.localClientActive)
				{
					ClientScene.SetLocalObject(netId, base.gameObject);
					OnStartClient();
				}
				if (m_HasAuthority)
				{
					OnStartAuthority();
				}
			}
		}

		internal void OnStartClient()
		{
			if (!m_IsClient)
			{
				m_IsClient = true;
			}
			CacheBehaviours();
			if (LogFilter.logDev)
			{
				Debug.Log(string.Concat(new object[]
				{
					"OnStartClient ",
					base.gameObject,
					" GUID:",
					netId,
					" localPlayerAuthority:",
					localPlayerAuthority
				}));
			}
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				try
				{
					networkBehaviour.PreStartClient();
					networkBehaviour.OnStartClient();
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception in OnStartClient:" + ex.Message + " " + ex.StackTrace);
				}
			}
		}

		internal void OnStartAuthority()
		{
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				try
				{
					networkBehaviour.OnStartAuthority();
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception in OnStartAuthority:" + ex.Message + " " + ex.StackTrace);
				}
			}
		}

		internal void OnStopAuthority()
		{
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				try
				{
					networkBehaviour.OnStopAuthority();
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception in OnStopAuthority:" + ex.Message + " " + ex.StackTrace);
				}
			}
		}

		internal void OnSetLocalVisibility(bool vis)
		{
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				try
				{
					networkBehaviour.OnSetLocalVisibility(vis);
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception in OnSetLocalVisibility:" + ex.Message + " " + ex.StackTrace);
				}
			}
		}

		internal bool OnCheckObserver(NetworkConnection conn)
		{
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				try
				{
					if (!networkBehaviour.OnCheckObserver(conn))
					{
						return false;
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception in OnCheckObserver:" + ex.Message + " " + ex.StackTrace);
				}
			}
			return true;
		}

		internal void UNetSerializeAllVars(NetworkWriter writer)
		{
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				networkBehaviour.OnSerialize(writer, true);
			}
		}

		internal void HandleClientAuthority(bool authority)
		{
			if (!localPlayerAuthority)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("HandleClientAuthority " + base.gameObject + " does not have localPlayerAuthority");
				}
			}
			else
			{
				ForceAuthority(authority);
			}
		}

		private bool GetInvokeComponent(int cmdHash, Type invokeClass, out NetworkBehaviour invokeComponent)
		{
			NetworkBehaviour networkBehaviour = null;
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour2 = m_NetworkBehaviours[i];
				if (networkBehaviour2.GetType() == invokeClass || networkBehaviour2.GetType().IsSubclassOf(invokeClass))
				{
					networkBehaviour = networkBehaviour2;
					break;
				}
			}
			bool result;
			if (networkBehaviour == null)
			{
				string cmdHashHandlerName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Found no behaviour for incoming [",
						cmdHashHandlerName,
						"] on ",
						base.gameObject,
						",  the server and client should have the same NetworkBehaviour instances [netId=",
						netId,
						"]."
					}));
				}
				invokeComponent = null;
				result = false;
			}
			else
			{
				invokeComponent = networkBehaviour;
				result = true;
			}
			return result;
		}

		internal void HandleSyncEvent(int cmdHash, NetworkReader reader)
		{
			Type invokeClass;
			NetworkBehaviour.CmdDelegate cmdDelegate;
			NetworkBehaviour obj;
			if (base.gameObject == null)
			{
				string cmdHashHandlerName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"SyncEvent [",
						cmdHashHandlerName,
						"] received for deleted object [netId=",
						netId,
						"]"
					}));
				}
			}
			else if (!NetworkBehaviour.GetInvokerForHashSyncEvent(cmdHash, out invokeClass, out cmdDelegate))
			{
				string cmdHashHandlerName2 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Found no receiver for incoming [",
						cmdHashHandlerName2,
						"] on ",
						base.gameObject,
						",  the server and client should have the same NetworkBehaviour instances [netId=",
						netId,
						"]."
					}));
				}
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out obj))
			{
				string cmdHashHandlerName3 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"SyncEvent [",
						cmdHashHandlerName3,
						"] handler not found [netId=",
						netId,
						"]"
					}));
				}
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void HandleSyncList(int cmdHash, NetworkReader reader)
		{
			Type invokeClass;
			NetworkBehaviour.CmdDelegate cmdDelegate;
			NetworkBehaviour obj;
			if (base.gameObject == null)
			{
				string cmdHashHandlerName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"SyncList [",
						cmdHashHandlerName,
						"] received for deleted object [netId=",
						netId,
						"]"
					}));
				}
			}
			else if (!NetworkBehaviour.GetInvokerForHashSyncList(cmdHash, out invokeClass, out cmdDelegate))
			{
				string cmdHashHandlerName2 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Found no receiver for incoming [",
						cmdHashHandlerName2,
						"] on ",
						base.gameObject,
						",  the server and client should have the same NetworkBehaviour instances [netId=",
						netId,
						"]."
					}));
				}
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out obj))
			{
				string cmdHashHandlerName3 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"SyncList [",
						cmdHashHandlerName3,
						"] handler not found [netId=",
						netId,
						"]"
					}));
				}
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void HandleCommand(int cmdHash, NetworkReader reader)
		{
			Type invokeClass;
			NetworkBehaviour.CmdDelegate cmdDelegate;
			NetworkBehaviour obj;
			if (base.gameObject == null)
			{
				string cmdHashHandlerName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"Command [",
						cmdHashHandlerName,
						"] received for deleted object [netId=",
						netId,
						"]"
					}));
				}
			}
			else if (!NetworkBehaviour.GetInvokerForHashCommand(cmdHash, out invokeClass, out cmdDelegate))
			{
				string cmdHashHandlerName2 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Found no receiver for incoming [",
						cmdHashHandlerName2,
						"] on ",
						base.gameObject,
						",  the server and client should have the same NetworkBehaviour instances [netId=",
						netId,
						"]."
					}));
				}
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out obj))
			{
				string cmdHashHandlerName3 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"Command [",
						cmdHashHandlerName3,
						"] handler not found [netId=",
						netId,
						"]"
					}));
				}
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void HandleRPC(int cmdHash, NetworkReader reader)
		{
			Type invokeClass;
			NetworkBehaviour.CmdDelegate cmdDelegate;
			NetworkBehaviour obj;
			if (base.gameObject == null)
			{
				string cmdHashHandlerName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"ClientRpc [",
						cmdHashHandlerName,
						"] received for deleted object [netId=",
						netId,
						"]"
					}));
				}
			}
			else if (!NetworkBehaviour.GetInvokerForHashClientRpc(cmdHash, out invokeClass, out cmdDelegate))
			{
				string cmdHashHandlerName2 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Found no receiver for incoming [",
						cmdHashHandlerName2,
						"] on ",
						base.gameObject,
						",  the server and client should have the same NetworkBehaviour instances [netId=",
						netId,
						"]."
					}));
				}
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out obj))
			{
				string cmdHashHandlerName3 = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				if (LogFilter.logWarn)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"ClientRpc [",
						cmdHashHandlerName3,
						"] handler not found [netId=",
						netId,
						"]"
					}));
				}
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void UNetUpdate()
		{
			uint num = 0U;
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				int dirtyChannel = networkBehaviour.GetDirtyChannel();
				if (dirtyChannel != -1)
				{
					num |= 1U << dirtyChannel;
				}
			}
			if (num != 0U)
			{
				int j = 0;
				while (j < NetworkServer.numChannels)
				{
					if ((num & 1U << j) != 0U)
					{
						NetworkIdentity.s_UpdateWriter.StartMessage(8);
						NetworkIdentity.s_UpdateWriter.Write(netId);
						bool flag = false;
						for (int k = 0; k < m_NetworkBehaviours.Length; k++)
						{
							short position = NetworkIdentity.s_UpdateWriter.Position;
							NetworkBehaviour networkBehaviour2 = m_NetworkBehaviours[k];
							if (networkBehaviour2.GetDirtyChannel() != j)
							{
								networkBehaviour2.OnSerialize(NetworkIdentity.s_UpdateWriter, false);
							}
							else
							{
								if (networkBehaviour2.OnSerialize(NetworkIdentity.s_UpdateWriter, false))
								{
									networkBehaviour2.ClearAllDirtyBits();
									flag = true;
								}
								if (NetworkIdentity.s_UpdateWriter.Position - position > (short)NetworkServer.maxPacketSize)
								{
									if (LogFilter.logWarn)
									{
										Debug.LogWarning(string.Concat(new object[]
										{
											"Large state update of ",
											(int)(NetworkIdentity.s_UpdateWriter.Position - position),
											" bytes for netId:",
											netId,
											" from script:",
											networkBehaviour2
										}));
									}
								}
							}
						}
						if (flag)
						{
							NetworkIdentity.s_UpdateWriter.FinishMessage();
							NetworkServer.SendWriterToReady(base.gameObject, NetworkIdentity.s_UpdateWriter, j);
						}
					}
					IL_197:
					j++;
					continue;
					goto IL_197;
				}
			}
		}

		internal void OnUpdateVars(NetworkReader reader, bool initialState)
		{
			if (initialState && m_NetworkBehaviours == null)
			{
				m_NetworkBehaviours = base.GetComponents<NetworkBehaviour>();
			}
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				networkBehaviour.OnDeserialize(reader, initialState);
			}
		}

		internal void SetLocalPlayer(short localPlayerControllerId)
		{
			m_IsLocalPlayer = true;
			m_PlayerId = localPlayerControllerId;
			bool hasAuthority = m_HasAuthority;
			if (localPlayerAuthority)
			{
				m_HasAuthority = true;
			}
			for (int i = 0; i < m_NetworkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
				networkBehaviour.OnStartLocalPlayer();
				if (localPlayerAuthority && !hasAuthority)
				{
					networkBehaviour.OnStartAuthority();
				}
			}
		}

		internal void SetConnectionToServer(NetworkConnection conn)
		{
			m_ConnectionToServer = conn;
		}

		internal void SetConnectionToClient(NetworkConnection conn, short newPlayerControllerId)
		{
			m_PlayerId = newPlayerControllerId;
			m_ConnectionToClient = conn;
		}

		internal void OnNetworkDestroy()
		{
			int num = 0;
			while (m_NetworkBehaviours != null && num < m_NetworkBehaviours.Length)
			{
				NetworkBehaviour networkBehaviour = m_NetworkBehaviours[num];
				networkBehaviour.OnNetworkDestroy();
				num++;
			}
			m_IsServer = false;
		}

		internal void ClearObservers()
		{
			if (m_Observers != null)
			{
				int count = m_Observers.Count;
				for (int i = 0; i < count; i++)
				{
					NetworkConnection networkConnection = m_Observers[i];
					networkConnection.RemoveFromVisList(this, true);
				}
				m_Observers.Clear();
				m_ObserverConnections.Clear();
			}
		}

		internal void AddObserver(NetworkConnection conn)
		{
			if (m_Observers == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("AddObserver for " + base.gameObject + " observer list is null");
				}
			}
			else if (m_ObserverConnections.Contains(conn.connectionId))
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Duplicate observer ",
						conn.address,
						" added for ",
						base.gameObject
					}));
				}
			}
			else
			{
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Added observer ",
						conn.address,
						" added for ",
						base.gameObject
					}));
				}
				m_Observers.Add(conn);
				m_ObserverConnections.Add(conn.connectionId);
				conn.AddToVisList(this);
			}
		}

		internal void RemoveObserver(NetworkConnection conn)
		{
			if (m_Observers != null)
			{
				m_Observers.Remove(conn);
				m_ObserverConnections.Remove(conn.connectionId);
				conn.RemoveFromVisList(this, false);
			}
		}

		public void RebuildObservers(bool initialize)
		{
			if (m_Observers != null)
			{
				bool flag = false;
				bool flag2 = false;
				HashSet<NetworkConnection> hashSet = new HashSet<NetworkConnection>();
				HashSet<NetworkConnection> hashSet2 = new HashSet<NetworkConnection>(m_Observers);
				for (int i = 0; i < m_NetworkBehaviours.Length; i++)
				{
					NetworkBehaviour networkBehaviour = m_NetworkBehaviours[i];
					flag2 |= networkBehaviour.OnRebuildObservers(hashSet, initialize);
				}
				if (!flag2)
				{
					if (initialize)
					{
						for (int j = 0; j < NetworkServer.connections.Count; j++)
						{
							NetworkConnection networkConnection = NetworkServer.connections[j];
							if (networkConnection != null)
							{
								if (networkConnection.isReady)
								{
									AddObserver(networkConnection);
								}
							}
						}
						for (int k = 0; k < NetworkServer.localConnections.Count; k++)
						{
							NetworkConnection networkConnection2 = NetworkServer.localConnections[k];
							if (networkConnection2 != null)
							{
								if (networkConnection2.isReady)
								{
									AddObserver(networkConnection2);
								}
							}
						}
					}
				}
				else
				{
					foreach (NetworkConnection networkConnection3 in hashSet)
					{
						if (networkConnection3 != null)
						{
							if (!networkConnection3.isReady)
							{
								if (LogFilter.logWarn)
								{
									Debug.LogWarning(string.Concat(new object[]
									{
										"Observer is not ready for ",
										base.gameObject,
										" ",
										networkConnection3
									}));
								}
							}
							else if (initialize || !hashSet2.Contains(networkConnection3))
							{
								networkConnection3.AddToVisList(this);
								if (LogFilter.logDebug)
								{
									Debug.Log(string.Concat(new object[]
									{
										"New Observer for ",
										base.gameObject,
										" ",
										networkConnection3
									}));
								}
								flag = true;
							}
						}
					}
					foreach (NetworkConnection networkConnection4 in hashSet2)
					{
						if (!hashSet.Contains(networkConnection4))
						{
							networkConnection4.RemoveFromVisList(this, false);
							if (LogFilter.logDebug)
							{
								Debug.Log(string.Concat(new object[]
								{
									"Removed Observer for ",
									base.gameObject,
									" ",
									networkConnection4
								}));
							}
							flag = true;
						}
					}
					if (initialize)
					{
						for (int l = 0; l < NetworkServer.localConnections.Count; l++)
						{
							if (!hashSet.Contains(NetworkServer.localConnections[l]))
							{
								OnSetLocalVisibility(false);
							}
						}
					}
					if (flag)
					{
						m_Observers = new List<NetworkConnection>(hashSet);
						m_ObserverConnections.Clear();
						for (int m = 0; m < m_Observers.Count; m++)
						{
							m_ObserverConnections.Add(m_Observers[m].connectionId);
						}
					}
				}
			}
		}

		public bool RemoveClientAuthority(NetworkConnection conn)
		{
			bool result;
			if (!isServer)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RemoveClientAuthority can only be call on the server for spawned objects.");
				}
				result = false;
			}
			else if (connectionToClient != null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RemoveClientAuthority cannot remove authority for a player object");
				}
				result = false;
			}
			else if (m_ClientAuthorityOwner == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RemoveClientAuthority for " + base.gameObject + " has no clientAuthority owner.");
				}
				result = false;
			}
			else if (m_ClientAuthorityOwner != conn)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RemoveClientAuthority for " + base.gameObject + " has different owner.");
				}
				result = false;
			}
			else
			{
				m_ClientAuthorityOwner.RemoveOwnedObject(this);
				m_ClientAuthorityOwner = null;
				ForceAuthority(true);
				conn.Send(15, new ClientAuthorityMessage
				{
					netId = netId,
					authority = false
				});
				if (NetworkIdentity.clientAuthorityCallback != null)
				{
					NetworkIdentity.clientAuthorityCallback(conn, this, false);
				}
				result = true;
			}
			return result;
		}

		public bool AssignClientAuthority(NetworkConnection conn)
		{
			bool result;
			if (!isServer)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("AssignClientAuthority can only be call on the server for spawned objects.");
				}
				result = false;
			}
			else if (!localPlayerAuthority)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("AssignClientAuthority can only be used for NetworkIdentity component with LocalPlayerAuthority set.");
				}
				result = false;
			}
			else if (m_ClientAuthorityOwner != null && conn != m_ClientAuthorityOwner)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("AssignClientAuthority for " + base.gameObject + " already has an owner. Use RemoveClientAuthority() first.");
				}
				result = false;
			}
			else if (conn == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("AssignClientAuthority for " + base.gameObject + " owner cannot be null. Use RemoveClientAuthority() instead.");
				}
				result = false;
			}
			else
			{
				m_ClientAuthorityOwner = conn;
				m_ClientAuthorityOwner.AddOwnedObject(this);
				ForceAuthority(false);
				conn.Send(15, new ClientAuthorityMessage
				{
					netId = netId,
					authority = true
				});
				if (NetworkIdentity.clientAuthorityCallback != null)
				{
					NetworkIdentity.clientAuthorityCallback(conn, this, true);
				}
				result = true;
			}
			return result;
		}

		internal void MarkForReset()
		{
			m_Reset = true;
		}

		internal void Reset()
		{
			if (m_Reset)
			{
				m_Reset = false;
				m_IsServer = false;
				m_IsClient = false;
				m_HasAuthority = false;
				m_NetId = NetworkInstanceId.Zero;
				m_IsLocalPlayer = false;
				m_ConnectionToServer = null;
				m_ConnectionToClient = null;
				m_PlayerId = -1;
				m_NetworkBehaviours = null;
				ClearObservers();
				m_ClientAuthorityOwner = null;
			}
		}

		internal static void UNetStaticUpdate()
		{
			NetworkServer.Update();
			NetworkClient.UpdateClients();
			NetworkManager.UpdateScene();
		}

		[SerializeField]
		private NetworkSceneId m_SceneId;

		[SerializeField]
		private NetworkHash128 m_AssetId;

		[SerializeField]
		private bool m_ServerOnly;

		[SerializeField]
		private bool m_LocalPlayerAuthority;

		private bool m_IsClient;

		private bool m_IsServer;

		private bool m_HasAuthority;

		private NetworkInstanceId m_NetId;

		private bool m_IsLocalPlayer;

		private NetworkConnection m_ConnectionToServer;

		private NetworkConnection m_ConnectionToClient;

		private short m_PlayerId = -1;

		private NetworkBehaviour[] m_NetworkBehaviours;

		private HashSet<int> m_ObserverConnections;

		private List<NetworkConnection> m_Observers;

		private NetworkConnection m_ClientAuthorityOwner;

		private bool m_Reset = false;

		private static uint s_NextNetworkId = 1U;

		private static NetworkWriter s_UpdateWriter = new NetworkWriter();

		public static NetworkIdentity.ClientAuthorityCallback clientAuthorityCallback;

		public delegate void ClientAuthorityCallback(NetworkConnection conn, NetworkIdentity uv, bool authorityState);
	}
}
