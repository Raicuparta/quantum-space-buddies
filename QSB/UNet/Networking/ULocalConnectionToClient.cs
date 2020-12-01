using System;

namespace QSB.UNet.Networking
{
	internal class ULocalConnectionToClient : NetworkConnection
	{
		public ULocalConnectionToClient(LocalClient localClient)
		{
			address = "localClient";
			m_LocalClient = localClient;
		}

		public LocalClient localClient
		{
			get
			{
				return m_LocalClient;
			}
		}

		public override bool Send(short msgType, MessageBase msg)
		{
			m_LocalClient.InvokeHandlerOnClient(msgType, msg, 0);
			return true;
		}

		public override bool SendUnreliable(short msgType, MessageBase msg)
		{
			m_LocalClient.InvokeHandlerOnClient(msgType, msg, 1);
			return true;
		}

		public override bool SendByChannel(short msgType, MessageBase msg, int channelId)
		{
			m_LocalClient.InvokeHandlerOnClient(msgType, msg, channelId);
			return true;
		}

		public override bool SendBytes(byte[] bytes, int numBytes, int channelId)
		{
			m_LocalClient.InvokeBytesOnClient(bytes, channelId);
			return true;
		}

		public override bool SendWriter(NetworkWriter writer, int channelId)
		{
			m_LocalClient.InvokeBytesOnClient(writer.AsArray(), channelId);
			return true;
		}

		public override void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
		{
			numMsgs = 0;
			numBufferedMsgs = 0;
			numBytes = 0;
			lastBufferedPerSecond = 0;
		}

		public override void GetStatsIn(out int numMsgs, out int numBytes)
		{
			numMsgs = 0;
			numBytes = 0;
		}

		private LocalClient m_LocalClient;
	}
}
