using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Internal;
using OWML.ModHelper.Events;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Reflection;

namespace QSB.UNet.Networking
{
	public sealed class NetworkTransport
	{
		private NetworkTransport()
		{

		}

		public static void Init()
		{
			UnityEngine.Networking.NetworkTransport.Init();
		}

		public static void Init(GlobalConfig config)
		{
			UnityEngine.Networking.NetworkTransport.Init(new UnityEngine.Networking.GlobalConfig());
		}

		public static void Shutdown()
		{
			UnityEngine.Networking.NetworkTransport.Shutdown();
		}

		private static int GetMaxPacketSize()
		{
			return (int)typeof(UnityEngine.Networking.NetworkTransport).GetMethod("GetMaxPacketSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
		}

		private static void CheckTopology(HostTopology topology)
		{
			int maxPacketSize = GetMaxPacketSize();
			if ((int)topology.DefaultConfig.PacketSize > maxPacketSize)
			{
				throw new ArgumentOutOfRangeException("Default config: packet size should be less than packet size defined in global config: " + maxPacketSize.ToString());
			}
			for (int i = 0; i < topology.SpecialConnectionConfigs.Count; i++)
			{
				if ((int)topology.SpecialConnectionConfigs[i].PacketSize > maxPacketSize)
				{
					throw new ArgumentOutOfRangeException("Special config " + i.ToString() + ": packet size should be less than packet size defined in global config: " + maxPacketSize.ToString());
				}
			}
		}

		private static int AddHostWrapper(HostTopologyInternal topologyInt, string ip, int port, int minTimeout, int maxTimeout)
		{
			return (int)typeof(UnityEngine.Networking.NetworkTransport).GetMethod("AddHostWrapper").Invoke(null, new object[] {topologyInt, ip, port, minTimeout, maxTimeout });
		}

		private static int AddHostWrapperWithoutIp(HostTopologyInternal topologyInt, int port, int minTimeout, int maxTimeout)
		{
			var assembly = Assembly.LoadFile($@"{QSB.Helper.OwmlConfig.GamePath}OuterWilds_Data\Managed\UnityEngine.UNETModule.dll");
			var type = assembly.GetType("UnityEngine.Networking.HostTopologyInternal");
			var instance = Activator.CreateInstance(type, topologyInt.storedTopology.ToUnity());
			return (int)typeof(UnityEngine.Networking.NetworkTransport).GetMethod("AddHostWrapperWithoutIp", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { instance, port, minTimeout, maxTimeout });
		}

		public static int AddHost(HostTopology topology, int port)
		{
			string ip = null;
			return NetworkTransport.AddHost(topology, port, ip);
		}

		public static int AddHost(HostTopology topology, [DefaultValue("0")] int port, [DefaultValue("null")] string ip)
		{
			if (topology == null)
			{
				throw new NullReferenceException("topology is not defined");
			}
			CheckTopology(topology);
			int result;
			if (ip == null)
			{
				result = AddHostWrapperWithoutIp(new HostTopologyInternal(topology), port, 0, 0);
			}
			else
			{
				result = AddHostWrapper(new HostTopologyInternal(topology), ip, port, 0, 0);
			}
			return result;
		}

		public static bool RemoveHost(int hostId)
		{
			return UnityEngine.Networking.NetworkTransport.RemoveHost(hostId);
		}

		public static int Connect(int hostId, string address, int port, int exeptionConnectionId, out byte error)
		{
			return UnityEngine.Networking.NetworkTransport.Connect(hostId, address, port, exeptionConnectionId, out error);
		}

		public static bool Disconnect(int hostId, int connectionId, out byte error)
		{
			return UnityEngine.Networking.NetworkTransport.Disconnect(hostId, connectionId, out error);
		}

		public static int ConnectEndPoint(int hostId, EndPoint endPoint, int exceptionConnectionId, out byte error)
		{
			error = 0;
			byte[] array = new byte[]
			{
				95,
				36,
				19,
				246
			};
			if (endPoint == null)
			{
				throw new NullReferenceException("Null EndPoint provided");
			}
			if (endPoint.GetType().FullName != "UnityEngine.XboxOne.XboxOneEndPoint" && endPoint.GetType().FullName != "UnityEngine.PS4.SceEndPoint" && endPoint.GetType().FullName != "UnityEngine.PSVita.SceEndPoint")
			{
				throw new ArgumentException("Endpoint of type XboxOneEndPoint or SceEndPoint  required");
			}
			int result;
			if (endPoint.GetType().FullName == "UnityEngine.XboxOne.XboxOneEndPoint")
			{
				if (endPoint.AddressFamily != AddressFamily.InterNetworkV6)
				{
					throw new ArgumentException("XboxOneEndPoint has an invalid family");
				}
				SocketAddress socketAddress = endPoint.Serialize();
				if (socketAddress.Size != 14)
				{
					throw new ArgumentException("XboxOneEndPoint has an invalid size");
				}
				if (socketAddress[0] != 0 || socketAddress[1] != 0)
				{
					throw new ArgumentException("XboxOneEndPoint has an invalid family signature");
				}
				if (socketAddress[2] != array[0] || socketAddress[3] != array[1] || socketAddress[4] != array[2] || socketAddress[5] != array[3])
				{
					throw new ArgumentException("XboxOneEndPoint has an invalid signature");
				}
				byte[] array2 = new byte[8];
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i] = socketAddress[6 + i];
				}
				IntPtr intPtr = new IntPtr(BitConverter.ToInt64(array2, 0));
				if (intPtr == IntPtr.Zero)
				{
					throw new ArgumentException("XboxOneEndPoint has an invalid SOCKET_STORAGE pointer");
				}
				byte[] array3 = new byte[2];
				Marshal.Copy(intPtr, array3, 0, array3.Length);
				AddressFamily addressFamily = (AddressFamily)(((int)array3[1] << 8) + (int)array3[0]);
				if (addressFamily != AddressFamily.InterNetworkV6)
				{
					throw new ArgumentException("XboxOneEndPoint has corrupt or invalid SOCKET_STORAGE pointer");
				}
				result = NetworkTransport.Internal_ConnectEndPoint(hostId, intPtr, 128, exceptionConnectionId, out error);
			}
			else
			{
				SocketAddress socketAddress2 = endPoint.Serialize();
				if (socketAddress2.Size != 16)
				{
					throw new ArgumentException("EndPoint has an invalid size");
				}
				if ((int)socketAddress2[0] != socketAddress2.Size)
				{
					throw new ArgumentException("EndPoint has an invalid size value");
				}
				if (socketAddress2[1] != 2)
				{
					throw new ArgumentException("EndPoint has an invalid family value");
				}
				byte[] array4 = new byte[16];
				for (int j = 0; j < array4.Length; j++)
				{
					array4[j] = socketAddress2[j];
				}
				IntPtr intPtr2 = Marshal.AllocHGlobal(array4.Length);
				Marshal.Copy(array4, 0, intPtr2, array4.Length);
				int num = NetworkTransport.Internal_ConnectEndPoint(hostId, intPtr2, 16, exceptionConnectionId, out error);
				Marshal.FreeHGlobal(intPtr2);
				result = num;
			}
			return result;
		}

