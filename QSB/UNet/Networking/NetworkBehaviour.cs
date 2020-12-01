using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace QSB.UNet.Networking
{
	[AddComponentMenu("")]
	[RequireComponent(typeof(NetworkIdentity))]
	public class NetworkBehaviour : MonoBehaviour
	{
		public bool localPlayerAuthority
		{
			get
			{
				return myView.localPlayerAuthority;
			}
		}

		public bool isServer
		{
			get
			{
				return myView.isServer;
			}
		}

		public bool isClient
		{
			get
			{
				return myView.isClient;
			}
		}

		public bool isLocalPlayer
		{
			get
			{
				return myView.isLocalPlayer;
			}
		}

		public bool hasAuthority
		{
			get
			{
				return myView.hasAuthority;
			}
		}

		public NetworkInstanceId netId
		{
			get
			{
				return myView.netId;
			}
		}

		public NetworkConnection connectionToServer
		{
			get
			{
				return myView.connectionToServer;
			}
		}

		public NetworkConnection connectionToClient
		{
			get
			{
				return myView.connectionToClient;
			}
		}

		public short playerControllerId
		{
			get
			{
				return myView.playerControllerId;
			}
		}

		protected uint syncVarDirtyBits
		{
			get
			{
				return m_SyncVarDirtyBits;
			}
		}

		protected bool syncVarHookGuard
		{
			get
			{
				return m_SyncVarGuard;
			}
			set
			{
				m_SyncVarGuard = value;
			}
		}

		internal NetworkIdentity netIdentity
		{
			get
			{
				return myView;
			}
		}

		private NetworkIdentity myView
		{
			get
			{
				NetworkIdentity myView;
				if (m_MyView == null)
				{
					m_MyView = base.GetComponent<NetworkIdentity>();
					if (m_MyView == null)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("There is no NetworkIdentity on this object. Please add one.");
						}
					}
					myView = m_MyView;
				}
				else
				{
					myView = m_MyView;
				}
				return myView;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void SendCommandInternal(NetworkWriter writer, int channelId, string cmdName)
		{
			if (!isLocalPlayer && !hasAuthority)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("Trying to send command for object without authority.");
				}
			}
			else if (ClientScene.readyConnection == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Send command attempted with no client running [client=" + connectionToServer + "].");
				}
			}
			else
			{
				writer.FinishMessage();
				ClientScene.readyConnection.SendWriter(writer, channelId);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual bool InvokeCommand(int cmdHash, NetworkReader reader)
		{
			return InvokeCommandDelegate(cmdHash, reader);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void SendRPCInternal(NetworkWriter writer, int channelId, string rpcName)
		{
			if (!isServer)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("ClientRpc call on un-spawned object");
				}
			}
			else
			{
				writer.FinishMessage();
				NetworkServer.SendWriterToReady(base.gameObject, writer, channelId);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void SendTargetRPCInternal(NetworkConnection conn, NetworkWriter writer, int channelId, string rpcName)
		{
			if (!isServer)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("TargetRpc call on un-spawned object");
				}
			}
			else
			{
				writer.FinishMessage();
				conn.SendWriter(writer, channelId);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual bool InvokeRPC(int cmdHash, NetworkReader reader)
		{
			return InvokeRpcDelegate(cmdHash, reader);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void SendEventInternal(NetworkWriter writer, int channelId, string eventName)
		{
			if (!NetworkServer.active)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("SendEvent no server?");
				}
			}
			else
			{
				writer.FinishMessage();
				NetworkServer.SendWriterToReady(base.gameObject, writer, channelId);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual bool InvokeSyncEvent(int cmdHash, NetworkReader reader)
		{
			return InvokeSyncEventDelegate(cmdHash, reader);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual bool InvokeSyncList(int cmdHash, NetworkReader reader)
		{
			return InvokeSyncListDelegate(cmdHash, reader);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected static void RegisterCommandDelegate(Type invokeClass, int cmdHash, NetworkBehaviour.CmdDelegate func)
		{
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				NetworkBehaviour.Invoker invoker = new NetworkBehaviour.Invoker();
				invoker.invokeType = NetworkBehaviour.UNetInvokeType.Command;
				invoker.invokeClass = invokeClass;
				invoker.invokeFunction = func;
				NetworkBehaviour.s_CmdHandlerDelegates[cmdHash] = invoker;
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterCommandDelegate hash:",
						cmdHash,
						" ",
						func.GetMethodName()
					}));
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected static void RegisterRpcDelegate(Type invokeClass, int cmdHash, NetworkBehaviour.CmdDelegate func)
		{
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				NetworkBehaviour.Invoker invoker = new NetworkBehaviour.Invoker();
				invoker.invokeType = NetworkBehaviour.UNetInvokeType.ClientRpc;
				invoker.invokeClass = invokeClass;
				invoker.invokeFunction = func;
				NetworkBehaviour.s_CmdHandlerDelegates[cmdHash] = invoker;
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterRpcDelegate hash:",
						cmdHash,
						" ",
						func.GetMethodName()
					}));
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected static void RegisterEventDelegate(Type invokeClass, int cmdHash, NetworkBehaviour.CmdDelegate func)
		{
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				NetworkBehaviour.Invoker invoker = new NetworkBehaviour.Invoker();
				invoker.invokeType = NetworkBehaviour.UNetInvokeType.SyncEvent;
				invoker.invokeClass = invokeClass;
				invoker.invokeFunction = func;
				NetworkBehaviour.s_CmdHandlerDelegates[cmdHash] = invoker;
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterEventDelegate hash:",
						cmdHash,
						" ",
						func.GetMethodName()
					}));
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected static void RegisterSyncListDelegate(Type invokeClass, int cmdHash, NetworkBehaviour.CmdDelegate func)
		{
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				NetworkBehaviour.Invoker invoker = new NetworkBehaviour.Invoker();
				invoker.invokeType = NetworkBehaviour.UNetInvokeType.SyncList;
				invoker.invokeClass = invokeClass;
				invoker.invokeFunction = func;
				NetworkBehaviour.s_CmdHandlerDelegates[cmdHash] = invoker;
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterSyncListDelegate hash:",
						cmdHash,
						" ",
						func.GetMethodName()
					}));
				}
			}
		}

		internal static string GetInvoker(int cmdHash)
		{
			string result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = null;
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				result = invoker.DebugString();
			}
			return result;
		}

		internal static bool GetInvokerForHashCommand(int cmdHash, out Type invokeClass, out NetworkBehaviour.CmdDelegate invokeFunction)
		{
			return NetworkBehaviour.GetInvokerForHash(cmdHash, NetworkBehaviour.UNetInvokeType.Command, out invokeClass, out invokeFunction);
		}

		internal static bool GetInvokerForHashClientRpc(int cmdHash, out Type invokeClass, out NetworkBehaviour.CmdDelegate invokeFunction)
		{
			return NetworkBehaviour.GetInvokerForHash(cmdHash, NetworkBehaviour.UNetInvokeType.ClientRpc, out invokeClass, out invokeFunction);
		}

		internal static bool GetInvokerForHashSyncList(int cmdHash, out Type invokeClass, out NetworkBehaviour.CmdDelegate invokeFunction)
		{
			return NetworkBehaviour.GetInvokerForHash(cmdHash, NetworkBehaviour.UNetInvokeType.SyncList, out invokeClass, out invokeFunction);
		}

		internal static bool GetInvokerForHashSyncEvent(int cmdHash, out Type invokeClass, out NetworkBehaviour.CmdDelegate invokeFunction)
		{
			return NetworkBehaviour.GetInvokerForHash(cmdHash, NetworkBehaviour.UNetInvokeType.SyncEvent, out invokeClass, out invokeFunction);
		}

		private static bool GetInvokerForHash(int cmdHash, NetworkBehaviour.UNetInvokeType invokeType, out Type invokeClass, out NetworkBehaviour.CmdDelegate invokeFunction)
		{
			NetworkBehaviour.Invoker invoker = null;
			bool result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.TryGetValue(cmdHash, out invoker))
			{
				if (LogFilter.logDev)
				{
					Debug.Log("GetInvokerForHash hash:" + cmdHash + " not found");
				}
				invokeClass = null;
				invokeFunction = null;
				result = false;
			}
			else if (invoker == null)
			{
				if (LogFilter.logDev)
				{
					Debug.Log("GetInvokerForHash hash:" + cmdHash + " invoker null");
				}
				invokeClass = null;
				invokeFunction = null;
				result = false;
			}
			else if (invoker.invokeType != invokeType)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("GetInvokerForHash hash:" + cmdHash + " mismatched invokeType");
				}
				invokeClass = null;
				invokeFunction = null;
				result = false;
			}
			else
			{
				invokeClass = invoker.invokeClass;
				invokeFunction = invoker.invokeFunction;
				result = true;
			}
			return result;
		}

		internal static void DumpInvokers()
		{
			Debug.Log("DumpInvokers size:" + NetworkBehaviour.s_CmdHandlerDelegates.Count);
			foreach (KeyValuePair<int, NetworkBehaviour.Invoker> keyValuePair in NetworkBehaviour.s_CmdHandlerDelegates)
			{
				Debug.Log(string.Concat(new object[]
				{
					"  Invoker:",
					keyValuePair.Value.invokeClass,
					":",
					keyValuePair.Value.invokeFunction.GetMethodName(),
					" ",
					keyValuePair.Value.invokeType,
					" ",
					keyValuePair.Key
				}));
			}
		}

		internal bool ContainsCommandDelegate(int cmdHash)
		{
			return NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash);
		}

		internal bool InvokeCommandDelegate(int cmdHash, NetworkReader reader)
		{
			bool result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != NetworkBehaviour.UNetInvokeType.Command)
				{
					result = false;
				}
				else
				{
					if (base.GetType() != invoker.invokeClass)
					{
						if (!base.GetType().IsSubclassOf(invoker.invokeClass))
						{
							return false;
						}
					}
					invoker.invokeFunction(this, reader);
					result = true;
				}
			}
			return result;
		}

		internal bool InvokeRpcDelegate(int cmdHash, NetworkReader reader)
		{
			bool result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != NetworkBehaviour.UNetInvokeType.ClientRpc)
				{
					result = false;
				}
				else
				{
					if (base.GetType() != invoker.invokeClass)
					{
						if (!base.GetType().IsSubclassOf(invoker.invokeClass))
						{
							return false;
						}
					}
					invoker.invokeFunction(this, reader);
					result = true;
				}
			}
			return result;
		}

		internal bool InvokeSyncEventDelegate(int cmdHash, NetworkReader reader)
		{
			bool result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != NetworkBehaviour.UNetInvokeType.SyncEvent)
				{
					result = false;
				}
				else
				{
					invoker.invokeFunction(this, reader);
					result = true;
				}
			}
			return result;
		}

		internal bool InvokeSyncListDelegate(int cmdHash, NetworkReader reader)
		{
			bool result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != NetworkBehaviour.UNetInvokeType.SyncList)
				{
					result = false;
				}
				else if (base.GetType() != invoker.invokeClass)
				{
					result = false;
				}
				else
				{
					invoker.invokeFunction(this, reader);
					result = true;
				}
			}
			return result;
		}

		internal static string GetCmdHashHandlerName(int cmdHash)
		{
			string result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = cmdHash.ToString();
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				result = invoker.invokeType + ":" + invoker.invokeFunction.GetMethodName();
			}
			return result;
		}

		private static string GetCmdHashPrefixName(int cmdHash, string prefix)
		{
			string result;
			if (!NetworkBehaviour.s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = cmdHash.ToString();
			}
			else
			{
				NetworkBehaviour.Invoker invoker = NetworkBehaviour.s_CmdHandlerDelegates[cmdHash];
				string text = invoker.invokeFunction.GetMethodName();
				int num = text.IndexOf(prefix);
				if (num > -1)
				{
					text = text.Substring(prefix.Length);
				}
				result = text;
			}
			return result;
		}

		internal static string GetCmdHashCmdName(int cmdHash)
		{
			return NetworkBehaviour.GetCmdHashPrefixName(cmdHash, "InvokeCmd");
		}

		internal static string GetCmdHashRpcName(int cmdHash)
		{
			return NetworkBehaviour.GetCmdHashPrefixName(cmdHash, "InvokeRpc");
		}

		internal static string GetCmdHashEventName(int cmdHash)
		{
			return NetworkBehaviour.GetCmdHashPrefixName(cmdHash, "InvokeSyncEvent");
		}

		internal static string GetCmdHashListName(int cmdHash)
		{
			return NetworkBehaviour.GetCmdHashPrefixName(cmdHash, "InvokeSyncList");
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void SetSyncVarGameObject(GameObject newGameObject, ref GameObject gameObjectField, uint dirtyBit, ref NetworkInstanceId netIdField)
		{
			if (!m_SyncVarGuard)
			{
				NetworkInstanceId networkInstanceId = default(NetworkInstanceId);
				if (newGameObject != null)
				{
					NetworkIdentity component = newGameObject.GetComponent<NetworkIdentity>();
					if (component != null)
					{
						networkInstanceId = component.netId;
						if (networkInstanceId.IsEmpty())
						{
							if (LogFilter.logWarn)
							{
								Debug.LogWarning("SetSyncVarGameObject GameObject " + newGameObject + " has a zero netId. Maybe it is not spawned yet?");
							}
						}
					}
				}
				NetworkInstanceId networkInstanceId2 = default(NetworkInstanceId);
				if (gameObjectField != null)
				{
					networkInstanceId2 = gameObjectField.GetComponent<NetworkIdentity>().netId;
				}
				if (networkInstanceId != networkInstanceId2)
				{
					if (LogFilter.logDev)
					{
						Debug.Log(string.Concat(new object[]
						{
							"SetSyncVar GameObject ",
							base.GetType().Name,
							" bit [",
							dirtyBit,
							"] netfieldId:",
							networkInstanceId2,
							"->",
							networkInstanceId
						}));
					}
					SetDirtyBit(dirtyBit);
					gameObjectField = newGameObject;
					netIdField = networkInstanceId;
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void SetSyncVar<T>(T value, ref T fieldValue, uint dirtyBit)
		{
			bool flag = false;
			if (value == null)
			{
				if (fieldValue != null)
				{
					flag = true;
				}
			}
			else
			{
				flag = !value.Equals(fieldValue);
			}
			if (flag)
			{
				if (LogFilter.logDev)
				{
					Debug.Log(string.Concat(new object[]
					{
						"SetSyncVar ",
						base.GetType().Name,
						" bit [",
						dirtyBit,
						"] ",
						fieldValue,
						"->",
						value
					}));
				}
				SetDirtyBit(dirtyBit);
				fieldValue = value;
			}
		}

		public void SetDirtyBit(uint dirtyBit)
		{
			m_SyncVarDirtyBits |= dirtyBit;
		}

		public void ClearAllDirtyBits()
		{
			m_LastSendTime = Time.time;
			m_SyncVarDirtyBits = 0U;
		}

		internal int GetDirtyChannel()
		{
			if (Time.time - m_LastSendTime > GetNetworkSendInterval())
			{
				if (m_SyncVarDirtyBits != 0U)
				{
					return GetNetworkChannel();
				}
			}
			return -1;
		}

		public virtual bool OnSerialize(NetworkWriter writer, bool initialState)
		{
			if (!initialState)
			{
				writer.WritePackedUInt32(0U);
			}
			return false;
		}

		public virtual void OnDeserialize(NetworkReader reader, bool initialState)
		{
			if (!initialState)
			{
				reader.ReadPackedUInt32();
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void PreStartClient()
		{
		}

		public virtual void OnNetworkDestroy()
		{
		}

		public virtual void OnStartServer()
		{
		}

		public virtual void OnStartClient()
		{
		}

		public virtual void OnStartLocalPlayer()
		{
		}

		public virtual void OnStartAuthority()
		{
		}

		public virtual void OnStopAuthority()
		{
		}

		public virtual bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
		{
			return false;
		}

		public virtual void OnSetLocalVisibility(bool vis)
		{
		}

		public virtual bool OnCheckObserver(NetworkConnection conn)
		{
			return true;
		}

		public virtual int GetNetworkChannel()
		{
			return 0;
		}

		public virtual float GetNetworkSendInterval()
		{
			return 0.1f;
		}

		private uint m_SyncVarDirtyBits;

		private float m_LastSendTime;

		private bool m_SyncVarGuard;

		private const float k_DefaultSendInterval = 0.1f;

		private NetworkIdentity m_MyView;

		private static Dictionary<int, NetworkBehaviour.Invoker> s_CmdHandlerDelegates = new Dictionary<int, NetworkBehaviour.Invoker>();

		public delegate void CmdDelegate(NetworkBehaviour obj, NetworkReader reader);

		protected delegate void EventDelegate(List<Delegate> targets, NetworkReader reader);

		protected enum UNetInvokeType
		{
			Command,
			ClientRpc,
			SyncEvent,
			SyncList
		}

		protected class Invoker
		{
			public string DebugString()
			{
				return string.Concat(new object[]
				{
					invokeType,
					":",
					invokeClass,
					":",
					invokeFunction.GetMethodName()
				});
			}

			public NetworkBehaviour.UNetInvokeType invokeType;

			public Type invokeClass;

			public NetworkBehaviour.CmdDelegate invokeFunction;
		}
	}
}
