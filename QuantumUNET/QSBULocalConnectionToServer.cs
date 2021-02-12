using QuantumUNET.Messages;
using QuantumUNET.Transport;
using UnityEngine;

namespace QuantumUNET
{
	internal class QSBULocalConnectionToServer : QSBNetworkConnection
	{
		public QSBULocalConnectionToServer(QSBNetworkServer localServer)
		{
			address = "localServer";
			m_LocalServer = localServer;
		}

		public override bool Send(short msgType, QSBMessageBase msg) =>
			m_LocalServer.InvokeHandlerOnServer(this, msgType, msg, 0);

		public override bool SendUnreliable(short msgType, QSBMessageBase msg) =>
			m_LocalServer.InvokeHandlerOnServer(this, msgType, msg, 1);

		public override bool SendByChannel(short msgType, QSBMessageBase msg, int channelId) =>
			m_LocalServer.InvokeHandlerOnServer(this, msgType, msg, channelId);

		public override bool SendBytes(byte[] bytes, int numBytes, int channelId)
		{
			bool result;
			if (numBytes <= 0)
			{
				Debug.LogError("LocalConnection:SendBytes cannot send zero bytes");
				result = false;
			}
			else
			{
				result = m_LocalServer.InvokeBytes(this, bytes, numBytes, channelId);
			}
			return result;
		}

		public override bool SendWriter(QSBNetworkWriter writer, int channelId) =>
			m_LocalServer.InvokeBytes(this, writer.AsArray(), (short)writer.AsArray().Length, channelId);

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

		private readonly QSBNetworkServer m_LocalServer;
	}
}