		private static int Internal_ConnectEndPoint(int hostId, IntPtr sockAddrStorage, int sockAddrStorageLen, int exceptionConnectionId, out byte error)
		{
			var parameters = new object[] { hostId, sockAddrStorage, sockAddrStorageLen, exceptionConnectionId, null };
			var result = (int)typeof(UnityEngine.Networking.NetworkTransport).GetMethod("Internal_ConnectEndPoint").Invoke(null, parameters);
			error = (byte)parameters[4];
			return result;
		}

		public static void GetConnectionInfo(int hostId, int connectionId, out string address, out int port, out NetworkID network, out NodeID dstNode, out byte error)
		{
			ulong num;
			ushort num2;
			address = NetworkTransport.GetConnectionInfo(hostId, connectionId, out port, out num, out num2, out error);
			network = (NetworkID)num;
			dstNode = (NodeID)num2;
		}

		public static string GetConnectionInfo(int hostId, int connectionId, out int port, out ulong network, out ushort dstNode, out byte error)
		{
			return UnityEngine.Networking.NetworkTransport.GetConnectionInfo(hostId, connectionId, out port, out network, out dstNode, out error);
		}

		public static bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
		{
			if (buffer == null)
			{
				throw new NullReferenceException("send buffer is not initialized");
			}
			return NetworkTransport.SendWrapper(hostId, connectionId, channelId, buffer, size, out error);
		}

