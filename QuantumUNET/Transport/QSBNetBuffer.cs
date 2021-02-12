using System;
using UnityEngine;

namespace QuantumUNET.Transport
{
	internal class QSBNetBuffer
	{
		public QSBNetBuffer()
		{
			m_Buffer = new byte[64];
		}

		public QSBNetBuffer(byte[] buffer)
		{
			m_Buffer = buffer;
		}

		public uint Position { get; private set; }
		public int Length => m_Buffer.Length;

		public byte ReadByte()
		{
			if (Position >= (ulong)m_Buffer.Length)
			{
				throw new IndexOutOfRangeException($"NetworkReader:ReadByte out of range:{ToString()}");
			}
			return m_Buffer[(int)(UIntPtr)Position++];
		}

		public void ReadBytes(byte[] buffer, uint count)
		{
			if (Position + count > (ulong)m_Buffer.Length)
			{
				throw new IndexOutOfRangeException($"NetworkReader:ReadBytes out of range: ({count}) {ToString()}");
			}
			ushort num = 0;
			while (num < count)
			{
				buffer[num] = m_Buffer[(int)(UIntPtr)(Position + num)];
				num += 1;
			}
			Position += count;
		}

		internal ArraySegment<byte> AsArraySegment() => new ArraySegment<byte>(m_Buffer, 0, (int)Position);

		public void WriteByte(byte value)
		{
			WriteCheckForSpace(1);
			m_Buffer[(int)(UIntPtr)Position] = value;
			Position += 1U;
		}

		public void WriteByte2(byte value0, byte value1)
		{
			WriteCheckForSpace(2);
			m_Buffer[(int)(UIntPtr)Position] = value0;
			m_Buffer[(int)(UIntPtr)(Position + 1U)] = value1;
			Position += 2U;
		}

		public void WriteByte4(byte value0, byte value1, byte value2, byte value3)
		{
			WriteCheckForSpace(4);
			m_Buffer[(int)(UIntPtr)Position] = value0;
			m_Buffer[(int)(UIntPtr)(Position + 1U)] = value1;
			m_Buffer[(int)(UIntPtr)(Position + 2U)] = value2;
			m_Buffer[(int)(UIntPtr)(Position + 3U)] = value3;
			Position += 4U;
		}

		public void WriteByte8(byte value0, byte value1, byte value2, byte value3, byte value4, byte value5, byte value6, byte value7)
		{
			WriteCheckForSpace(8);
			m_Buffer[(int)(UIntPtr)Position] = value0;
			m_Buffer[(int)(UIntPtr)(Position + 1U)] = value1;
			m_Buffer[(int)(UIntPtr)(Position + 2U)] = value2;
			m_Buffer[(int)(UIntPtr)(Position + 3U)] = value3;
			m_Buffer[(int)(UIntPtr)(Position + 4U)] = value4;
			m_Buffer[(int)(UIntPtr)(Position + 5U)] = value5;
			m_Buffer[(int)(UIntPtr)(Position + 6U)] = value6;
			m_Buffer[(int)(UIntPtr)(Position + 7U)] = value7;
			Position += 8U;
		}

		public void WriteBytesAtOffset(byte[] buffer, ushort targetOffset, ushort count)
		{
			var num = (uint)(count + targetOffset);
			WriteCheckForSpace((ushort)num);
			if (targetOffset == 0 && count == buffer.Length)
			{
				buffer.CopyTo(m_Buffer, (int)Position);
			}
			else
			{
				for (var i = 0; i < count; i++)
				{
					m_Buffer[targetOffset + i] = buffer[i];
				}
			}
			if (num > Position)
			{
				Position = num;
			}
		}

		public void WriteBytes(byte[] buffer, ushort count)
		{
			WriteCheckForSpace(count);
			if (count == buffer.Length)
			{
				buffer.CopyTo(m_Buffer, (int)Position);
			}
			else
			{
				for (var i = 0; i < count; i++)
				{
					m_Buffer[(int)checked((IntPtr)unchecked(Position + (ulong)i))] = buffer[i];
				}
			}
			Position += count;
		}

		private void WriteCheckForSpace(ushort count)
		{
			if (Position + count >= (ulong)m_Buffer.Length)
			{
				var num = (int)Math.Ceiling(m_Buffer.Length * 1.5f);
				while (Position + count >= (ulong)num)
				{
					num = (int)Math.Ceiling(num * 1.5f);
					if (num > 134217728)
					{
						Debug.LogWarning($"NetworkBuffer size is {num} bytes!");
					}
				}
				var array = new byte[num];
				m_Buffer.CopyTo(array, 0);
				m_Buffer = array;
			}
		}

		public void FinishMessage()
		{
			var num = (ushort)(Position - 4U);
			m_Buffer[0] = (byte)(num & 255);
			m_Buffer[1] = (byte)((num >> 8) & 255);
		}

		public void SeekZero() =>
			Position = 0U;

		public void Replace(byte[] buffer)
		{
			m_Buffer = buffer;
			Position = 0U;
		}

		public override string ToString() => $"NetBuf sz:{m_Buffer.Length} pos:{Position}";

		private byte[] m_Buffer;
	}
}