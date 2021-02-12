using OWML.Logging;
using QuantumUNET.Messages;
using QuantumUNET.Transport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET.Components
{
	public sealed class QSBNetworkIdentity : MonoBehaviour
	{
		public bool IsClient { get; private set; }
		public bool IsServer => m_IsServer && QSBNetworkServer.active && m_IsServer;
		public bool HasAuthority { get; private set; }
		public NetworkInstanceId NetId { get; private set; }
		public NetworkSceneId SceneId => m_SceneId;
		public QSBNetworkConnection ClientAuthorityOwner { get; private set; }
		public NetworkHash128 AssetId => m_AssetId;
		public bool IsLocalPlayer { get; private set; }
		public short PlayerControllerId { get; private set; } = -1;
		public QSBNetworkConnection ConnectionToServer { get; private set; }
		public QSBNetworkConnection ConnectionToClient { get; private set; }
		public QSBNetworkIdentity RootIdentity { get; private set; }
		public List<QSBNetworkIdentity> SubIdentities { get; private set; } = new List<QSBNetworkIdentity>();

		public bool ServerOnly
		{
			get => m_ServerOnly;
			set => m_ServerOnly = value;
		}

		public bool LocalPlayerAuthority
		{
			get => m_LocalPlayerAuthority;
			set => m_LocalPlayerAuthority = value;
		}

		public void SetRootIdentity(QSBNetworkIdentity newRoot)
		{
			if (RootIdentity != null)
			{
				RootIdentity.RemoveSubIdentity(this);
			}
			RootIdentity = newRoot;
			RootIdentity.AddSubIndentity(this);
		}

		internal void AddSubIndentity(QSBNetworkIdentity identityToAdd)
			=> SubIdentities.Add(identityToAdd);

		internal void RemoveSubIdentity(QSBNetworkIdentity identityToRemove)
			=> SubIdentities.Remove(identityToRemove);

		internal void SetDynamicAssetId(NetworkHash128 newAssetId)
		{
			if (!m_AssetId.IsValid() || m_AssetId.Equals(newAssetId))
			{
				m_AssetId = newAssetId;
			}
			else
			{
				Debug.LogWarning($"SetDynamicAssetId object already has an assetId <{m_AssetId}>");
			}
		}

		internal void SetClientOwner(QSBNetworkConnection conn)
		{
			if (ClientAuthorityOwner != null)
			{
				Debug.LogError("SetClientOwner m_ClientAuthorityOwner already set!");
			}
			ClientAuthorityOwner = conn;
			ClientAuthorityOwner.AddOwnedObject(this);
		}

		internal void ClearClientOwner() => ClientAuthorityOwner = null;

		internal void ForceAuthority(bool authority)
		{
			if (HasAuthority != authority)
			{
				HasAuthority = authority;
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

		public ReadOnlyCollection<QSBNetworkConnection> Observers
		{
			get
			{
				ReadOnlyCollection<QSBNetworkConnection> result;
				if (m_Observers == null)
				{
					result = null;
				}
				else
				{
					result = new ReadOnlyCollection<QSBNetworkConnection>(m_Observers);
				}
				return result;
			}
		}

		internal static NetworkInstanceId GetNextNetworkId()
		{
			var value = s_NextNetworkId;
			s_NextNetworkId += 1U;
			return new NetworkInstanceId(value);
		}

		private void CacheBehaviours()
		{
			if (m_NetworkBehaviours == null)
			{
				m_NetworkBehaviours = GetComponents<QSBNetworkBehaviour>();
			}
		}

		internal static void AddNetworkId(uint id)
		{
			if (id >= s_NextNetworkId)
			{
				s_NextNetworkId = id + 1U;
			}
		}

		internal void SetNetworkInstanceId(NetworkInstanceId newNetId)
		{
			NetId = newNetId;
			if (newNetId.Value == 0U)
			{
				m_IsServer = false;
			}
		}

		public void ForceSceneId(int newSceneId) => m_SceneId = new NetworkSceneId((uint)newSceneId);

		internal void UpdateClientServer(bool isClientFlag, bool isServerFlag)
		{
			IsClient = IsClient || isClientFlag;
			m_IsServer = m_IsServer || isServerFlag;
		}

		internal void SetNotLocalPlayer()
		{
			IsLocalPlayer = false;
			if (!QSBNetworkServer.active || !QSBNetworkServer.localClientActive)
			{
				HasAuthority = false;
			}
		}

		internal void RemoveObserverInternal(QSBNetworkConnection conn)
		{
			if (m_Observers != null)
			{
				m_Observers.Remove(conn);
				m_ObserverConnections.Remove(conn.connectionId);
			}
		}

		public void OnDestroy()
		{
			if (m_IsServer && QSBNetworkServer.active)
			{
				QSBNetworkServer.Destroy(gameObject);
			}
		}

		internal void OnStartServer(bool allowNonZeroNetId)
		{
			if (!m_IsServer)
			{
				m_IsServer = true;
				if (m_LocalPlayerAuthority)
				{
					HasAuthority = false;
				}
				else
				{
					HasAuthority = true;
				}
				m_Observers = new List<QSBNetworkConnection>();
				m_ObserverConnections = new HashSet<int>();
				CacheBehaviours();
				if (NetId.IsEmpty())
				{
					NetId = GetNextNetworkId();
				}
				else if (!allowNonZeroNetId)
				{
					ModConsole.OwmlConsole.WriteLine($"Object has non-zero netId {NetId} for {gameObject}");
					return;
				}
				QSBNetworkServer.instance.SetLocalObjectOnServer(NetId, gameObject);
				foreach (var networkBehaviour in m_NetworkBehaviours)
				{
					try
					{
						networkBehaviour.OnStartServer();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Exception in OnStartServer:{ex.Message} {ex.StackTrace}");
					}
				}
				if (QSBNetworkClient.active && QSBNetworkServer.localClientActive)
				{
					QSBClientScene.SetLocalObject(NetId, gameObject);
					OnStartClient();
				}
				if (HasAuthority)
				{
					OnStartAuthority();
				}
			}
		}

		internal void OnStartClient()
		{
			if (!IsClient)
			{
				IsClient = true;
			}
			CacheBehaviours();
			Debug.Log($"OnStartClient {gameObject} GUID:{NetId} localPlayerAuthority:{LocalPlayerAuthority}");
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				try
				{
					networkBehaviour.PreStartClient();
					networkBehaviour.OnStartClient();
				}
				catch (Exception ex)
				{
					Debug.LogError($"Exception in OnStartClient:{ex.Message} {ex.StackTrace}");
				}
			}
		}

		internal void OnStartAuthority()
		{
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				try
				{
					networkBehaviour.OnStartAuthority();
				}
				catch (Exception ex)
				{
					Debug.LogError($"Exception in OnStartAuthority:{ex.Message} {ex.StackTrace}");
				}
			}
		}

		internal void OnStopAuthority()
		{
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				try
				{
					networkBehaviour.OnStopAuthority();
				}
				catch (Exception ex)
				{
					Debug.LogError($"Exception in OnStopAuthority:{ex.Message} {ex.StackTrace}");
				}
			}
		}

		internal void OnSetLocalVisibility(bool vis)
		{
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				try
				{
					networkBehaviour.OnSetLocalVisibility(vis);
				}
				catch (Exception ex)
				{
					Debug.LogError($"Exception in OnSetLocalVisibility:{ex.Message} {ex.StackTrace}");
				}
			}
		}

		internal bool OnCheckObserver(QSBNetworkConnection conn)
		{
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				try
				{
					if (!networkBehaviour.OnCheckObserver(conn))
					{
						return false;
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Exception in OnCheckObserver:{ex.Message} {ex.StackTrace}");
				}
			}

			return true;
		}

		internal void UNetSerializeAllVars(QSBNetworkWriter writer)
		{
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				networkBehaviour.OnSerialize(writer, true);
			}
		}

		internal void HandleClientAuthority(bool authority)
		{
			if (!LocalPlayerAuthority)
			{
				Debug.LogError($"HandleClientAuthority {gameObject} does not have localPlayerAuthority");
			}
			else
			{
				ForceAuthority(authority);
			}
		}

		private bool GetInvokeComponent(int cmdHash, Type invokeClass, out QSBNetworkBehaviour invokeComponent)
		{
			QSBNetworkBehaviour networkBehaviour = null;
			foreach (var networkBehaviour2 in m_NetworkBehaviours)
			{
				if (networkBehaviour2.GetType() == invokeClass || networkBehaviour2.GetType().IsSubclassOf(invokeClass))
				{
					networkBehaviour = networkBehaviour2;
					break;
				}
			}
			bool result;
			if (networkBehaviour == null)
			{
				var cmdHashHandlerName = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogError(
					$"Found no behaviour for incoming [{cmdHashHandlerName}] on {gameObject},  the server and client should have the same NetworkBehaviour instances [netId={NetId}].");
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

		internal void HandleSyncEvent(int cmdHash, QSBNetworkReader reader)
		{
			if (gameObject == null)
			{
				var cmdHashHandlerName = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"SyncEvent [{cmdHashHandlerName}] received for deleted object [netId={NetId}]");
			}
			else if (!QSBNetworkBehaviour.GetInvokerForHashSyncEvent(cmdHash, out var invokeClass, out var cmdDelegate))
			{
				var cmdHashHandlerName2 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogError(
					$"Found no receiver for incoming [{cmdHashHandlerName2}] on {gameObject},  the server and client should have the same NetworkBehaviour instances [netId={NetId}].");
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out var obj))
			{
				var cmdHashHandlerName3 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"SyncEvent [{cmdHashHandlerName3}] handler not found [netId={NetId}]");
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void HandleSyncList(int cmdHash, QSBNetworkReader reader)
		{
			if (gameObject == null)
			{
				var cmdHashHandlerName = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"SyncList [{cmdHashHandlerName}] received for deleted object [netId={NetId}]");
			}
			else if (!QSBNetworkBehaviour.GetInvokerForHashSyncList(cmdHash, out var invokeClass, out var cmdDelegate))
			{
				var cmdHashHandlerName2 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogError(
					$"Found no receiver for incoming [{cmdHashHandlerName2}] on {gameObject},  the server and client should have the same NetworkBehaviour instances [netId={NetId}].");
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out var obj))
			{
				var cmdHashHandlerName3 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"SyncList [{cmdHashHandlerName3}] handler not found [netId={NetId}]");
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void HandleCommand(int cmdHash, QSBNetworkReader reader)
		{
			if (gameObject == null)
			{
				var cmdHashHandlerName = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"Command [{cmdHashHandlerName}] received for deleted object [netId={NetId}]");
			}
			else if (!QSBNetworkBehaviour.GetInvokerForHashCommand(cmdHash, out var invokeClass, out var cmdDelegate))
			{
				var cmdHashHandlerName2 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogError(
					$"Found no receiver for incoming [{cmdHashHandlerName2}] on {gameObject},  the server and client should have the same NetworkBehaviour instances [netId={NetId}].");
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out var obj))
			{
				var cmdHashHandlerName3 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"Command [{cmdHashHandlerName3}] handler not found [netId={NetId}]");
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void HandleRPC(int cmdHash, QSBNetworkReader reader)
		{
			if (gameObject == null)
			{
				var cmdHashHandlerName = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"ClientRpc [{cmdHashHandlerName}] received for deleted object [netId={NetId}]");
			}
			else if (!QSBNetworkBehaviour.GetInvokerForHashClientRpc(cmdHash, out var invokeClass, out var cmdDelegate))
			{
				var cmdHashHandlerName2 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogError(
					$"Found no receiver for incoming [{cmdHashHandlerName2}] on {gameObject},  the server and client should have the same NetworkBehaviour instances [netId={NetId}].");
			}
			else if (!GetInvokeComponent(cmdHash, invokeClass, out var obj))
			{
				var cmdHashHandlerName3 = QSBNetworkBehaviour.GetCmdHashHandlerName(cmdHash);
				Debug.LogWarning($"ClientRpc [{cmdHashHandlerName3}] handler not found [netId={NetId}]");
			}
			else
			{
				cmdDelegate(obj, reader);
			}
		}

		internal void UNetUpdate()
		{
			var num = 0U;
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				var dirtyChannel = networkBehaviour.GetDirtyChannel();
				if (dirtyChannel != -1)
				{
					num |= 1U << dirtyChannel;
				}
			}
			if (num != 0U)
			{
				var j = 0;
				while (j < QSBNetworkServer.numChannels)
				{
					if ((num & (1U << j)) != 0U)
					{
						s_UpdateWriter.StartMessage(8);
						s_UpdateWriter.Write(NetId);
						var flag = false;
						foreach (var networkBehaviour in m_NetworkBehaviours)
						{
							var position = s_UpdateWriter.Position;
							if (networkBehaviour.GetDirtyChannel() != j)
							{
								networkBehaviour.OnSerialize(s_UpdateWriter, false);
							}
							else
							{
								if (networkBehaviour.OnSerialize(s_UpdateWriter, false))
								{
									networkBehaviour.ClearAllDirtyBits();
									flag = true;
								}
								var maxPacketSize = QSBNetworkServer.maxPacketSize;
								if (s_UpdateWriter.Position - position > maxPacketSize)
								{
									Debug.LogWarning(
										$"Large state update of {s_UpdateWriter.Position - position} bytes for netId:{NetId} from script:{networkBehaviour}");
								}
							}
						}
						if (flag)
						{
							s_UpdateWriter.FinishMessage();
							QSBNetworkServer.SendWriterToReady(gameObject, s_UpdateWriter, j);
						}
					}
					j++;
				}
			}
		}

		internal void OnUpdateVars(QSBNetworkReader reader, bool initialState)
		{
			if (initialState && m_NetworkBehaviours == null)
			{
				m_NetworkBehaviours = GetComponents<QSBNetworkBehaviour>();
			}

			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				networkBehaviour.OnDeserialize(reader, initialState);
			}
		}

		internal void SetLocalPlayer(short localPlayerControllerId)
		{
			IsLocalPlayer = true;
			PlayerControllerId = localPlayerControllerId;
			var hasAuthority = HasAuthority;
			if (LocalPlayerAuthority)
			{
				HasAuthority = true;
			}
			foreach (var networkBehaviour in m_NetworkBehaviours)
			{
				networkBehaviour.OnStartLocalPlayer();
				if (LocalPlayerAuthority && !hasAuthority)
				{
					networkBehaviour.OnStartAuthority();
				}
			}
		}

		internal void SetConnectionToServer(QSBNetworkConnection conn) => ConnectionToServer = conn;

		internal void SetConnectionToClient(QSBNetworkConnection conn, short newPlayerControllerId)
		{
			PlayerControllerId = newPlayerControllerId;
			ConnectionToClient = conn;
		}

		internal void OnNetworkDestroy()
		{
			var num = 0;
			while (m_NetworkBehaviours != null && num < m_NetworkBehaviours.Length)
			{
				var networkBehaviour = m_NetworkBehaviours[num];
				networkBehaviour.OnNetworkDestroy();
				num++;
			}
			m_IsServer = false;
		}

		internal void ClearObservers()
		{
			if (m_Observers != null)
			{
				var count = m_Observers.Count;
				for (var i = 0; i < count; i++)
				{
					var networkConnection = m_Observers[i];
					networkConnection.RemoveFromVisList(this, true);
				}
				m_Observers.Clear();
				m_ObserverConnections.Clear();
			}
		}

		internal void AddObserver(QSBNetworkConnection conn)
		{
			if (m_Observers == null)
			{
				Debug.LogError($"AddObserver for {gameObject} observer list is null");
			}
			else if (m_ObserverConnections.Contains(conn.connectionId))
			{
				Debug.Log($"Duplicate observer {conn.address} added for {gameObject}");
			}
			else
			{
				Debug.Log($"Added observer {conn.address} added for {gameObject}");
				m_Observers.Add(conn);
				m_ObserverConnections.Add(conn.connectionId);
				conn.AddToVisList(this);
			}
		}

		internal void RemoveObserver(QSBNetworkConnection conn)
		{
			if (m_Observers != null)
			{
				m_Observers.Remove(conn);
				m_ObserverConnections.Remove(conn.connectionId);
				conn.RemoveFromVisList(this, true);
			}
		}

		public void RebuildObservers(bool initialize)
		{
			if (m_Observers != null)
			{
				var flag = false;
				var flag2 = false;
				var hashSet = new HashSet<QSBNetworkConnection>();
				var hashSet2 = new HashSet<QSBNetworkConnection>(m_Observers);
				foreach (var networkBehaviour in m_NetworkBehaviours)
				{
					flag2 |= networkBehaviour.OnRebuildObservers(hashSet, initialize);
				}
				if (!flag2)
				{
					if (initialize)
					{
						foreach (var networkConnection in QSBNetworkServer.connections)
						{
							if (networkConnection != null)
							{
								if (networkConnection.isReady)
								{
									AddObserver(networkConnection);
								}
							}
						}

						foreach (var networkConnection2 in QSBNetworkServer.localConnections)
						{
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
					foreach (var networkConnection3 in hashSet)
					{
						if (networkConnection3 != null)
						{
							if (!networkConnection3.isReady)
							{
								Debug.LogWarning($"Observer is not ready for {gameObject} {networkConnection3}");
							}
							else if (initialize || !hashSet2.Contains(networkConnection3))
							{
								networkConnection3.AddToVisList(this);
								Debug.Log($"New Observer for {gameObject} {networkConnection3}");
								flag = true;
							}
						}
					}
					foreach (var networkConnection4 in hashSet2)
					{
						if (!hashSet.Contains(networkConnection4))
						{
							networkConnection4.RemoveFromVisList(this, true);
							Debug.Log($"Removed Observer for {gameObject} {networkConnection4}");
							flag = true;
						}
					}
					if (initialize)
					{
						foreach (var connection in QSBNetworkServer.localConnections)
						{
							if (!hashSet.Contains(connection))
							{
								OnSetLocalVisibility(false);
							}
						}
					}
					if (flag)
					{
						m_Observers = new List<QSBNetworkConnection>(hashSet);
						m_ObserverConnections.Clear();
						foreach (var observer in m_Observers)
						{
							m_ObserverConnections.Add(observer.connectionId);
						}
					}
				}
			}
		}

		public bool RemoveClientAuthority(QSBNetworkConnection conn)
		{
			if (!IsServer)
			{
				ModConsole.OwmlConsole.WriteLine($"Warning - Cannot remove authority on client-side. (NetId:{NetId}, Gameobject:{gameObject.name})");
				return false;
			}
			else if (ConnectionToClient != null)
			{
				Debug.LogError("RemoveClientAuthority cannot remove authority for a player object");
				return false;
			}
			else if (ClientAuthorityOwner == null)
			{
				Debug.LogError($"RemoveClientAuthority for {gameObject} has no clientAuthority owner.");
				return false;
			}
			else if (ClientAuthorityOwner != conn)
			{
				Debug.LogError($"RemoveClientAuthority for {gameObject} has different owner.");
				return false;
			}
			ClientAuthorityOwner.RemoveOwnedObject(this);
			ClientAuthorityOwner = null;
			ForceAuthority(true);
			conn.Send(15, new QSBClientAuthorityMessage
			{
				netId = NetId,
				authority = false
			});
			clientAuthorityCallback?.Invoke(conn, this, false);
			return true;
		}

		public bool AssignClientAuthority(QSBNetworkConnection conn)
		{
			if (!IsServer)
			{
				ModConsole.OwmlConsole.WriteLine($"Warning - Cannot assign authority on client-side. (NetId:{NetId}, Gameobject:{gameObject.name})");
				return false;
			}
			else if (!LocalPlayerAuthority)
			{
				ModConsole.OwmlConsole.WriteLine($"Warning - Cannot assign authority on object without LocalPlayerAuthority. (NetId:{NetId}, Gameobject:{gameObject.name})");
				return false;
			}
			else if (ClientAuthorityOwner != null && conn != ClientAuthorityOwner)
			{
				ModConsole.OwmlConsole.WriteLine(
					$"AssignClientAuthority for {gameObject} already has an owner. Use RemoveClientAuthority() first.");
				return false;
			}
			else if (conn == null)
			{
				ModConsole.OwmlConsole.WriteLine(
					$"AssignClientAuthority for {gameObject} owner cannot be null. Use RemoveClientAuthority() instead.");
				return false;
			}
			ClientAuthorityOwner = conn;
			ClientAuthorityOwner.AddOwnedObject(this);

			ForceAuthority(false);
			conn.Send(15, new QSBClientAuthorityMessage
			{
				netId = NetId,
				authority = true
			});
			clientAuthorityCallback?.Invoke(conn, this, true);
			return true;
		}

		internal void MarkForReset() => m_Reset = true;

		internal void Reset()
		{
			if (m_Reset)
			{
				m_Reset = false;
				m_IsServer = false;
				IsClient = false;
				HasAuthority = false;
				NetId = (NetworkInstanceId)typeof(NetworkInstanceId).GetField("Zero", System.Reflection.BindingFlags.Static).GetValue(null);
				IsLocalPlayer = false;
				ConnectionToServer = null;
				ConnectionToClient = null;
				PlayerControllerId = -1;
				m_NetworkBehaviours = null;
				ClearObservers();
				ClientAuthorityOwner = null;
			}
		}

		public static void UNetStaticUpdate()
		{
			QSBNetworkServer.Update();
			QSBNetworkClient.UpdateClients();
			QSBNetworkManagerUNET.UpdateScene();
		}

		[SerializeField]
		private NetworkSceneId m_SceneId;

		[SerializeField]
		private NetworkHash128 m_AssetId;

		[SerializeField]
		private bool m_ServerOnly;

		[SerializeField]
		private bool m_LocalPlayerAuthority;

		private bool m_IsServer;

		private QSBNetworkBehaviour[] m_NetworkBehaviours;

		private HashSet<int> m_ObserverConnections;

		private List<QSBNetworkConnection> m_Observers;

		private bool m_Reset;

		private static uint s_NextNetworkId = 1U;

		private static readonly QSBNetworkWriter s_UpdateWriter = new QSBNetworkWriter();

		public static ClientAuthorityCallback clientAuthorityCallback;

		public delegate void ClientAuthorityCallback(QSBNetworkConnection conn, QSBNetworkIdentity uv, bool authorityState);
	}
}