		private static bool SendWrapper(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
		{
			var parameters = new object[] { hostId, connectionId, channelId, buffer, size, null };
			var result = (bool)typeof(UnityEngine.Networking.NetworkTransport).GetMethod("SendWrapper").Invoke(null, parameters);
			error = (byte)parameters[5];
			return result;
		}

		public static int AddWebsocketHost(HostTopology topology, int port)
		{
			string ip = null;
			return NetworkTransport.AddWebsocketHost(topology, port, ip);
		}

		public static int AddWebsocketHost(HostTopology topology, int port, [DefaultValue("null")] string ip)
		{
			if (port != 0)
			{
				if (NetworkTransport.IsPortOpen(ip, port))
				{
					throw new InvalidOperationException("Cannot open web socket on port " + port + " It has been already occupied.");
				}
			}
			if (topology == null)
			{
				throw new NullReferenceException("topology is not defined");
			}
			NetworkTransport.CheckTopology(topology);
			int result = 0;
			if (ip == null)
			{
				//result = NetworkTransport.AddWsHostWrapperWithoutIp(new HostTopologyInternal(topology), port);
			}
			else
			{
				//result = NetworkTransport.AddWsHostWrapper(new HostTopologyInternal(topology), ip, port);
			}
			return result;
		}

		private static bool IsPortOpen(string ip, int port)
		{
			TimeSpan timeout = TimeSpan.FromMilliseconds(500.0);
			string host = (ip != null) ? ip : "127.0.0.1";
			try
			{
				using (TcpClient tcpClient = new TcpClient())
				{
					IAsyncResult asyncResult = tcpClient.BeginConnect(host, port, null, null);
					if (!asyncResult.AsyncWaitHandle.WaitOne(timeout))
					{
						return false;
					}
					tcpClient.EndConnect(asyncResult);
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		public static NetworkEventType ReceiveFromHost(int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
		{
			return (NetworkEventType)UnityEngine.Networking.NetworkTransport.ReceiveFromHost(hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);
		}

		public static int GetCurrentRTT(int hostId, int connectionId, out byte error)
		{
			return UnityEngine.Networking.NetworkTransport.GetCurrentRTT(hostId, connectionId, out error);
		}

		public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port)
		{
			string ip = null;
			return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port, ip);
		}

		public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout)
		{
			string ip = null;
			int port = 0;
			return NetworkTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port, ip);
		}

		public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, [DefaultValue("0")] int port, [DefaultValue("null")] string ip)
		{
			if (topology == null)
			{
				throw new NullReferenceException("topology is not defined");
			}
			int result;
			if (ip == null)
			{
				result = NetworkTransport.AddHostWrapperWithoutIp(new HostTopologyInternal(topology), port, minTimeout, maxTimeout);
			}
			else
			{
				result = NetworkTransport.AddHostWrapper(new HostTopologyInternal(topology), ip, port, minTimeout, maxTimeout);
			}
			return result;
		}

		public static int ConnectWithSimulator(int hostId, string address, int port, int exeptionConnectionId, out byte error, ConnectionSimulatorConfig conf)
		{
			return UnityEngine.Networking.NetworkTransport.ConnectWithSimulator(hostId, address, port, exeptionConnectionId, out error, null);
		}

		public static void ConnectAsNetworkHost(int hostId, string address, int port, NetworkID network, SourceID source, NodeID node, out byte error)
		{
			UnityEngine.Networking.NetworkTransport.ConnectAsNetworkHost(hostId, address, port, (UnityEngine.Networking.Types.NetworkID)network, (UnityEngine.Networking.Types.SourceID)source, (UnityEngine.Networking.Types.NodeID)node, out error);
		}

		public static NetworkEventType ReceiveRelayEventFromHost(int hostId, out byte error)
		{
			return (NetworkEventType)UnityEngine.Networking.NetworkTransport.ReceiveRelayEventFromHost(hostId, out error);
		}

		internal static bool DoesEndPointUsePlatformProtocols(EndPoint endPoint)
		{
			if (endPoint.GetType().FullName == "UnityEngine.PS4.SceEndPoint" || endPoint.GetType().FullName == "UnityEngine.PSVita.SceEndPoint")
			{
				SocketAddress socketAddress = endPoint.Serialize();
				if (socketAddress[8] != 0 || socketAddress[9] != 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}
