using OWML.Logging;
using QuantumUNET.Components;
using QuantumUNET.Messages;
using QuantumUNET.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET
{
	public class QSBNetworkConnection : IDisposable
	{
		public QSBNetworkConnection()
		{
			m_Writer = new QSBNetworkWriter();
		}

		internal HashSet<QSBNetworkIdentity> VisList { get; } = new HashSet<QSBNetworkIdentity>();

		public List<QSBPlayerController> PlayerControllers { get; } = new List<QSBPlayerController>();

		public HashSet<NetworkInstanceId> ClientOwnedObjects { get; private set; }

		public bool isConnected => hostId != -1;

		public NetworkError LastError { get; internal set; }

		internal Dictionary<short, PacketStat> PacketStats { get; } = new Dictionary<short, PacketStat>();

		public virtual void Initialize(string networkAddress, int networkHostId, int networkConnectionId, HostTopology hostTopology)
		{
			m_Writer = new QSBNetworkWriter();
			address = networkAddress;
			hostId = networkHostId;
			connectionId = networkConnectionId;
			var channelCount = hostTopology.DefaultConfig.ChannelCount;
			var packetSize = (int)hostTopology.DefaultConfig.PacketSize;
			if (hostTopology.DefaultConfig.UsePlatformSpecificProtocols && Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PSP2)
			{
				throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
			}
			m_Channels = new QSBChannelBuffer[channelCount];
			for (var i = 0; i < channelCount; i++)
			{
				var channelQOS = hostTopology.DefaultConfig.Channels[i];
				var bufferSize = packetSize;
				if (channelQOS.QOS == QosType.ReliableFragmented || channelQOS.QOS == QosType.UnreliableFragmented)
				{
					bufferSize = hostTopology.DefaultConfig.FragmentSize * 128;
				}
				m_Channels[i] = new QSBChannelBuffer(this, bufferSize, (byte)i, IsReliableQoS(channelQOS.QOS), IsSequencedQoS(channelQOS.QOS));
			}
		}

		~QSBNetworkConnection()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_Disposed && m_Channels != null)
			{
				foreach (var channel in m_Channels)
				{
					channel.Dispose();
				}
			}
			m_Channels = null;
			if (ClientOwnedObjects != null)
			{
				foreach (var netId in ClientOwnedObjects)
				{
					var gameObject = QSBNetworkServer.FindLocalObject(netId);
					if (gameObject != null)
					{
						gameObject.GetComponent<QSBNetworkIdentity>().ClearClientOwner();
					}
				}
			}
			ClientOwnedObjects = null;
			m_Disposed = true;
		}

		private static bool IsSequencedQoS(QosType qos) => qos == QosType.ReliableSequenced || qos == QosType.UnreliableSequenced;

		private static bool IsReliableQoS(QosType qos) => qos == QosType.Reliable || qos == QosType.ReliableFragmented || qos == QosType.ReliableSequenced || qos == QosType.ReliableStateUpdate;

		public bool SetChannelOption(int channelId, ChannelOption option, int value) => m_Channels != null && channelId >= 0 && channelId < m_Channels.Length && m_Channels[channelId].SetOption(option, value);

		public void Disconnect()
		{
			address = "";
			isReady = false;
			QSBClientScene.HandleClientDisconnect(this);
			if (hostId != -1)
			{
				NetworkTransport.Disconnect(hostId, connectionId, out var b);
				RemoveObservers();
			}
		}

		internal void SetHandlers(QSBNetworkMessageHandlers handlers)
		{
			m_MessageHandlers = handlers;
			m_MessageHandlersDict = handlers.GetHandlers();
		}

		public bool CheckHandler(short msgType) => m_MessageHandlersDict.ContainsKey(msgType);

		public bool InvokeHandlerNoData(short msgType) => InvokeHandler(msgType, null, 0);

		public bool InvokeHandler(short msgType, QSBNetworkReader reader, int channelId)
		{
			bool result;
			if (m_MessageHandlersDict.ContainsKey(msgType))
			{
				m_MessageInfo.MsgType = msgType;
				m_MessageInfo.Connection = this;
				m_MessageInfo.Reader = reader;
				m_MessageInfo.ChannelId = channelId;
				var networkMessageDelegate = m_MessageHandlersDict[msgType];
				if (networkMessageDelegate == null)
				{
					result = false;
				}
				else
				{
					networkMessageDelegate(m_MessageInfo);
					result = true;
				}
			}
			else
			{
				result = false;
			}
			return result;
		}

		public bool InvokeHandler(QSBNetworkMessage netMsg)
		{
			bool result;
			if (m_MessageHandlersDict.ContainsKey(netMsg.MsgType))
			{
				var networkMessageDelegate = m_MessageHandlersDict[netMsg.MsgType];
				networkMessageDelegate(netMsg);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		internal void HandleFragment(QSBNetworkReader reader, int channelId)
		{
			if (channelId >= 0 && channelId < m_Channels.Length)
			{
				var channelBuffer = m_Channels[channelId];
				if (channelBuffer.HandleFragment(reader))
				{
					var networkReader = new QSBNetworkReader(channelBuffer._fragmentBuffer.AsArraySegment().Array);
					networkReader.ReadInt16();
					var msgType = networkReader.ReadInt16();
					InvokeHandler(msgType, networkReader, channelId);
				}
			}
		}

		public void RegisterHandler(short msgType, QSBNetworkMessageDelegate handler) => m_MessageHandlers.RegisterHandler(msgType, handler);

		public void UnregisterHandler(short msgType) => m_MessageHandlers.UnregisterHandler(msgType);

		internal void SetPlayerController(QSBPlayerController player)
		{
			while (player.PlayerControllerId >= PlayerControllers.Count)
			{
				PlayerControllers.Add(new QSBPlayerController());
			}
			PlayerControllers[player.PlayerControllerId] = player;
		}

		internal void RemovePlayerController(short playerControllerId)
		{
			for (var i = PlayerControllers.Count; i >= 0; i--)
			{
				if (playerControllerId == i && playerControllerId == PlayerControllers[i].PlayerControllerId)
				{
					PlayerControllers[i] = new QSBPlayerController();
					return;
				}
			}
			Debug.LogError($"RemovePlayer player at playerControllerId {playerControllerId} not found");
		}

		internal bool GetPlayerController(short playerControllerId, out QSBPlayerController playerController)
		{
			playerController = null;
			bool result;
			if (PlayerControllers.Count > 0)
			{
				foreach (var controller in PlayerControllers)
				{
					if (controller.IsValid && controller.PlayerControllerId == playerControllerId)
					{
						playerController = controller;
						return true;
					}
				}

				result = false;
			}
			else
			{
				result = false;
			}
			return result;
		}

		public void FlushChannels()
		{
			if (m_Channels != null)
			{
				foreach (var channel in m_Channels)
				{
					channel.CheckInternalBuffer();
				}
			}
		}

		public void SetMaxDelay(float seconds)
		{
			if (m_Channels != null)
			{
				foreach (var channel in m_Channels)
				{
					channel.MaxDelay = seconds;
				}
			}
		}

		public virtual bool Send(short msgType, QSBMessageBase msg) => SendByChannel(msgType, msg, 0);

		public virtual bool SendUnreliable(short msgType, QSBMessageBase msg) => SendByChannel(msgType, msg, 1);

		public virtual bool SendByChannel(short msgType, QSBMessageBase msg, int channelId)
		{
			m_Writer.StartMessage(msgType);
			msg.Serialize(m_Writer);
			m_Writer.FinishMessage();
			return SendWriter(m_Writer, channelId);
		}

		public virtual bool SendBytes(byte[] bytes, int numBytes, int channelId) => CheckChannel(channelId) && m_Channels[channelId].SendBytes(bytes, numBytes);

		public virtual bool SendWriter(QSBNetworkWriter writer, int channelId) => CheckChannel(channelId) && m_Channels[channelId].SendWriter(writer);

		private void LogSend(byte[] bytes)
		{
			var networkReader = new QSBNetworkReader(bytes);
			var num = networkReader.ReadUInt16();
			var num2 = networkReader.ReadUInt16();
			var stringBuilder = new StringBuilder();
			for (var i = 4; i < 4 + num; i++)
			{
				stringBuilder.AppendFormat("{0:X2}", bytes[i]);
				if (i > 150)
				{
					break;
				}
			}
			ModConsole.OwmlConsole.WriteLine(
				$"ConnectionSend con:{connectionId} bytes:{num} msgId:{num2} {stringBuilder}");
		}

		private bool CheckChannel(int channelId)
		{
			bool result;
			if (m_Channels == null)
			{
				Debug.LogWarning($"Channels not initialized sending on id '{channelId}");
				result = false;
			}
			else if (channelId < 0 || channelId >= m_Channels.Length)
			{
				Debug.LogError(
					$"Invalid channel when sending buffered data, '{channelId}'. Current channel count is {m_Channels.Length}");
				result = false;
			}
			else
			{
				result = true;
			}
			return result;
		}

		public void ResetStats()
		{
		}

		protected void HandleBytes(byte[] buffer, int receivedSize, int channelId)
		{
			var reader = new QSBNetworkReader(buffer);
			HandleReader(reader, receivedSize, channelId);
		}

		protected void HandleReader(QSBNetworkReader reader, int receivedSize, int channelId)
		{
			while (reader.Position < receivedSize)
			{
				var num = reader.ReadUInt16();
				var num2 = reader.ReadInt16();
				var array = reader.ReadBytes(num);
				var reader2 = new QSBNetworkReader(array);
				QSBNetworkMessageDelegate networkMessageDelegate = null;
				if (m_MessageHandlersDict.ContainsKey(num2))
				{
					networkMessageDelegate = m_MessageHandlersDict[num2];
				}
				if (networkMessageDelegate == null)
				{
					ModConsole.OwmlConsole.WriteLine($"Unknown message ID {num2} connId:{connectionId}");
					break;
				}
				m_NetMsg.MsgType = num2;
				m_NetMsg.Reader = reader2;
				m_NetMsg.Connection = this;
				m_NetMsg.ChannelId = channelId;
				networkMessageDelegate(m_NetMsg);
				lastMessageTime = Time.time;
			}
		}

		public virtual void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
		{
			numMsgs = 0;
			numBufferedMsgs = 0;
			numBytes = 0;
			lastBufferedPerSecond = 0;
			foreach (var channelBuffer in m_Channels)
			{
				numMsgs += channelBuffer.NumMsgsOut;
				numBufferedMsgs += channelBuffer.NumBufferedMsgsOut;
				numBytes += channelBuffer.NumBytesOut;
				lastBufferedPerSecond += channelBuffer.LastBufferedPerSecond;
			}
		}

		public virtual void GetStatsIn(out int numMsgs, out int numBytes)
		{
			numMsgs = 0;
			numBytes = 0;
			foreach (var channelBuffer in m_Channels)
			{
				numMsgs += channelBuffer.NumMsgsIn;
				numBytes += channelBuffer.NumBytesIn;
			}
		}

		public override string ToString() =>
			$"hostId: {hostId} connectionId: {connectionId} isReady: {isReady} channel count: {m_Channels?.Length ?? 0}";

		internal void AddToVisList(QSBNetworkIdentity uv)
		{
			VisList.Add(uv);
			QSBNetworkServer.ShowForConnection(uv, this);
		}

		internal void RemoveFromVisList(QSBNetworkIdentity uv, bool isDestroyed)
		{
			VisList.Remove(uv);
			if (!isDestroyed)
			{
				QSBNetworkServer.HideForConnection(uv, this);
			}
		}

		internal void RemoveObservers()
		{
			foreach (var networkIdentity in VisList)
			{
				networkIdentity.RemoveObserverInternal(this);
			}
			VisList.Clear();
		}

		public virtual void TransportReceive(byte[] bytes, int numBytes, int channelId) => HandleBytes(bytes, numBytes, channelId);

		public virtual bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error) => NetworkTransport.Send(hostId, connectionId, channelId, bytes, numBytes, out error);

		internal void AddOwnedObject(QSBNetworkIdentity obj)
		{
			if (ClientOwnedObjects == null)
			{
				ClientOwnedObjects = new HashSet<NetworkInstanceId>();
			}
			ClientOwnedObjects.Add(obj.NetId);
		}

		internal void RemoveOwnedObject(QSBNetworkIdentity obj)
		{
			ClientOwnedObjects?.Remove(obj.NetId);
		}

		internal static void OnFragment(QSBNetworkMessage netMsg) => netMsg.Connection.HandleFragment(netMsg.Reader, netMsg.ChannelId);

		private QSBChannelBuffer[] m_Channels;
		private readonly QSBNetworkMessage m_NetMsg = new QSBNetworkMessage();
		private QSBNetworkWriter m_Writer;

		private Dictionary<short, QSBNetworkMessageDelegate> m_MessageHandlersDict;

		private QSBNetworkMessageHandlers m_MessageHandlers;
		private readonly QSBNetworkMessage m_MessageInfo = new QSBNetworkMessage();

		private const int k_MaxMessageLogSize = 150;
		public int hostId = -1;

		public int connectionId = -1;

		public bool isReady;

		public string address;

		public float lastMessageTime;

		public bool logNetworkMessages = false;
		private bool m_Disposed;

		public class PacketStat
		{
			public PacketStat()
			{
				msgType = 0;
				count = 0;
				bytes = 0;
			}

			public PacketStat(PacketStat s)
			{
				msgType = s.msgType;
				count = s.count;
				bytes = s.bytes;
			}

			public override string ToString()
			{
				return $"{QSBMsgType.MsgTypeToString(msgType)}: count={count} bytes={bytes}";
			}

			public short msgType;

			public int count;

			public int bytes;
		}
	}
}