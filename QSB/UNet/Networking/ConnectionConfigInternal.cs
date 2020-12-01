using QSB.Utility;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace QSB.UNet.Networking
{
	internal sealed class ConnectionConfigInternal : IDisposable
	{
		object instance;

		private ConnectionConfigInternal()
		{
			var assembly = Assembly.LoadFile($@"{QSB.Helper.OwmlConfig.GamePath}OuterWilds_Data\Managed\UnityEngine.UNETModule.dll");
			var type = assembly.GetType("UnityEngine.Networking.ConnectionConfigInternal");
			instance = Activator.CreateInstance(type, new UnityEngine.Networking.ConnectionConfig());
		}

		public ConnectionConfigInternal(ConnectionConfig config)
		{
			var assembly = Assembly.LoadFile($@"{QSB.Helper.OwmlConfig.GamePath}OuterWilds_Data\Managed\UnityEngine.UNETModule.dll");
			var type = assembly.GetType("UnityEngine.Networking.ConnectionConfigInternal");
			instance = Activator.CreateInstance(type, config.ToUnity());
			if (config == null)
			{
				throw new NullReferenceException("config is not defined");
			}
			DebugLog.DebugWrite("wrapper");
			InitWrapper();
			DebugLog.DebugWrite("packet size");
			InitPacketSize(config.PacketSize);
			DebugLog.DebugWrite("fragment size");
			InitFragmentSize(config.FragmentSize);
			DebugLog.DebugWrite("resend timeout");
			InitResendTimeout(config.ResendTimeout);
			DebugLog.DebugWrite("disconnect timeout");
			InitDisconnectTimeout(config.DisconnectTimeout);
			InitConnectTimeout(config.ConnectTimeout);
			InitMinUpdateTimeout(config.MinUpdateTimeout);
			InitPingTimeout(config.PingTimeout);
			InitReducedPingTimeout(config.ReducedPingTimeout);
			InitAllCostTimeout(config.AllCostTimeout);
			InitNetworkDropThreshold(config.NetworkDropThreshold);
			InitOverflowDropThreshold(config.OverflowDropThreshold);
			InitMaxConnectionAttempt(config.MaxConnectionAttempt);
			InitAckDelay(config.AckDelay);
			InitSendDelay(config.SendDelay);
			InitMaxCombinedReliableMessageSize(config.MaxCombinedReliableMessageSize);
			InitMaxCombinedReliableMessageCount(config.MaxCombinedReliableMessageCount);
			InitMaxSentMessageQueueSize(config.MaxSentMessageQueueSize);
			InitAcksType((int)config.AcksType);
			InitUsePlatformSpecificProtocols(config.UsePlatformSpecificProtocols);
			InitInitialBandwidth(config.InitialBandwidth);
			InitBandwidthPeakFactor(config.BandwidthPeakFactor);
			InitWebSocketReceiveBufferMaxSize(config.WebSocketReceiveBufferMaxSize);
			InitUdpSocketReceiveBufferMaxSize(config.UdpSocketReceiveBufferMaxSize);
			if (config.SSLCertFilePath != null)
			{
				int num = InitSSLCertFilePath(config.SSLCertFilePath);
				if (num != 0)
				{
					throw new ArgumentOutOfRangeException("SSLCertFilePath cannot be > than " + num.ToString());
				}
			}
			if (config.SSLPrivateKeyFilePath != null)
			{
				int num2 = InitSSLPrivateKeyFilePath(config.SSLPrivateKeyFilePath);
				if (num2 != 0)
				{
					throw new ArgumentOutOfRangeException("SSLPrivateKeyFilePath cannot be > than " + num2.ToString());
				}
			}
			if (config.SSLCAFilePath != null)
			{
				int num3 = InitSSLCAFilePath(config.SSLCAFilePath);
				if (num3 != 0)
				{
					throw new ArgumentOutOfRangeException("SSLCAFilePath cannot be > than " + num3.ToString());
				}
			}
			byte b = 0;
			while ((int)b < config.ChannelCount)
			{
				AddChannel(config.GetChannel(b));
				b += 1;
			}
		}

		public void InitWrapper()
		{
			instance.GetType().GetMethod("InitWrapper", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, null);
		}

		public byte AddChannel(QosType value)
		{
			return (byte)instance.GetType().GetMethod("AddChannel", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { (UnityEngine.Networking.QosType)value });
		}

		public QosType GetChannel(int i)
		{
			return (QosType)instance.GetType().GetMethod("GetChannel", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { i });
		}

		public int ChannelSize
		{
			get
			{
				return 0;
			}
		}

		public void InitPacketSize(ushort value)
		{
			instance.GetType().GetMethod("InitPacketSize", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { value });
		}

		public void InitFragmentSize(ushort value)
		{
			instance.GetType().GetMethod("InitFragmentSize", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { value });
		}

		public void InitResendTimeout(uint value)
		{
			instance.GetType().GetMethod("InitResendTimeout", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { value });
		}

		public void InitDisconnectTimeout(uint value)
		{

		}

		public void InitConnectTimeout(uint value)
		{

		}

		public void InitMinUpdateTimeout(uint value)
		{

		}

		public void InitPingTimeout(uint value)
		{

		}

		public void InitReducedPingTimeout(uint value)
		{

		}

		public void InitAllCostTimeout(uint value)
		{

		}

		public void InitNetworkDropThreshold(byte value)
		{

		}

		public void InitOverflowDropThreshold(byte value)
		{

		}

		public void InitMaxConnectionAttempt(byte value)
		{

		}

		public void InitAckDelay(uint value)
		{

		}

		public void InitSendDelay(uint value)
		{

		}

		public void InitMaxCombinedReliableMessageSize(ushort value)
		{

		}

		public void InitMaxCombinedReliableMessageCount(ushort value)
		{

		}

		public void InitMaxSentMessageQueueSize(ushort value)
		{

		}

		public void InitAcksType(int value)
		{

		}

		public void InitUsePlatformSpecificProtocols(bool value)
		{

		}

		public void InitInitialBandwidth(uint value)
		{

		}

		public void InitBandwidthPeakFactor(float value)
		{

		}

		public void InitWebSocketReceiveBufferMaxSize(ushort value)
		{

		}

		public void InitUdpSocketReceiveBufferMaxSize(uint value)
		{

		}

		public int InitSSLCertFilePath(string value)
		{
			return 0;
		}

		public int InitSSLPrivateKeyFilePath(string value)
		{
			return 0;
		}

		public int InitSSLCAFilePath(string value)
		{
			return 0;
		}

		public void Dispose()
		{

		}

		~ConnectionConfigInternal()
		{
			Dispose();
		}

		internal IntPtr m_Ptr;
	}
}
