using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	internal class NetBuffer
	{
		public NetBuffer()
		{
			m_Buffer = new byte[64];
		}

		public NetBuffer(byte[] buffer)
		{
			m_Buffer = buffer;
		}

		public uint Position
		{
			get
			{
				return m_Pos;
			}
		}

		public int Length
		{
			get
			{
				return m_Buffer.Length;
			}
		}

		public byte ReadByte()
		{
			if ((ulong)m_Pos >= (ulong)((long)m_Buffer.Length))
			{
				throw new IndexOutOfRangeException("NetworkReader:ReadByte out of range:" + ToString());
			}
			return m_Buffer[(int)((UIntPtr)(m_Pos++))];
		}

		public void ReadBytes(byte[] buffer, uint count)
		{
			if ((ulong)(m_Pos + count) > (ulong)((long)m_Buffer.Length))
			{
				throw new IndexOutOfRangeException(string.Concat(new object[]
				{
					"NetworkReader:ReadBytes out of range: (",
					count,
					") ",
					ToString()
				}));
			}
			ushort num = 0;
			while ((uint)num < count)
			{
				buffer[(int)num] = m_Buffer[(int)((UIntPtr)(m_Pos + (uint)num))];
				num += 1;
			}
			m_Pos += count;
		}

		internal ArraySegment<byte> AsArraySegment()
		{
			return new ArraySegment<byte>(m_Buffer, 0, (int)m_Pos);
		}

		public void WriteByte(byte value)
		{
			WriteCheckForSpace(1);
			m_Buffer[(int)((UIntPtr)m_Pos)] = value;
			m_Pos += 1U;
		}

		public void WriteByte2(byte value0, byte value1)
		{
			WriteCheckForSpace(2);
			m_Buffer[(int)((UIntPtr)m_Pos)] = value0;
			m_Buffer[(int)((UIntPtr)(m_Pos + 1U))] = value1;
			m_Pos += 2U;
		}

		public void WriteByte4(byte value0, byte value1, byte value2, byte value3)
		{
			WriteCheckForSpace(4);
			m_Buffer[(int)((UIntPtr)m_Pos)] = value0;
			m_Buffer[(int)((UIntPtr)(m_Pos + 1U))] = value1;
			m_Buffer[(int)((UIntPtr)(m_Pos + 2U))] = value2;
			m_Buffer[(int)((UIntPtr)(m_Pos + 3U))] = value3;
			m_Pos += 4U;
		}

		public void WriteByte8(byte value0, byte value1, byte value2, byte value3, byte value4, byte value5, byte value6, byte value7)
		{
			WriteCheckForSpace(8);
			m_Buffer[(int)((UIntPtr)m_Pos)] = value0;
			m_Buffer[(int)((UIntPtr)(m_Pos + 1U))] = value1;
			m_Buffer[(int)((UIntPtr)(m_Pos + 2U))] = value2;
			m_Buffer[(int)((UIntPtr)(m_Pos + 3U))] = value3;
			m_Buffer[(int)((UIntPtr)(m_Pos + 4U))] = value4;
			m_Buffer[(int)((UIntPtr)(m_Pos + 5U))] = value5;
			m_Buffer[(int)((UIntPtr)(m_Pos + 6U))] = value6;
			m_Buffer[(int)((UIntPtr)(m_Pos + 7U))] = value7;
			m_Pos += 8U;
		}

		public void WriteBytesAtOffset(byte[] buffer, ushort targetOffset, ushort count)
		{
			uint num = (uint)(count + targetOffset);
			WriteCheckForSpace((ushort)num);
			if (targetOffset == 0 && (int)count == buffer.Length)
			{
				buffer.CopyTo(m_Buffer, (int)m_Pos);
			}
			else
			{
				for (int i = 0; i < (int)count; i++)
				{
					m_Buffer[(int)targetOffset + i] = buffer[i];
				}
			}
			if (num > m_Pos)
			{
				m_Pos = num;
			}
		}

		public void WriteBytes(byte[] buffer, ushort count)
		{
			WriteCheckForSpace(count);
			if ((int)count == buffer.Length)
			{
				buffer.CopyTo(m_Buffer, (int)m_Pos);
			}
			else
			{
				for (int i = 0; i < (int)count; i++)
				{
					m_Buffer[(int)(checked((IntPtr)(unchecked((ulong)m_Pos + (ulong)((long)i)))))] = buffer[i];
				}
			}
			m_Pos += (uint)count;
		}

		private void WriteCheckForSpace(ushort count)
		{
			if ((ulong)(m_Pos + (uint)count) >= (ulong)((long)m_Buffer.Length))
			{
				int num = (int)Math.Ceiling((double)((float)m_Buffer.Length * 1.5f));
				while ((ulong)(m_Pos + (uint)count) >= (ulong)((long)num))
				{
					num = (int)Math.Ceiling((double)((float)num * 1.5f));
					if (num > 134217728)
					{
						Debug.LogWarning("NetworkBuffer size is " + num + " bytes!");
					}
				}
				byte[] array = new byte[num];
				m_Buffer.CopyTo(array, 0);
				m_Buffer = array;
			}
		}

		public void FinishMessage()
		{
			ushort num = (ushort)(m_Pos - 4U);
			m_Buffer[0] = (byte)(num & 255);
			m_Buffer[1] = (byte)(num >> 8 & 255);
		}

		public void SeekZero()
		{
			m_Pos = 0U;
		}

		public void Replace(byte[] buffer)
		{
			m_Buffer = buffer;
			m_Pos = 0U;
		}

		public override string ToString()
		{
			return string.Format("NetBuf sz:{0} pos:{1}", m_Buffer.Length, m_Pos);
		}

		private byte[] m_Buffer;

		private uint m_Pos;

		private const int k_InitialSize = 64;

		private const float k_GrowthFactor = 1.5f;

		private const int k_BufferSizeWarning = 134217728;
	}
}
