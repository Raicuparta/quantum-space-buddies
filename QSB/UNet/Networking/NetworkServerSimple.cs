using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace QSB.UNet.Networking
{
	public class NetworkServerSimple
	{
		public NetworkServerSimple()
		{
			m_ConnectionsReadOnly = new ReadOnlyCollection<NetworkConnection>(m_Connections);
		}

		public int listenPort
		{
			get
			{
				return m_ListenPort;
			}
			set
			{
				m_ListenPort = value;
			}
		}

		public int serverHostId
		{
			get
			{
				return m_ServerHostId;
			}
			set
			{
				m_ServerHostId = value;
			}
		}

		public HostTopology hostTopology
		{
			get
			{
				return m_HostTopology;
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

		public ReadOnlyCollection<NetworkConnection> connections
		{
			get
			{
				return m_ConnectionsReadOnly;
			}
		}

		public Dictionary<short, NetworkMessageDelegate> handlers
		{
			get
			{
				return m_MessageHandlers.GetHandlers();
			}
		}

		public byte[] messageBuffer
		{
			get
			{
				return m_MsgBuffer;
			}
		}

		public NetworkReader messageReader
		{
			get
			{
				return m_MsgReader;
			}
		}

		public Type networkConnectionClass
		{
			get
			{
				return m_NetworkConnectionClass;
			}
		}

		public void SetNetworkConnectionClass<T>() where T : NetworkConnection
		{
			m_NetworkConnectionClass = typeof(T);
		}

		public virtual void Initialize()
		{
			if (!m_Initialized)
			{
				m_Initialized = true;
				NetworkTransport.Init();
				m_MsgBuffer = new byte[65535];
				m_MsgReader = new NetworkReader(m_MsgBuffer);
				if (m_HostTopology == null)
				{
					ConnectionConfig connectionConfig = new ConnectionConfig();
					connectionConfig.AddChannel(QosType.ReliableSequenced);
					connectionConfig.AddChannel(QosType.Unreliable);
					m_HostTopology = new HostTopology(connectionConfig, 8);
				}
				if (LogFilter.logDebug)
				{
					Debug.Log("NetworkServerSimple initialize.");
				}
			}
		}

		public bool Configure(ConnectionConfig config, int maxConnections)
		{
			HostTopology topology = new HostTopology(config, maxConnections);
			return Configure(topology);
		}

		public bool Configure(HostTopology topology)
		{
			m_HostTopology = topology;
			return true;
		}

		public bool Listen(string ipAddress, int serverListenPort)
		{
			Initialize();
			m_ListenPort = serverListenPort;
			if (m_UseWebSockets)
			{
				m_ServerHostId = NetworkTransport.AddWebsocketHost(m_HostTopology, serverListenPort, ipAddress);
			}
			else
			{
				m_ServerHostId = NetworkTransport.AddHost(m_HostTopology, serverListenPort, ipAddress);
			}
			bool result;
			if (m_ServerHostId == -1)
			{
				result = false;
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"NetworkServerSimple listen: ",
						ipAddress,
						":",
						m_ListenPort
					}));
				}
				result = true;
			}
			return result;
		}

		public bool Listen(int serverListenPort)
		{
			return Listen(serverListenPort, m_HostTopology);
		}

		public bool Listen(int serverListenPort, HostTopology topology)
		{
			m_HostTopology = topology;
			Initialize();
			m_ListenPort = serverListenPort;
			if (m_UseWebSockets)
			{
				m_ServerHostId = NetworkTransport.AddWebsocketHost(m_HostTopology, serverListenPort);
			}
			else
			{
				m_ServerHostId = NetworkTransport.AddHost(m_HostTopology, serverListenPort);
			}
			bool result;
			if (m_ServerHostId == -1)
			{
				result = false;
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log("NetworkServerSimple listen " + m_ListenPort);
				}
				result = true;
			}
			return result;
		}

		public void ListenRelay(string relayIp, int relayPort, NetworkID netGuid, SourceID sourceId, NodeID nodeId)
		{
			Initialize();
			m_ServerHostId = NetworkTransport.AddHost(m_HostTopology, listenPort);
			if (LogFilter.logDebug)
			{
				Debug.Log("Server Host Slot Id: " + m_ServerHostId);
			}
			Update();
			byte b;
			NetworkTransport.ConnectAsNetworkHost(m_ServerHostId, relayIp, relayPort, netGuid, sourceId, nodeId, out b);
			m_RelaySlotId = 0;
			if (LogFilter.logDebug)
			{
				Debug.Log("Relay Slot Id: " + m_RelaySlotId);
			}
		}

		public void Stop()
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkServerSimple stop ");
			}
			NetworkTransport.RemoveHost(m_ServerHostId);
			m_ServerHostId = -1;
		}

		internal void RegisterHandlerSafe(short msgType, NetworkMessageDelegate handler)
		{
			m_MessageHandlers.RegisterHandlerSafe(msgType, handler);
		}

		public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
		{
			m_MessageHandlers.RegisterHandler(msgType, handler);
		}

		public void UnregisterHandler(short msgType)
		{
			m_MessageHandlers.UnregisterHandler(msgType);
		}

		public void ClearHandlers()
		{
			m_MessageHandlers.ClearMessageHandlers();
		}

		public void UpdateConnections()
		{
			for (int i = 0; i < m_Connections.Count; i++)
			{
				NetworkConnection networkConnection = m_Connections[i];
				if (networkConnection != null)
				{
					networkConnection.FlushChannels();
				}
			}
		}

		public void Update()
		{
			if (m_ServerHostId != -1)
			{
				NetworkEventType networkEventType;
				if (m_RelaySlotId != -1)
				{
					byte b;
					networkEventType = (NetworkEventType)NetworkTransport.ReceiveRelayEventFromHost(m_ServerHostId, out b);
					if (networkEventType != NetworkEventType.Nothing)
					{
						if (LogFilter.logDebug)
						{
							Debug.Log("NetGroup event:" + networkEventType);
						}
					}
					if (networkEventType == NetworkEventType.ConnectEvent)
					{
						if (LogFilter.logDebug)
						{
							Debug.Log("NetGroup server connected");
						}
					}
					if (networkEventType == NetworkEventType.DisconnectEvent)
					{
						if (LogFilter.logDebug)
						{
							Debug.Log("NetGroup server disconnected");
						}
					}
				}
				do
				{
					byte b;
					int connectionId;
					int channelId;
					int receivedSize;
					networkEventType = (NetworkEventType)NetworkTransport.ReceiveFromHost(m_ServerHostId, out connectionId, out channelId, m_MsgBuffer, m_MsgBuffer.Length, out receivedSize, out b);
					if (networkEventType != NetworkEventType.Nothing)
					{
						if (LogFilter.logDev)
						{
							Debug.Log(string.Concat(new object[]
							{
								"Server event: host=",
								m_ServerHostId,
								" event=",
								networkEventType,
								" error=",
								b
							}));
						}
					}
					switch (networkEventType)
					{
						case NetworkEventType.DataEvent:
							HandleData(connectionId, channelId, receivedSize, b);
							break;
						case NetworkEventType.ConnectEvent:
							HandleConnect(connectionId, b);
							break;
						case NetworkEventType.DisconnectEvent:
							HandleDisconnect(connectionId, b);
							break;
						case NetworkEventType.Nothing:
							break;
						default:
							if (LogFilter.logError)
							{
								Debug.LogError("Unknown network message type received: " + networkEventType);
							}
							break;
					}
				}
				while (networkEventType != NetworkEventType.Nothing);
				UpdateConnections();
			}
		}

		public NetworkConnection FindConnection(int connectionId)
		{
			NetworkConnection result;
			if (connectionId < 0 || connectionId >= m_Connections.Count)
			{
				result = null;
			}
			else
			{
				result = m_Connections[connectionId];
			}
			return result;
		}

		public bool SetConnectionAtIndex(NetworkConnection conn)
		{
			while (m_Connections.Count <= conn.connectionId)
			{
				m_Connections.Add(null);
			}
			bool result;
			if (m_Connections[conn.connectionId] != null)
			{
				result = false;
			}
			else
			{
				m_Connections[conn.connectionId] = conn;
				conn.SetHandlers(m_MessageHandlers);
				result = true;
			}
			return result;
		}

		public bool RemoveConnectionAtIndex(int connectionId)
		{
			bool result;
			if (connectionId < 0 || connectionId >= m_Connections.Count)
			{
				result = false;
			}
			else
			{
				m_Connections[connectionId] = null;
				result = true;
			}
			return result;
		}

		private void HandleConnect(int connectionId, byte error)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkServerSimple accepted client:" + connectionId);
			}
			if (error != 0)
			{
				OnConnectError(connectionId, error);
			}
			else
			{
				string networkAddress;
				int num;
				NetworkID networkID;
				NodeID nodeID;
				byte lastError;
				NetworkTransport.GetConnectionInfo(m_ServerHostId, connectionId, out networkAddress, out num, out networkID, out nodeID, out lastError);
				NetworkConnection networkConnection = (NetworkConnection)Activator.CreateInstance(m_NetworkConnectionClass);
				networkConnection.SetHandlers(m_MessageHandlers);
				networkConnection.Initialize(networkAddress, m_ServerHostId, connectionId, m_HostTopology);
				networkConnection.lastError = (NetworkError)lastError;
				while (m_Connections.Count <= connectionId)
				{
					m_Connections.Add(null);
				}
				m_Connections[connectionId] = networkConnection;
				OnConnected(networkConnection);
			}
		}

		private void HandleDisconnect(int connectionId, byte error)
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("NetworkServerSimple disconnect client:" + connectionId);
			}
			NetworkConnection networkConnection = FindConnection(connectionId);
			if (networkConnection != null)
			{
				networkConnection.lastError = (NetworkError)error;
				if (error != 0)
				{
					if (error != 6)
					{
						m_Connections[connectionId] = null;
						if (LogFilter.logError)
						{
							Debug.LogError(string.Concat(new object[]
							{
								"Server client disconnect error, connectionId: ",
								connectionId,
								" error: ",
								(NetworkError)error
							}));
						}
						OnDisconnectError(networkConnection, error);
						return;
					}
				}
				networkConnection.Disconnect();
				m_Connections[connectionId] = null;
				if (LogFilter.logDebug)
				{
					Debug.Log("Server lost client:" + connectionId);
				}
				OnDisconnected(networkConnection);
			}
		}

		private void HandleData(int connectionId, int channelId, int receivedSize, byte error)
		{
			NetworkConnection networkConnection = FindConnection(connectionId);
			if (networkConnection == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("HandleData Unknown connectionId:" + connectionId);
				}
			}
			else
			{
				networkConnection.lastError = (NetworkError)error;
				if (error != 0)
				{
					OnDataError(networkConnection, error);
				}
				else
				{
					m_MsgReader.SeekZero();
					OnData(networkConnection, receivedSize, channelId);
				}
			}
		}

		public void SendBytesTo(int connectionId, byte[] bytes, int numBytes, int channelId)
		{
			NetworkConnection networkConnection = FindConnection(connectionId);
			if (networkConnection != null)
			{
				networkConnection.SendBytes(bytes, numBytes, channelId);
			}
		}

		public void SendWriterTo(int connectionId, NetworkWriter writer, int channelId)
		{
			NetworkConnection networkConnection = FindConnection(connectionId);
			if (networkConnection != null)
			{
				networkConnection.SendWriter(writer, channelId);
			}
		}

		public void Disconnect(int connectionId)
		{
			NetworkConnection networkConnection = FindConnection(connectionId);
			if (networkConnection != null)
			{
				networkConnection.Disconnect();
				m_Connections[connectionId] = null;
			}
		}

		public void DisconnectAllConnections()
		{
			for (int i = 0; i < m_Connections.Count; i++)
			{
				NetworkConnection networkConnection = m_Connections[i];
				if (networkConnection != null)
				{
					networkConnection.Disconnect();
					networkConnection.Dispose();
				}
			}
		}

		public virtual void OnConnectError(int connectionId, byte error)
		{
			Debug.LogError("OnConnectError error:" + error);
		}

		public virtual void OnDataError(NetworkConnection conn, byte error)
		{
			Debug.LogError("OnDataError error:" + error);
		}

		public virtual void OnDisconnectError(NetworkConnection conn, byte error)
		{
			Debug.LogError("OnDisconnectError error:" + error);
		}

		public virtual void OnConnected(NetworkConnection conn)
		{
			conn.InvokeHandlerNoData(32);
		}

		public virtual void OnDisconnected(NetworkConnection conn)
		{
			conn.InvokeHandlerNoData(33);
		}

		public virtual void OnData(NetworkConnection conn, int receivedSize, int channelId)
		{
			conn.TransportReceive(m_MsgBuffer, receivedSize, channelId);
		}

		private bool m_Initialized = false;

		private int m_ListenPort;

		private int m_ServerHostId = -1;

		private int m_RelaySlotId = -1;

		private bool m_UseWebSockets;

		private byte[] m_MsgBuffer = null;

		private NetworkReader m_MsgReader = null;

		private Type m_NetworkConnectionClass = typeof(NetworkConnection);

		private HostTopology m_HostTopology;

		private List<NetworkConnection> m_Connections = new List<NetworkConnection>();

		private ReadOnlyCollection<NetworkConnection> m_ConnectionsReadOnly;

		private NetworkMessageHandlers m_MessageHandlers = new NetworkMessageHandlers();
	}
}
