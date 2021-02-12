using System;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET.Transport
{
	internal struct QSBChannelPacket
	{
		private int m_Position;
		private readonly byte[] m_Buffer;
		private readonly bool m_IsReliable;

		public QSBChannelPacket(int packetSize, bool isReliable)
		{
			m_Position = 0;
			m_Buffer = new byte[packetSize];
			m_IsReliable = isReliable;
		}

		public void Reset() => m_Position = 0;

		public bool IsEmpty() => m_Position == 0;

		public void Write(byte[] bytes, int numBytes)
		{
			Array.Copy(bytes, 0, m_Buffer, m_Position, numBytes);
			m_Position += numBytes;
		}

		public bool HasSpace(int numBytes) => m_Position + numBytes <= m_Buffer.Length;

		public bool SendToTransport(QSBNetworkConnection conn, int channelId)
		{
			var result = true;
			if (!conn.TransportSend(m_Buffer, (ushort)m_Position, channelId, out var b))
			{
				if (!m_IsReliable || b != 4)
				{
					Debug.LogError($"Failed to send internal buffer channel:{channelId} bytesToSend:{m_Position}");
					result = false;
				}
			}
			if (b != 0)
			{
				if (m_IsReliable && b == 4)
				{
					return false;
				}
				Debug.LogError($"Send Error: {(NetworkError)b} channel:{channelId} bytesToSend:{m_Position}");
				result = false;
			}
			m_Position = 0;
			return result;
		}
	}
}