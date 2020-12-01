using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	internal struct ChannelPacket
	{
		public ChannelPacket(int packetSize, bool isReliable)
		{
			m_Position = 0;
			m_Buffer = new byte[packetSize];
			m_IsReliable = isReliable;
		}

		public void Reset()
		{
			m_Position = 0;
		}

		public bool IsEmpty()
		{
			return m_Position == 0;
		}

		public void Write(byte[] bytes, int numBytes)
		{
			Array.Copy(bytes, 0, m_Buffer, m_Position, numBytes);
			m_Position += numBytes;
		}

		public bool HasSpace(int numBytes)
		{
			return m_Position + numBytes <= m_Buffer.Length;
		}

		public bool SendToTransport(NetworkConnection conn, int channelId)
		{
			bool result = true;
			byte b;
			if (!conn.TransportSend(m_Buffer, (int)((ushort)m_Position), channelId, out b))
			{
				if (!m_IsReliable || b != 4)
				{
					if (LogFilter.logError)
					{
						Debug.LogError(string.Concat(new object[]
						{
							"Failed to send internal buffer channel:",
							channelId,
							" bytesToSend:",
							m_Position
						}));
					}
					result = false;
				}
			}
			if (b != 0)
			{
				if (m_IsReliable && b == 4)
				{
					return false;
				}
				if (LogFilter.logError)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Send Error: ",
						(NetworkError)b,
						" channel:",
						channelId,
						" bytesToSend:",
						m_Position
					}));
				}
				result = false;
			}
			m_Position = 0;
			return result;
		}

		private int m_Position;

		private byte[] m_Buffer;

		private bool m_IsReliable;
	}
}
