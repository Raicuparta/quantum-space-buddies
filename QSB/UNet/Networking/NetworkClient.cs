using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace QSB.UNet.Networking
{
	public class NetworkClient
	{
		public NetworkClient()
		{
			if (LogFilter.logDev)
			{
				Debug.Log("Client created version " + Version.Current);
			}
			m_MsgBuffer = new byte[65535];
			m_MsgReader = new NetworkReader(m_MsgBuffer);
			NetworkClient.AddClient(this);
		}

		public NetworkClient(NetworkConnection conn)
		{
			if (LogFilter.logDev)
			{
				Debug.Log("Client created version " + Version.Current);
			}
			m_MsgBuffer = new byte[65535];
			m_MsgReader = new NetworkReader(m_MsgBuffer);
			NetworkClient.AddClient(this);
			NetworkClient.SetActive(true);
			m_Connection = conn;
			m_AsyncConnect = NetworkClient.ConnectState.Connected;
			conn.SetHandlers(m_MessageHandlers);
			RegisterSystemHandlers(false);
		}

		public static List<NetworkClient> allClients
		{
			get
			{
				return NetworkClient.s_Clients;
			}
		}

		public static bool active
		{
			get
			{
				return NetworkClient.s_IsActive;
			}
		}

		internal void SetHandlers(NetworkConnection conn)
		{
			conn.SetHandlers(m_MessageHandlers);
		}

		public string serverIp
		{
			get
			{
				return m_ServerIp;
			}
		}

		public int serverPort
		{
			get
			{
				return m_ServerPort;
			}
		}

		public NetworkConnection connection
		{
			get
			{
				return m_Connection;
			}
		}

		[Obsolete("Moved to NetworkMigrationManager.")]
		public PeerInfoMessage[] peers
		{
			get
			{
				return null;
			}
		}

		internal int hostId
		{
			get
			{
				return m_ClientId;
			}
		}

		public Dictionary<short, NetworkMessageDelegate> handlers
		{
			get
			{
				return m_MessageHandlers.GetHandlers();
			}
		}

		public int numChannels
		{
			get
			{
				return m_HostTopology.DefaultConfig.ChannelCount;
			}
		}

		public HostTopology hostTopology
		{
			get
			{
				return m_HostTopology;
			}
		}

		public int hostPort
		{
			get
			{
				return m_HostPort;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("Port must not be a negative number.");
				}
				if (value > 65535)
				{
					throw new ArgumentException("Port must not be greater than 65535.");
				}
				m_HostPort = value;
			}
		}

		public bool isConnected
		{
			get
			{
				return m_AsyncConnect == NetworkClient.ConnectState.Connected;
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

		public bool ReconnectToNewHost(string serverIp, int serverPort)
		{
			bool result;
			if (!NetworkClient.active)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Reconnect - NetworkClient must be active");
				}
				result = false;
			}
			else if (m_Connection == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Reconnect - no old connection exists");
				}
				result = false;
			}
			else
			{
				if (LogFilter.logInfo)
				{
					Debug.Log(string.Concat(new object[]
					{
						"NetworkClient Reconnect ",
						serverIp,
						":",
						serverPort
					}));
				}
				ClientScene.HandleClientDisconnect(m_Connection);
				ClientScene.ClearLocalPlayers();
				m_Connection.Disconnect();
				m_Connection = null;
				m_ClientId = NetworkTransport.AddHost(m_HostTopology, m_HostPort);
				m_ServerPort = serverPort;
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
					m_ServerIp = serverIp;
					m_AsyncConnect = NetworkClient.ConnectState.Resolved;
				}
				else if (serverIp.Equals("127.0.0.1") || serverIp.Equals("localhost"))
				{
					m_ServerIp = "127.0.0.1";
					m_AsyncConnect = NetworkClient.ConnectState.Resolved;
				}
				else
				{
					if (LogFilter.logDebug)
					{
						Debug.Log("Async DNS START:" + serverIp);
					}
					m_AsyncConnect = NetworkClient.ConnectState.Resolving;
					Dns.BeginGetHostAddresses(serverIp, new AsyncCallback(NetworkClient.GetHostAddressesCallback), this);
				}
				result = true;
			}
			return result;
		}

		public bool ReconnectToNewHost(EndPoint secureTunnelEndPoint)
		{
			bool result;
			if (!NetworkClient.active)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Reconnect - NetworkClient must be active");
				}
				result = false;
			}
			else if (m_Connection == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Reconnect - no old connection exists");
				}
				result = false;
			}
			else
			{
				if (LogFilter.logInfo)
				{
					Debug.Log("NetworkClient Reconnect to remoteSockAddr");
				}
				ClientScene.HandleClientDisconnect(m_Connection);
				ClientScene.ClearLocalPlayers();
				m_Connection.Disconnect();
				m_Connection = null;
				m_ClientId = NetworkTransport.AddHost(m_HostTopology, m_HostPort);
				if (secureTunnelEndPoint == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("Reconnect failed: null endpoint passed in");
					}
					m_AsyncConnect = NetworkClient.ConnectState.Failed;
					result = false;
				}
				else if (secureTunnelEndPoint.AddressFamily != AddressFamily.InterNetwork && secureTunnelEndPoint.AddressFamily != AddressFamily.InterNetworkV6)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("Reconnect failed: Endpoint AddressFamily must be either InterNetwork or InterNetworkV6");
					}
					m_AsyncConnect = NetworkClient.ConnectState.Failed;
					result = false;
				}
				else
				{
					string fullName = secureTunnelEndPoint.GetType().FullName;
					if (fullName == "System.Net.IPEndPoint")
					{
						IPEndPoint ipendPoint = (IPEndPoint)secureTunnelEndPoint;
						Connect(ipendPoint.Address.ToString(), ipendPoint.Port);
						result = (m_AsyncConnect != NetworkClient.ConnectState.Failed);
					}
					else if (fullName != "UnityEngine.XboxOne.XboxOneEndPoint" && fullName != "UnityEngine.PS4.SceEndPoint")
					{
						if (LogFilter.logError)
						{
							Debug.LogError("Reconnect failed: invalid Endpoint (not IPEndPoint or XboxOneEndPoint or SceEndPoint)");
						}
						m_AsyncConnect = NetworkClient.ConnectState.Failed;
						result = false;
					}
					else
					{
						byte b = 0;
						m_RemoteEndPoint = secureTunnelEndPoint;
						m_AsyncConnect = NetworkClient.ConnectState.Connecting;
						try
						{
							m_ClientConnectionId = NetworkTransport.ConnectEndPoint(m_ClientId, m_RemoteEndPoint, 0, out b);
						}
						catch (Exception arg)
						{
							if (LogFilter.logError)
							{
								Debug.LogError("Reconnect failed: Exception when trying to connect to EndPoint: " + arg);
							}
							m_AsyncConnect = NetworkClient.ConnectState.Failed;
							return false;
						}
						if (m_ClientConnectionId == 0)
						{
							if (LogFilter.logError)
							{
								Debug.LogError("Reconnect failed: Unable to connect to EndPoint (" + b + ")");
							}
							m_AsyncConnect = NetworkClient.ConnectState.Failed;
							result = false;
						}
						else
						{
							m_Connection = (NetworkConnection)Activator.CreateInstance(m_NetworkConnectionClass);
							m_Connection.SetHandlers(m_MessageHandlers);
							m_Connection.Initialize(m_ServerIp, m_ClientId, m_ClientConnectionId, m_HostTopology);
							result = true;
						}
					}
				}
			}
			return result;
		}

		public void ConnectWithSimulator(string serverIp, int serverPort, int latency, float packetLoss)
		{
			m_UseSimulator = true;
			m_SimulatedLatency = latency;
			m_PacketLoss = packetLoss;
			Connect(serverIp, serverPort);
		}

		private static bool IsValidIpV6(string address)
		{
			foreach (char c in address)
			{
				if (c != ':' && (c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
				{
					return false;
				}
			}
			return true;
		}

		public void Connect(string serverIp, int serverPort)
		{
			PrepareForConnect();
			if (LogFilter.logDebug)
				Debug.Log((object)("Client Connect: " + serverIp + ":" + (object)serverPort));
			string hostNameOrAddress = serverIp;
			m_ServerPort = serverPort;
			if (Application.platform == RuntimePlatform.WebGLPlayer)
			{
				m_ServerIp = hostNameOrAddress;
				m_AsyncConnect = NetworkClient.ConnectState.Resolved;
			}
			else if (serverIp.Equals("127.0.0.1") || serverIp.Equals("localhost"))
			{
				m_ServerIp = "127.0.0.1";
				m_AsyncConnect = NetworkClient.ConnectState.Resolved;
			}
			else if (serverIp.IndexOf(":") != -1 && NetworkClient.IsValidIpV6(serverIp))
			{
				m_ServerIp = serverIp;
				m_AsyncConnect = NetworkClient.ConnectState.Resolved;
			}
			else
			{
				if (LogFilter.logDebug)
					Debug.Log((object)("Async DNS START:" + hostNameOrAddress));
				m_RequestedServerHost = hostNameOrAddress;
				m_AsyncConnect = NetworkClient.ConnectState.Resolving;
				Dns.BeginGetHostAddresses(hostNameOrAddress, new AsyncCallback(NetworkClient.GetHostAddressesCallback), (object)this);
			}
		}

		public void Connect(EndPoint secureTunnelEndPoint)
		{
			bool usePlatformSpecificProtocols = NetworkTransport.DoesEndPointUsePlatformProtocols(secureTunnelEndPoint);
			PrepareForConnect(usePlatformSpecificProtocols);
			if (LogFilter.logDebug)
			{
				Debug.Log("Client Connect to remoteSockAddr");
			}
			if (secureTunnelEndPoint == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Connect failed: null endpoint passed in");
				}
				m_AsyncConnect = NetworkClient.ConnectState.Failed;
			}
			else if (secureTunnelEndPoint.AddressFamily != AddressFamily.InterNetwork && secureTunnelEndPoint.AddressFamily != AddressFamily.InterNetworkV6)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Connect failed: Endpoint AddressFamily must be either InterNetwork or InterNetworkV6");
				}
				m_AsyncConnect = NetworkClient.ConnectState.Failed;
			}
			else
			{
				string fullName = secureTunnelEndPoint.GetType().FullName;
				if (fullName == "System.Net.IPEndPoint")
				{
					IPEndPoint ipendPoint = (IPEndPoint)secureTunnelEndPoint;
					Connect(ipendPoint.Address.ToString(), ipendPoint.Port);
				}
				else if (fullName != "UnityEngine.XboxOne.XboxOneEndPoint" && fullName != "UnityEngine.PS4.SceEndPoint" && fullName != "UnityEngine.PSVita.SceEndPoint")
				{
					if (LogFilter.logError)
					{
						Debug.LogError("Connect failed: invalid Endpoint (not IPEndPoint or XboxOneEndPoint or SceEndPoint)");
					}
					m_AsyncConnect = NetworkClient.ConnectState.Failed;
				}
				else
				{
					byte b = 0;
					m_RemoteEndPoint = secureTunnelEndPoint;
					m_AsyncConnect = NetworkClient.ConnectState.Connecting;
					try
					{
						m_ClientConnectionId = NetworkTransport.ConnectEndPoint(m_ClientId, m_RemoteEndPoint, 0, out b);
					}
					catch (Exception arg)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("Connect failed: Exception when trying to connect to EndPoint: " + arg);
						}
						m_AsyncConnect = NetworkClient.ConnectState.Failed;
						return;
					}
					if (m_ClientConnectionId == 0)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("Connect failed: Unable to connect to EndPoint (" + b + ")");
						}
						m_AsyncConnect = NetworkClient.ConnectState.Failed;
					}
					else
					{
						m_Connection = (NetworkConnection)Activator.CreateInstance(m_NetworkConnectionClass);
						m_Connection.SetHandlers(m_MessageHandlers);
						m_Connection.Initialize(m_ServerIp, m_ClientId, m_ClientConnectionId, m_HostTopology);
					}
				}
			}
		}

		private void PrepareForConnect()
		{
			PrepareForConnect(false);
		}

		private void PrepareForConnect(bool usePlatformSpecificProtocols)
		{
			NetworkClient.SetActive(true);
			RegisterSystemHandlers(false);
			if (m_HostTopology == null)
			{
				ConnectionConfig connectionConfig = new ConnectionConfig();
				connectionConfig.AddChannel(QosType.ReliableSequenced);
				connectionConfig.AddChannel(QosType.Unreliable);
				connectionConfig.UsePlatformSpecificProtocols = usePlatformSpecificProtocols;
				m_HostTopology = new HostTopology(connectionConfig, 8);
			}
			if (m_UseSimulator)
			{
				int num = m_SimulatedLatency / 3 - 1;
				if (num < 1)
				{
					num = 1;
				}
				int num2 = m_SimulatedLatency * 3;
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"AddHost Using Simulator ",
						num,
						"/",
						num2
					}));
				}
				m_ClientId = NetworkTransport.AddHostWithSimulator(m_HostTopology, num, num2, m_HostPort);
			}
			else
			{
				m_ClientId = NetworkTransport.AddHost(m_HostTopology, m_HostPort);
			}
		}

		internal static void GetHostAddressesCallback(IAsyncResult ar)
		{
			try
			{
				IPAddress[] array = Dns.EndGetHostAddresses(ar);
				NetworkClient networkClient = (NetworkClient)ar.AsyncState;
				if (array.Length == 0)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("DNS lookup failed for:" + networkClient.m_RequestedServerHost);
					}
					networkClient.m_AsyncConnect = NetworkClient.ConnectState.Failed;
				}
				else
				{
					networkClient.m_ServerIp = array[0].ToString();
					networkClient.m_AsyncConnect = NetworkClient.ConnectState.Resolved;
					if (LogFilter.logDebug)
					{
						Debug.Log(string.Concat(new string[]
						{
							"Async DNS Result:",
							networkClient.m_ServerIp,
							" for ",
							networkClient.m_RequestedServerHost,
							": ",
							networkClient.m_ServerIp
						}));
					}
				}
			}
			catch (SocketException ex)
			{
				NetworkClient networkClient2 = (NetworkClient)ar.AsyncState;
				if (LogFilter.logError)
				{
					Debug.LogError("DNS resolution failed: " + ex.GetErrorCode());
				}
				if (LogFilter.logDebug)
				{
					Debug.Log("Exception:" + ex);
				}
				networkClient2.m_AsyncConnect = NetworkClient.ConnectState.Failed;
			}
		}

		internal void ContinueConnect()
		{
			if (m_UseSimulator)
			{
				int num = m_SimulatedLatency / 3;
				if (num < 1)
				{
					num = 1;
				}
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Connect Using Simulator ",
						m_SimulatedLatency / 3,
						"/",
						m_SimulatedLatency
					}));
				}
				ConnectionSimulatorConfig conf = new ConnectionSimulatorConfig(num, m_SimulatedLatency, num, m_SimulatedLatency, m_PacketLoss);
				byte b;
				m_ClientConnectionId = NetworkTransport.ConnectWithSimulator(m_ClientId, m_ServerIp, m_ServerPort, 0, out b, conf);
			}
			else
			{
				byte b;
				m_ClientConnectionId = NetworkTransport.Connect(m_ClientId, m_ServerIp, m_ServerPort, 0, out b);
			}
			m_Connection = (NetworkConnection)Activator.CreateInstance(m_NetworkConnectionClass);
			m_Connection.SetHandlers(m_MessageHandlers);
			m_Connection.Initialize(m_ServerIp, m_ClientId, m_ClientConnectionId, m_HostTopology);
		}

		public virtual void Disconnect()
		{
			m_AsyncConnect = NetworkClient.ConnectState.Disconnected;
			ClientScene.HandleClientDisconnect(m_Connection);
			if (m_Connection != null)
			{
				m_Connection.Disconnect();
				m_Connection.Dispose();
				m_Connection = null;
				if (m_ClientId != -1)
				{
					NetworkTransport.RemoveHost(m_ClientId);
					m_ClientId = -1;
				}
			}
		}

		public bool Send(short msgType, MessageBase msg)
		{
			bool result;
			if (m_Connection != null)
			{
				if (m_AsyncConnect != NetworkClient.ConnectState.Connected)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkClient Send when not connected to a server");
					}
					result = false;
				}
				else
				{
					result = m_Connection.Send(msgType, msg);
				}
			}
			else
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkClient Send with no connection");
				}
				result = false;
			}
			return result;
		}

		public bool SendWriter(NetworkWriter writer, int channelId)
		{
			bool result;
			if (m_Connection != null)
			{
				if (m_AsyncConnect != NetworkClient.ConnectState.Connected)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkClient SendWriter when not connected to a server");
					}
					result = false;
				}
				else
				{
					result = m_Connection.SendWriter(writer, channelId);
				}
			}
			else
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkClient SendWriter with no connection");
				}
				result = false;
			}
			return result;
		}

		public bool SendBytes(byte[] data, int numBytes, int channelId)
		{
			bool result;
			if (m_Connection != null)
			{
				if (m_AsyncConnect != NetworkClient.ConnectState.Connected)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkClient SendBytes when not connected to a server");
					}
					result = false;
				}
				else
				{
					result = m_Connection.SendBytes(data, numBytes, channelId);
				}
			}
			else
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkClient SendBytes with no connection");
				}
				result = false;
			}
			return result;
		}

		public bool SendUnreliable(short msgType, MessageBase msg)
		{
			bool result;
			if (m_Connection != null)
			{
				if (m_AsyncConnect != NetworkClient.ConnectState.Connected)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkClient SendUnreliable when not connected to a server");
					}
					result = false;
				}
				else
				{
					result = m_Connection.SendUnreliable(msgType, msg);
				}
			}
			else
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkClient SendUnreliable with no connection");
				}
				result = false;
			}
			return result;
		}

		public bool SendByChannel(short msgType, MessageBase msg, int channelId)
		{
			bool result;
			if (m_Connection != null)
			{
				if (m_AsyncConnect != NetworkClient.ConnectState.Connected)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkClient SendByChannel when not connected to a server");
					}
					result = false;
				}
				else
				{
					result = m_Connection.SendByChannel(msgType, msg, channelId);
				}
			}
			else
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkClient SendByChannel with no connection");
				}
				result = false;
			}
			return result;
		}

		public void SetMaxDelay(float seconds)
		{
			if (m_Connection == null)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("SetMaxDelay failed, not connected.");
				}
			}
			else
			{
				m_Connection.SetMaxDelay(seconds);
			}
		}

		public void Shutdown()
		{
			if (LogFilter.logDebug)
			{
				Debug.Log("Shutting down client " + m_ClientId);
			}
			if (m_ClientId != -1)
			{
				NetworkTransport.RemoveHost(m_ClientId);
				m_ClientId = -1;
			}
			NetworkClient.RemoveClient(this);
			if (NetworkClient.s_Clients.Count == 0)
			{
				NetworkClient.SetActive(false);
			}
		}

		internal virtual void Update()
		{
			if (m_ClientId != -1)
			{
				switch (m_AsyncConnect)
				{
					case NetworkClient.ConnectState.None:
					case NetworkClient.ConnectState.Resolving:
					case NetworkClient.ConnectState.Disconnected:
						return;
					case NetworkClient.ConnectState.Resolved:
						m_AsyncConnect = NetworkClient.ConnectState.Connecting;
						ContinueConnect();
						return;
					case NetworkClient.ConnectState.Failed:
						GenerateConnectError(11);
						m_AsyncConnect = NetworkClient.ConnectState.Disconnected;
						return;
				}
				if (m_Connection != null)
				{
					if ((int)Time.time != m_StatResetTime)
					{
						m_Connection.ResetStats();
						m_StatResetTime = (int)Time.time;
					}
				}
				int num = 0;
				byte b;
				for (; ; )
				{
					int num2;
					int channelId;
					int numBytes;
					NetworkEventType networkEventType = (NetworkEventType)NetworkTransport.ReceiveFromHost(m_ClientId, out num2, out channelId, m_MsgBuffer, (int)((ushort)m_MsgBuffer.Length), out numBytes, out b);
					if (m_Connection != null)
					{
						m_Connection.lastError = (NetworkError)b;
					}
					if (networkEventType != NetworkEventType.Nothing)
					{
						if (LogFilter.logDev)
						{
							Debug.Log(string.Concat(new object[]
							{
								"Client event: host=",
								m_ClientId,
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
							if (b != 0)
							{
								goto Block_11;
							}
							m_MsgReader.SeekZero();
							m_Connection.TransportReceive(m_MsgBuffer, numBytes, channelId);
							break;
						case NetworkEventType.ConnectEvent:
							if (LogFilter.logDebug)
							{
								Debug.Log("Client connected");
							}
							if (b != 0)
							{
								goto Block_10;
							}
							m_AsyncConnect = NetworkClient.ConnectState.Connected;
							m_Connection.InvokeHandlerNoData(32);
							break;
						case NetworkEventType.DisconnectEvent:
							if (LogFilter.logDebug)
							{
								Debug.Log("Client disconnected");
							}
							m_AsyncConnect = NetworkClient.ConnectState.Disconnected;
							if (b != 0)
							{
								if (b != 6)
								{
									GenerateDisconnectError((int)b);
								}
							}
							ClientScene.HandleClientDisconnect(m_Connection);
							if (m_Connection != null)
							{
								m_Connection.InvokeHandlerNoData(33);
							}
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
					if (++num >= 500)
					{
						goto Block_17;
					}
					if (m_ClientId == -1)
					{
						goto Block_19;
					}
					if (networkEventType == NetworkEventType.Nothing)
					{
						goto IL_2C6;
					}
				}
				Block_10:
				GenerateConnectError((int)b);
				return;
				Block_11:
				GenerateDataError((int)b);
				return;
				Block_17:
				if (LogFilter.logDebug)
				{
					Debug.Log("MaxEventsPerFrame hit (" + 500 + ")");
				}
				Block_19:
				IL_2C6:
				if (m_Connection != null && m_AsyncConnect == NetworkClient.ConnectState.Connected)
				{
					m_Connection.FlushChannels();
				}
			}
		}

		private void GenerateConnectError(int error)
		{
			if (LogFilter.logError)
			{
				Debug.LogError("UNet Client Error Connect Error: " + error);
			}
			GenerateError(error);
		}

		private void GenerateDataError(int error)
		{
			if (LogFilter.logError)
			{
				Debug.LogError("UNet Client Data Error: " + (NetworkError)error);
			}
			GenerateError(error);
		}

		private void GenerateDisconnectError(int error)
		{
			if (LogFilter.logError)
			{
				Debug.LogError("UNet Client Disconnect Error: " + (NetworkError)error);
			}
			GenerateError(error);
		}

		private void GenerateError(int error)
		{
			NetworkMessageDelegate handler = m_MessageHandlers.GetHandler(34);
			if (handler == null)
			{
				handler = m_MessageHandlers.GetHandler(34);
			}
			if (handler != null)
			{
				ErrorMessage errorMessage = new ErrorMessage();
				errorMessage.errorCode = error;
				byte[] buffer = new byte[200];
				NetworkWriter writer = new NetworkWriter(buffer);
				errorMessage.Serialize(writer);
				NetworkReader reader = new NetworkReader(buffer);
				handler(new NetworkMessage
				{
					msgType = 34,
					reader = reader,
					conn = m_Connection,
					channelId = 0
				});
			}
		}

		public void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
		{
			numMsgs = 0;
			numBufferedMsgs = 0;
			numBytes = 0;
			lastBufferedPerSecond = 0;
			if (m_Connection != null)
			{
				m_Connection.GetStatsOut(out numMsgs, out numBufferedMsgs, out numBytes, out lastBufferedPerSecond);
			}
		}

		public void GetStatsIn(out int numMsgs, out int numBytes)
		{
			numMsgs = 0;
			numBytes = 0;
			if (m_Connection != null)
			{
				m_Connection.GetStatsIn(out numMsgs, out numBytes);
			}
		}

		public Dictionary<short, NetworkConnection.PacketStat> GetConnectionStats()
		{
			Dictionary<short, NetworkConnection.PacketStat> result;
			if (m_Connection == null)
			{
				result = null;
			}
			else
			{
				result = m_Connection.packetStats;
			}
			return result;
		}

		public void ResetConnectionStats()
		{
			if (m_Connection != null)
			{
				m_Connection.ResetStats();
			}
		}

		public int GetRTT()
		{
			int result;
			if (m_ClientId == -1)
			{
				result = 0;
			}
			else
			{
				byte b;
				result = NetworkTransport.GetCurrentRTT(m_ClientId, m_ClientConnectionId, out b);
			}
			return result;
		}

		internal void RegisterSystemHandlers(bool localClient)
		{
			ClientScene.RegisterSystemHandlers(this, localClient);
			RegisterHandlerSafe((short)14, new NetworkMessageDelegate(OnCRC));
		}

		private void OnCRC(NetworkMessage netMsg)
		{
			netMsg.ReadMessage<CRCMessage>(NetworkClient.s_CRCMessage);
			NetworkCRC.Validate(NetworkClient.s_CRCMessage.scripts, numChannels);
		}

		public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
		{
			m_MessageHandlers.RegisterHandler(msgType, handler);
		}

		public void RegisterHandlerSafe(short msgType, NetworkMessageDelegate handler)
		{
			m_MessageHandlers.RegisterHandlerSafe(msgType, handler);
		}

		public void UnregisterHandler(short msgType)
		{
			m_MessageHandlers.UnregisterHandler(msgType);
		}

		public static Dictionary<short, NetworkConnection.PacketStat> GetTotalConnectionStats()
		{
			Dictionary<short, NetworkConnection.PacketStat> dictionary = new Dictionary<short, NetworkConnection.PacketStat>();
			for (int i = 0; i < NetworkClient.s_Clients.Count; i++)
			{
				NetworkClient networkClient = NetworkClient.s_Clients[i];
				Dictionary<short, NetworkConnection.PacketStat> connectionStats = networkClient.GetConnectionStats();
				foreach (short key in connectionStats.Keys)
				{
					if (dictionary.ContainsKey(key))
					{
						NetworkConnection.PacketStat packetStat = dictionary[key];
						packetStat.count += connectionStats[key].count;
						packetStat.bytes += connectionStats[key].bytes;
						dictionary[key] = packetStat;
					}
					else
					{
						dictionary[key] = new NetworkConnection.PacketStat(connectionStats[key]);
					}
				}
			}
			return dictionary;
		}

		internal static void AddClient(NetworkClient client)
		{
			NetworkClient.s_Clients.Add(client);
		}

		internal static bool RemoveClient(NetworkClient client)
		{
			return NetworkClient.s_Clients.Remove(client);
		}

		internal static void UpdateClients()
		{
			for (int i = 0; i < NetworkClient.s_Clients.Count; i++)
			{
				if (NetworkClient.s_Clients[i] != null)
				{
					NetworkClient.s_Clients[i].Update();
				}
				else
				{
					NetworkClient.s_Clients.RemoveAt(i);
				}
			}
		}

		public static void ShutdownAll()
		{
			while (NetworkClient.s_Clients.Count != 0)
			{
				NetworkClient.s_Clients[0].Shutdown();
			}
			NetworkClient.s_Clients = new List<NetworkClient>();
			NetworkClient.s_IsActive = false;
			ClientScene.Shutdown();
		}

		internal static void SetActive(bool state)
		{
			if (!NetworkClient.s_IsActive && state)
			{
				NetworkTransport.Init();
			}
			NetworkClient.s_IsActive = state;
		}

		private Type m_NetworkConnectionClass = typeof(NetworkConnection);

		private const int k_MaxEventsPerFrame = 500;

		private static List<NetworkClient> s_Clients = new List<NetworkClient>();

		private static bool s_IsActive;

		private HostTopology m_HostTopology;

		private int m_HostPort;

		private bool m_UseSimulator;

		private int m_SimulatedLatency;

		private float m_PacketLoss;

		private string m_ServerIp = "";

		private int m_ServerPort;

		private int m_ClientId = -1;

		private int m_ClientConnectionId = -1;

		private int m_StatResetTime;

		private EndPoint m_RemoteEndPoint;

		private static CRCMessage s_CRCMessage = new CRCMessage();

		private NetworkMessageHandlers m_MessageHandlers = new NetworkMessageHandlers();

		protected NetworkConnection m_Connection;

		private byte[] m_MsgBuffer;

		private NetworkReader m_MsgReader;

		protected NetworkClient.ConnectState m_AsyncConnect = NetworkClient.ConnectState.None;

		private string m_RequestedServerHost = "";

		protected enum ConnectState
		{
			None,
			Resolving,
			Resolved,
			Connecting,
			Connected,
			Disconnected,
			Failed
		}
	}
}
