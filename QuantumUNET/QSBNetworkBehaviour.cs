using OWML.Logging;
using QuantumUNET.Components;
using QuantumUNET.Transport;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET
{
	public class QSBNetworkBehaviour : MonoBehaviour
	{
		public bool LocalPlayerAuthority => MyView.LocalPlayerAuthority;
		public bool IsServer => MyView.IsServer;
		public bool IsClient => MyView.IsClient;
		public bool IsLocalPlayer => MyView.IsLocalPlayer;
		public bool HasAuthority => MyView.HasAuthority;
		public NetworkInstanceId NetId => MyView.NetId;
		public QSBNetworkConnection ConnectionToServer => MyView.ConnectionToServer;
		public QSBNetworkConnection ConnectionToClient => MyView.ConnectionToClient;
		public short PlayerControllerId => MyView.PlayerControllerId;

		protected uint SyncVarDirtyBits { get; private set; }
		protected bool SyncVarHookGuard { get; set; }

		public QSBNetworkIdentity NetIdentity => MyView;

		private QSBNetworkIdentity MyView
		{
			get
			{
				QSBNetworkIdentity myView;
				if (m_MyView == null)
				{
					m_MyView = GetComponent<QSBNetworkIdentity>();
					if (m_MyView == null)
					{
						Debug.LogError("There is no NetworkIdentity on this object. Please add one.");
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

		protected void SendCommandInternal(QSBNetworkWriter writer, int channelId, string cmdName)
		{
			if (!IsLocalPlayer && !HasAuthority)
			{
				Debug.LogWarning("Trying to send command for object without authority.");
			}
			else if (QSBClientScene.readyConnection == null)
			{
				Debug.LogError($"Send command attempted with no client running [client={ConnectionToServer}].");
			}
			else
			{
				writer.FinishMessage();
				QSBClientScene.readyConnection.SendWriter(writer, channelId);
			}
		}

		public virtual bool InvokeCommand(int cmdHash, QSBNetworkReader reader) => InvokeCommandDelegate(cmdHash, reader);

		protected void SendRPCInternal(QSBNetworkWriter writer, int channelId, string rpcName)
		{
			if (!IsServer)
			{
				Debug.LogWarning("ClientRpc call on un-spawned object");
				return;
			}
			writer.FinishMessage();
			QSBNetworkServer.SendWriterToReady(gameObject, writer, channelId);
		}

		protected void SendTargetRPCInternal(QSBNetworkConnection conn, QSBNetworkWriter writer, int channelId, string rpcName)
		{
			if (!IsServer)
			{
				Debug.LogWarning("TargetRpc call on un-spawned object");
				return;
			}
			writer.FinishMessage();
			conn.SendWriter(writer, channelId);
		}

		public virtual bool InvokeRPC(int cmdHash, QSBNetworkReader reader) => InvokeRpcDelegate(cmdHash, reader);

		protected void SendEventInternal(QSBNetworkWriter writer, int channelId, string eventName)
		{
			if (!QSBNetworkServer.active)
			{
				ModConsole.OwmlConsole.WriteLine($"Error - Tried to send event {eventName} on channel {channelId} but QSBNetworkServer isn't active.");
				return;
			}
			writer.FinishMessage();
			QSBNetworkServer.SendWriterToReady(gameObject, writer, channelId);
		}

		public virtual bool InvokeSyncEvent(int cmdHash, QSBNetworkReader reader) => InvokeSyncEventDelegate(cmdHash, reader);

		public virtual bool InvokeSyncList(int cmdHash, QSBNetworkReader reader) => InvokeSyncListDelegate(cmdHash, reader);

		protected static void RegisterCommandDelegate(Type invokeClass, int cmdHash, CmdDelegate func)
		{
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				var invoker = new Invoker
				{
					invokeType = UNetInvokeType.Command,
					invokeClass = invokeClass,
					invokeFunction = func
				};
				s_CmdHandlerDelegates[cmdHash] = invoker;
			}
		}

		protected static void RegisterRpcDelegate(Type invokeClass, int cmdHash, CmdDelegate func)
		{
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				var invoker = new Invoker
				{
					invokeType = UNetInvokeType.ClientRpc,
					invokeClass = invokeClass,
					invokeFunction = func
				};
				s_CmdHandlerDelegates[cmdHash] = invoker;
			}
		}

		protected static void RegisterEventDelegate(Type invokeClass, int cmdHash, CmdDelegate func)
		{
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				var invoker = new Invoker
				{
					invokeType = UNetInvokeType.SyncEvent,
					invokeClass = invokeClass,
					invokeFunction = func
				};
				s_CmdHandlerDelegates[cmdHash] = invoker;
			}
		}

		protected static void RegisterSyncListDelegate(Type invokeClass, int cmdHash, CmdDelegate func)
		{
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				var invoker = new Invoker
				{
					invokeType = UNetInvokeType.SyncList,
					invokeClass = invokeClass,
					invokeFunction = func
				};
				s_CmdHandlerDelegates[cmdHash] = invoker;
			}
		}

		internal static string GetInvoker(int cmdHash)
		{
			string result;
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = null;
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				result = invoker.DebugString();
			}
			return result;
		}

		internal static bool GetInvokerForHashCommand(int cmdHash, out Type invokeClass, out CmdDelegate invokeFunction)
			=> GetInvokerForHash(cmdHash, UNetInvokeType.Command, out invokeClass, out invokeFunction);

		internal static bool GetInvokerForHashClientRpc(int cmdHash, out Type invokeClass, out CmdDelegate invokeFunction)
			=> GetInvokerForHash(cmdHash, UNetInvokeType.ClientRpc, out invokeClass, out invokeFunction);

		internal static bool GetInvokerForHashSyncList(int cmdHash, out Type invokeClass, out CmdDelegate invokeFunction)
			=> GetInvokerForHash(cmdHash, UNetInvokeType.SyncList, out invokeClass, out invokeFunction);

		internal static bool GetInvokerForHashSyncEvent(int cmdHash, out Type invokeClass, out CmdDelegate invokeFunction)
			=> GetInvokerForHash(cmdHash, UNetInvokeType.SyncEvent, out invokeClass, out invokeFunction);

		private static bool GetInvokerForHash(int cmdHash, UNetInvokeType invokeType, out Type invokeClass, out CmdDelegate invokeFunction)
		{
			bool result;
			if (!s_CmdHandlerDelegates.TryGetValue(cmdHash, out var invoker))
			{
				Debug.Log($"GetInvokerForHash hash:{cmdHash} not found");
				invokeClass = null;
				invokeFunction = null;
				result = false;
			}
			else if (invoker == null)
			{
				Debug.Log($"GetInvokerForHash hash:{cmdHash} invoker null");
				invokeClass = null;
				invokeFunction = null;
				result = false;
			}
			else if (invoker.invokeType != invokeType)
			{
				Debug.LogError($"GetInvokerForHash hash:{cmdHash} mismatched invokeType");
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
			Debug.Log($"DumpInvokers size:{s_CmdHandlerDelegates.Count}");
			foreach (var keyValuePair in s_CmdHandlerDelegates)
			{
				Debug.Log($"  Invoker:{keyValuePair.Value.invokeClass}:{keyValuePair.Value.invokeFunction.GetMethodName()} {keyValuePair.Value.invokeType} {keyValuePair.Key}");
			}
		}

		internal bool ContainsCommandDelegate(int cmdHash)
			=> s_CmdHandlerDelegates.ContainsKey(cmdHash);

		internal bool InvokeCommandDelegate(int cmdHash, QSBNetworkReader reader)
		{
			bool result;
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != UNetInvokeType.Command)
				{
					result = false;
				}
				else
				{
					if (GetType() != invoker.invokeClass)
					{
						if (!GetType().IsSubclassOf(invoker.invokeClass))
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

		internal bool InvokeRpcDelegate(int cmdHash, QSBNetworkReader reader)
		{
			bool result;
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != UNetInvokeType.ClientRpc)
				{
					result = false;
				}
				else
				{
					if (GetType() != invoker.invokeClass)
					{
						if (!GetType().IsSubclassOf(invoker.invokeClass))
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

		internal bool InvokeSyncEventDelegate(int cmdHash, QSBNetworkReader reader)
		{
			bool result;
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != UNetInvokeType.SyncEvent)
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

		internal bool InvokeSyncListDelegate(int cmdHash, QSBNetworkReader reader)
		{
			bool result;
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = false;
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				if (invoker.invokeType != UNetInvokeType.SyncList)
				{
					result = false;
				}
				else if (GetType() != invoker.invokeClass)
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
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = cmdHash.ToString();
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				result = $"{invoker.invokeType}:{invoker.invokeFunction.GetMethodName()}";
			}
			return result;
		}

		private static string GetCmdHashPrefixName(int cmdHash, string prefix)
		{
			string result;
			if (!s_CmdHandlerDelegates.ContainsKey(cmdHash))
			{
				result = cmdHash.ToString();
			}
			else
			{
				var invoker = s_CmdHandlerDelegates[cmdHash];
				var text = invoker.invokeFunction.GetMethodName();
				var num = text.IndexOf(prefix);
				if (num > -1)
				{
					text = text.Substring(prefix.Length);
				}
				result = text;
			}
			return result;
		}

		internal static string GetCmdHashCmdName(int cmdHash)
			=> GetCmdHashPrefixName(cmdHash, "InvokeCmd");

		internal static string GetCmdHashRpcName(int cmdHash)
			=> GetCmdHashPrefixName(cmdHash, "InvokeRpc");

		internal static string GetCmdHashEventName(int cmdHash)
			=> GetCmdHashPrefixName(cmdHash, "InvokeSyncEvent");

		internal static string GetCmdHashListName(int cmdHash)
			=> GetCmdHashPrefixName(cmdHash, "InvokeSyncList");

		protected void SetSyncVarGameObject(GameObject newGameObject, ref GameObject gameObjectField, uint dirtyBit, ref NetworkInstanceId netIdField)
		{
			if (!SyncVarHookGuard)
			{
				NetworkInstanceId networkInstanceId = default;
				if (newGameObject != null)
				{
					var component = newGameObject.GetComponent<QSBNetworkIdentity>();
					if (component != null)
					{
						networkInstanceId = component.NetId;
						if (networkInstanceId.IsEmpty())
						{
							Debug.LogWarning(
								$"SetSyncVarGameObject GameObject {newGameObject} has a zero netId. Maybe it is not spawned yet?");
						}
					}
				}
				NetworkInstanceId networkInstanceId2 = default;
				if (gameObjectField != null)
				{
					networkInstanceId2 = gameObjectField.GetComponent<QSBNetworkIdentity>().NetId;
				}
				if (networkInstanceId != networkInstanceId2)
				{
					Debug.Log(
						$"SetSyncVar GameObject {GetType().Name} bit [{dirtyBit}] netfieldId:{networkInstanceId2}->{networkInstanceId}");
					SetDirtyBit(dirtyBit);
					gameObjectField = newGameObject;
					netIdField = networkInstanceId;
				}
			}
		}

		protected void SetSyncVar<T>(T value, ref T fieldValue, uint dirtyBit)
		{
			var flag = false;
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
				Debug.Log($"SetSyncVar {GetType().Name} bit [{dirtyBit}] {fieldValue}->{value}");

				SetDirtyBit(dirtyBit);
				fieldValue = value;
			}
		}

		public void SetDirtyBit(uint dirtyBit) => SyncVarDirtyBits |= dirtyBit;

		public void ClearAllDirtyBits()
		{
			m_LastSendTime = Time.time;
			SyncVarDirtyBits = 0U;
		}

		internal int GetDirtyChannel()
		{
			if (Time.time - m_LastSendTime > GetNetworkSendInterval())
			{
				if (SyncVarDirtyBits != 0U)
				{
					return GetNetworkChannel();
				}
			}
			return -1;
		}

		public virtual bool OnSerialize(QSBNetworkWriter writer, bool initialState)
		{
			if (!initialState)
			{
				writer.WritePackedUInt32(0U);
			}
			return false;
		}

		public virtual void OnDeserialize(QSBNetworkReader reader, bool initialState)
		{
			if (!initialState)
			{
				reader.ReadPackedUInt32();
			}
		}

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

		public virtual bool OnRebuildObservers(HashSet<QSBNetworkConnection> observers, bool initialize) => false;

		public virtual void OnSetLocalVisibility(bool vis)
		{
		}

		public virtual bool OnCheckObserver(QSBNetworkConnection conn) => true;

		public virtual int GetNetworkChannel() => 0;

		public virtual float GetNetworkSendInterval() => 0.1f;

		private float m_LastSendTime;
		private QSBNetworkIdentity m_MyView;

		private static readonly Dictionary<int, Invoker> s_CmdHandlerDelegates = new Dictionary<int, Invoker>();

		public delegate void CmdDelegate(QSBNetworkBehaviour obj, QSBNetworkReader reader);

		protected delegate void EventDelegate(List<Delegate> targets, QSBNetworkReader reader);

		protected enum UNetInvokeType
		{
			Command,
			ClientRpc,
			SyncEvent,
			SyncList
		}

		protected class Invoker
		{
			public string DebugString() =>
				$"{invokeType}:{invokeClass}:{invokeFunction.GetMethodName()}";

			public UNetInvokeType invokeType;
			public Type invokeClass;
			public CmdDelegate invokeFunction;
		}
	}

	internal static class DotNetCompatibility
	{
		internal static string GetMethodName(this Delegate func) => func.Method.Name;

		internal static Type GetBaseType(this Type type) => type.BaseType;

		internal static string GetErrorCode(this SocketException e) => e.ErrorCode.ToString();
	}
}