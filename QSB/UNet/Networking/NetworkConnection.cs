using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.UNet.Networking
{
	public class NetworkConnection : IDisposable
	{
		public NetworkConnection()
		{
			m_Writer = new NetworkWriter();
		}

		internal HashSet<NetworkIdentity> visList
		{
			get
			{
				return m_VisList;
			}
		}

		public List<PlayerController> playerControllers
		{
			get
			{
				return m_PlayerControllers;
			}
		}

		public HashSet<NetworkInstanceId> clientOwnedObjects
		{
			get
			{
				return m_ClientOwnedObjects;
			}
		}

		public bool isConnected
		{
			get
			{
				return hostId != -1;
			}
		}

		public NetworkError lastError
		{
			get
			{
				return error;
			}
			internal set
			{
				error = value;
			}
		}

		internal Dictionary<short, NetworkConnection.PacketStat> packetStats
		{
			get
			{
				return m_PacketStats;
			}
		}

		public virtual void Initialize(string networkAddress, int networkHostId, int networkConnectionId, HostTopology hostTopology)
		{
			m_Writer = new NetworkWriter();
			address = networkAddress;
			hostId = networkHostId;
			connectionId = networkConnectionId;
			int channelCount = hostTopology.DefaultConfig.ChannelCount;
			int packetSize = (int)hostTopology.DefaultConfig.PacketSize;
			if (hostTopology.DefaultConfig.UsePlatformSpecificProtocols && Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PSP2)
			{
				throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
			}
			m_Channels = new ChannelBuffer[channelCount];
			for (int i = 0; i < channelCount; i++)
			{
				ChannelQOS channelQOS = hostTopology.DefaultConfig.Channels[i];
				int bufferSize = packetSize;
				if (channelQOS.QOS == QosType.ReliableFragmented || channelQOS.QOS == QosType.UnreliableFragmented)
				{
					bufferSize = (int)(hostTopology.DefaultConfig.FragmentSize * 128);
				}
				m_Channels[i] = new ChannelBuffer(this, bufferSize, (byte)i, NetworkConnection.IsReliableQoS(channelQOS.QOS), NetworkConnection.IsSequencedQoS(channelQOS.QOS));
			}
		}

		~NetworkConnection()
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
				for (int i = 0; i < m_Channels.Length; i++)
				{
					m_Channels[i].Dispose();
				}
			}
			m_Channels = null;
			if (m_ClientOwnedObjects != null)
			{
				foreach (NetworkInstanceId netId in m_ClientOwnedObjects)
				{
					GameObject gameObject = NetworkServer.FindLocalObject(netId);
					if (gameObject != null)
					{
						gameObject.GetComponent<NetworkIdentity>().ClearClientOwner();
					}
				}
			}
			m_ClientOwnedObjects = null;
			m_Disposed = true;
		}

		private static bool IsSequencedQoS(QosType qos)
		{
			return qos == QosType.ReliableSequenced || qos == QosType.UnreliableSequenced;
		}

		private static bool IsReliableQoS(QosType qos)
		{
			return qos == QosType.Reliable || qos == QosType.ReliableFragmented || qos == QosType.ReliableSequenced || qos == QosType.ReliableStateUpdate;
		}

		public bool SetChannelOption(int channelId, ChannelOption option, int value)
		{
			return m_Channels != null && channelId >= 0 && channelId < m_Channels.Length && m_Channels[channelId].SetOption(option, value);
		}

		public void Disconnect()
		{
			address = "";
			isReady = false;
			ClientScene.HandleClientDisconnect(this);
			if (hostId != -1)
			{
				byte b;
				NetworkTransport.Disconnect(hostId, connectionId, out b);
				RemoveObservers();
			}
		}

		internal void SetHandlers(NetworkMessageHandlers handlers)
		{
			m_MessageHandlers = handlers;
			m_MessageHandlersDict = handlers.GetHandlers();
		}

		public bool CheckHandler(short msgType)
		{
			return m_MessageHandlersDict.ContainsKey(msgType);
		}

		public bool InvokeHandlerNoData(short msgType)
		{
			return InvokeHandler(msgType, null, 0);
		}

		public bool InvokeHandler(short msgType, NetworkReader reader, int channelId)
		{
			bool result;
			if (m_MessageHandlersDict.ContainsKey(msgType))
			{
				m_MessageInfo.msgType = msgType;
				m_MessageInfo.conn = this;
				m_MessageInfo.reader = reader;
				m_MessageInfo.channelId = channelId;
				NetworkMessageDelegate networkMessageDelegate = m_MessageHandlersDict[msgType];
				if (networkMessageDelegate == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkConnection InvokeHandler no handler for " + msgType);
					}
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

		public bool InvokeHandler(NetworkMessage netMsg)
		{
			bool result;
			if (m_MessageHandlersDict.ContainsKey(netMsg.msgType))
			{
				NetworkMessageDelegate networkMessageDelegate = m_MessageHandlersDict[netMsg.msgType];
				networkMessageDelegate(netMsg);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		internal void HandleFragment(NetworkReader reader, int channelId)
		{
			if (channelId >= 0 && channelId < m_Channels.Length)
			{
				ChannelBuffer channelBuffer = m_Channels[channelId];
				if (channelBuffer.HandleFragment(reader))
				{
					NetworkReader networkReader = new NetworkReader(channelBuffer.fragmentBuffer.AsArraySegment().Array);
					networkReader.ReadInt16();
					short msgType = networkReader.ReadInt16();
					InvokeHandler(msgType, networkReader, channelId);
				}
			}
		}

		public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
		{
			m_MessageHandlers.RegisterHandler(msgType, handler);
		}

		public void UnregisterHandler(short msgType)
		{
			m_MessageHandlers.UnregisterHandler(msgType);
		}

		internal void SetPlayerController(PlayerController player)
		{
			while ((int)player.playerControllerId >= m_PlayerControllers.Count)
			{
				m_PlayerControllers.Add(new PlayerController());
			}
			m_PlayerControllers[(int)player.playerControllerId] = player;
		}

		internal void RemovePlayerController(short playerControllerId)
		{
			for (int i = m_PlayerControllers.Count; i >= 0; i--)
			{
				if ((int)playerControllerId == i && playerControllerId == m_PlayerControllers[i].playerControllerId)
				{
					m_PlayerControllers[i] = new PlayerController();
					return;
				}
			}
			if (LogFilter.logError)
			{
				Debug.LogError("RemovePlayer player at playerControllerId " + playerControllerId + " not found");
				return;
			}
		}

		internal bool GetPlayerController(short playerControllerId, out PlayerController playerController)
		{
			playerController = null;
			bool result;
			if (playerControllers.Count > 0)
			{
				for (int i = 0; i < playerControllers.Count; i++)
				{
					if (playerControllers[i].IsValid && playerControllers[i].playerControllerId == playerControllerId)
					{
						playerController = playerControllers[i];
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
				for (int i = 0; i < m_Channels.Length; i++)
				{
					m_Channels[i].CheckInternalBuffer();
				}
			}
		}

		public void SetMaxDelay(float seconds)
		{
			if (m_Channels != null)
			{
				for (int i = 0; i < m_Channels.Length; i++)
				{
					m_Channels[i].maxDelay = seconds;
				}
			}
		}

		public virtual bool Send(short msgType, MessageBase msg)
		{
			return SendByChannel(msgType, msg, 0);
		}

		public virtual bool SendUnreliable(short msgType, MessageBase msg)
		{
			return SendByChannel(msgType, msg, 1);
		}

		public virtual bool SendByChannel(short msgType, MessageBase msg, int channelId)
		{
			m_Writer.StartMessage(msgType);
			msg.Serialize(m_Writer);
			m_Writer.FinishMessage();
			return SendWriter(m_Writer, channelId);
		}

		public virtual bool SendBytes(byte[] bytes, int numBytes, int channelId)
		{
			if (logNetworkMessages)
			{
				LogSend(bytes);
			}
			return CheckChannel(channelId) && m_Channels[channelId].SendBytes(bytes, numBytes);
		}

		public virtual bool SendWriter(NetworkWriter writer, int channelId)
		{
			if (logNetworkMessages)
			{
				LogSend(writer.ToArray());
			}
			return CheckChannel(channelId) && m_Channels[channelId].SendWriter(writer);
		}

		private void LogSend(byte[] bytes)
		{
			NetworkReader networkReader = new NetworkReader(bytes);
			ushort num = networkReader.ReadUInt16();
			ushort num2 = networkReader.ReadUInt16();
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 4; i < (int)(4 + num); i++)
			{
				stringBuilder.AppendFormat("{0:X2}", bytes[i]);
				if (i > 150)
				{
					break;
				}
			}
			Debug.Log(string.Concat(new object[]
			{
				"ConnectionSend con:",
				connectionId,
				" bytes:",
				num,
				" msgId:",
				num2,
				" ",
				stringBuilder
			}));
		}

		private bool CheckChannel(int channelId)
		{
			bool result;
			if (m_Channels == null)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("Channels not initialized sending on id '" + channelId);
				}
				result = false;
			}
			else if (channelId < 0 || channelId >= m_Channels.Length)
			{
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Invalid channel when sending buffered data, '",
						channelId,
						"'. Current channel count is ",
						m_Channels.Length
					}));
				}
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
			NetworkReader reader = new NetworkReader(buffer);
			HandleReader(reader, receivedSize, channelId);
		}

		protected void HandleReader(NetworkReader reader, int receivedSize, int channelId)
		{
			while ((ulong)reader.Position < (ulong)((long)receivedSize))
			{
				ushort num = reader.ReadUInt16();
				short num2 = reader.ReadInt16();
				byte[] array = reader.ReadBytes((int)num);
				NetworkReader reader2 = new NetworkReader(array);
				if (logNetworkMessages)
				{
					StringBuilder stringBuilder = new StringBuilder();
					for (int i = 0; i < (int)num; i++)
					{
						stringBuilder.AppendFormat("{0:X2}", array[i]);
						if (i > 150)
						{
							break;
						}
					}
					Debug.Log(string.Concat(new object[]
					{
						"ConnectionRecv con:",
						connectionId,
						" bytes:",
						num,
						" msgId:",
						num2,
						" ",
						stringBuilder
					}));
				}
				NetworkMessageDelegate networkMessageDelegate = null;
				if (m_MessageHandlersDict.ContainsKey(num2))
				{
					networkMessageDelegate = m_MessageHandlersDict[num2];
				}
				if (networkMessageDelegate == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError(string.Concat(new object[]
						{
							"Unknown message ID ",
							num2,
							" connId:",
							connectionId
						}));
					}
					break;
				}
				m_NetMsg.msgType = num2;
				m_NetMsg.reader = reader2;
				m_NetMsg.conn = this;
				m_NetMsg.channelId = channelId;
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
			for (int i = 0; i < m_Channels.Length; i++)
			{
				ChannelBuffer channelBuffer = m_Channels[i];
				numMsgs += channelBuffer.numMsgsOut;
				numBufferedMsgs += channelBuffer.numBufferedMsgsOut;
				numBytes += channelBuffer.numBytesOut;
				lastBufferedPerSecond += channelBuffer.lastBufferedPerSecond;
			}
		}

		public virtual void GetStatsIn(out int numMsgs, out int numBytes)
		{
			numMsgs = 0;
			numBytes = 0;
			for (int i = 0; i < m_Channels.Length; i++)
			{
				ChannelBuffer channelBuffer = m_Channels[i];
				numMsgs += channelBuffer.numMsgsIn;
				numBytes += channelBuffer.numBytesIn;
			}
		}

		public override string ToString()
		{
			return string.Format("hostId: {0} connectionId: {1} isReady: {2} channel count: {3}", new object[]
			{
				hostId,
				connectionId,
				isReady,
				(m_Channels == null) ? 0 : m_Channels.Length
			});
		}

		internal void AddToVisList(NetworkIdentity uv)
		{
			m_VisList.Add(uv);
			NetworkServer.ShowForConnection(uv, this);
		}

		internal void RemoveFromVisList(NetworkIdentity uv, bool isDestroyed)
		{
			m_VisList.Remove(uv);
			if (!isDestroyed)
			{
				NetworkServer.HideForConnection(uv, this);
			}
		}

		internal void RemoveObservers()
		{
			foreach (NetworkIdentity networkIdentity in m_VisList)
			{
				networkIdentity.RemoveObserverInternal(this);
			}
			m_VisList.Clear();
		}

		public virtual void TransportReceive(byte[] bytes, int numBytes, int channelId)
		{
			HandleBytes(bytes, numBytes, channelId);
		}

		[Obsolete("TransportRecieve has been deprecated. Use TransportReceive instead (UnityUpgradable) -> TransportReceive(*)", false)]
		public virtual void TransportRecieve(byte[] bytes, int numBytes, int channelId)
		{
			TransportReceive(bytes, numBytes, channelId);
		}

		public virtual bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
		{
			return NetworkTransport.Send(hostId, connectionId, channelId, bytes, numBytes, out error);
		}

		internal void AddOwnedObject(NetworkIdentity obj)
		{
			if (m_ClientOwnedObjects == null)
			{
				m_ClientOwnedObjects = new HashSet<NetworkInstanceId>();
			}
			m_ClientOwnedObjects.Add(obj.netId);
		}

		internal void RemoveOwnedObject(NetworkIdentity obj)
		{
			if (m_ClientOwnedObjects != null)
			{
				m_ClientOwnedObjects.Remove(obj.netId);
			}
		}

		internal static void OnFragment(NetworkMessage netMsg)
		{
			netMsg.conn.HandleFragment(netMsg.reader, netMsg.channelId);
		}

		private ChannelBuffer[] m_Channels;

		private List<PlayerController> m_PlayerControllers = new List<PlayerController>();

		private NetworkMessage m_NetMsg = new NetworkMessage();

		private HashSet<NetworkIdentity> m_VisList = new HashSet<NetworkIdentity>();

		private NetworkWriter m_Writer;

		private Dictionary<short, NetworkMessageDelegate> m_MessageHandlersDict;

		private NetworkMessageHandlers m_MessageHandlers;

		private HashSet<NetworkInstanceId> m_ClientOwnedObjects;

		private NetworkMessage m_MessageInfo = new NetworkMessage();

		private const int k_MaxMessageLogSize = 150;

		private NetworkError error;

		public int hostId = -1;

		public int connectionId = -1;

		public bool isReady;

		public string address;

		public float lastMessageTime;

		public bool logNetworkMessages = false;

		private Dictionary<short, NetworkConnection.PacketStat> m_PacketStats = new Dictionary<short, NetworkConnection.PacketStat>();

		private bool m_Disposed;

		public class PacketStat
		{
			public PacketStat()
			{
				msgType = 0;
				count = 0;
				bytes = 0;
			}

			public PacketStat(NetworkConnection.PacketStat s)
			{
				msgType = s.msgType;
				count = s.count;
				bytes = s.bytes;
			}

			public override string ToString()
			{
				return string.Concat(new object[]
				{
					MsgType.MsgTypeToString(msgType),
					": count=",
					count,
					" bytes=",
					bytes
				});
			}

			public short msgType;

			public int count;

			public int bytes;
		}
	}
}
