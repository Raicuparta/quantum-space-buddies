using System;
using System.Text;
using UnityEngine;

namespace QSB.UNet.Networking
{
	public class NetworkWriter
	{
		public NetworkWriter()
		{
			m_Buffer = new NetBuffer();
			if (NetworkWriter.s_Encoding == null)
			{
				NetworkWriter.s_Encoding = new UTF8Encoding();
				NetworkWriter.s_StringWriteBuffer = new byte[32768];
			}
		}

		public NetworkWriter(byte[] buffer)
		{
			m_Buffer = new NetBuffer(buffer);
			if (NetworkWriter.s_Encoding == null)
			{
				NetworkWriter.s_Encoding = new UTF8Encoding();
				NetworkWriter.s_StringWriteBuffer = new byte[32768];
			}
		}

		public short Position
		{
			get
			{
				return (short)m_Buffer.Position;
			}
		}

		public byte[] ToArray()
		{
			byte[] array = new byte[m_Buffer.AsArraySegment().Count];
			Array.Copy(m_Buffer.AsArraySegment().Array, array, m_Buffer.AsArraySegment().Count);
			return array;
		}

		public byte[] AsArray()
		{
			return AsArraySegment().Array;
		}

		internal ArraySegment<byte> AsArraySegment()
		{
			return m_Buffer.AsArraySegment();
		}

		public void WritePackedUInt32(uint value)
		{
			if (value <= 240U)
			{
				Write((byte)value);
			}
			else if (value <= 2287U)
			{
				Write((byte)((value - 240U) / 256U + 241U));
				Write((byte)((value - 240U) % 256U));
			}
			else if (value <= 67823U)
			{
				Write(249);
				Write((byte)((value - 2288U) / 256U));
				Write((byte)((value - 2288U) % 256U));
			}
			else if (value <= 16777215U)
			{
				Write(250);
				Write((byte)(value & 255U));
				Write((byte)(value >> 8 & 255U));
				Write((byte)(value >> 16 & 255U));
			}
			else
			{
				Write(251);
				Write((byte)(value & 255U));
				Write((byte)(value >> 8 & 255U));
				Write((byte)(value >> 16 & 255U));
				Write((byte)(value >> 24 & 255U));
			}
		}

		public void WritePackedUInt64(ulong value)
		{
			if (value <= 240UL)
			{
				Write((byte)value);
			}
			else if (value <= 2287UL)
			{
				Write((byte)((value - 240UL) / 256UL + 241UL));
				Write((byte)((value - 240UL) % 256UL));
			}
			else if (value <= 67823UL)
			{
				Write(249);
				Write((byte)((value - 2288UL) / 256UL));
				Write((byte)((value - 2288UL) % 256UL));
			}
			else if (value <= 16777215UL)
			{
				Write(250);
				Write((byte)(value & 255UL));
				Write((byte)(value >> 8 & 255UL));
				Write((byte)(value >> 16 & 255UL));
			}
			else if (value <= 1099511627775UL)
			{
				Write(252);
				Write((byte)(value & 255UL));
				Write((byte)(value >> 8 & 255UL));
				Write((byte)(value >> 16 & 255UL));
				Write((byte)(value >> 24 & 255UL));
				Write((byte)(value >> 32 & 255UL));
			}
			else if (value <= 281474976710655UL)
			{
				Write(253);
				Write((byte)(value & 255UL));
				Write((byte)(value >> 8 & 255UL));
				Write((byte)(value >> 16 & 255UL));
				Write((byte)(value >> 24 & 255UL));
				Write((byte)(value >> 32 & 255UL));
				Write((byte)(value >> 40 & 255UL));
			}
			else if (value <= 72057594037927935UL)
			{
				Write(254);
				Write((byte)(value & 255UL));
				Write((byte)(value >> 8 & 255UL));
				Write((byte)(value >> 16 & 255UL));
				Write((byte)(value >> 24 & 255UL));
				Write((byte)(value >> 32 & 255UL));
				Write((byte)(value >> 40 & 255UL));
				Write((byte)(value >> 48 & 255UL));
			}
			else
			{
				Write(byte.MaxValue);
				Write((byte)(value & 255UL));
				Write((byte)(value >> 8 & 255UL));
				Write((byte)(value >> 16 & 255UL));
				Write((byte)(value >> 24 & 255UL));
				Write((byte)(value >> 32 & 255UL));
				Write((byte)(value >> 40 & 255UL));
				Write((byte)(value >> 48 & 255UL));
				Write((byte)(value >> 56 & 255UL));
			}
		}

		public void Write(NetworkInstanceId value)
		{
			WritePackedUInt32(value.Value);
		}

		public void Write(NetworkSceneId value)
		{
			WritePackedUInt32(value.Value);
		}

		public void Write(char value)
		{
			m_Buffer.WriteByte((byte)value);
		}

		public void Write(byte value)
		{
			m_Buffer.WriteByte(value);
		}

		public void Write(sbyte value)
		{
			m_Buffer.WriteByte((byte)value);
		}

		public void Write(short value)
		{
			m_Buffer.WriteByte2((byte)(value & 255), (byte)(value >> 8 & 255));
		}

		public void Write(ushort value)
		{
			m_Buffer.WriteByte2((byte)(value & 255), (byte)(value >> 8 & 255));
		}

		public void Write(int value)
		{
			m_Buffer.WriteByte4((byte)(value & 255), (byte)(value >> 8 & 255), (byte)(value >> 16 & 255), (byte)(value >> 24 & 255));
		}

		public void Write(uint value)
		{
			m_Buffer.WriteByte4((byte)(value & 255U), (byte)(value >> 8 & 255U), (byte)(value >> 16 & 255U), (byte)(value >> 24 & 255U));
		}

		public void Write(long value)
		{
			m_Buffer.WriteByte8((byte)(value & 255L), (byte)(value >> 8 & 255L), (byte)(value >> 16 & 255L), (byte)(value >> 24 & 255L), (byte)(value >> 32 & 255L), (byte)(value >> 40 & 255L), (byte)(value >> 48 & 255L), (byte)(value >> 56 & 255L));
		}

		public void Write(ulong value)
		{
			m_Buffer.WriteByte8((byte)(value & 255UL), (byte)(value >> 8 & 255UL), (byte)(value >> 16 & 255UL), (byte)(value >> 24 & 255UL), (byte)(value >> 32 & 255UL), (byte)(value >> 40 & 255UL), (byte)(value >> 48 & 255UL), (byte)(value >> 56 & 255UL));
		}

		public void Write(float value)
		{
			NetworkWriter.s_FloatConverter.floatValue = value;
			Write(NetworkWriter.s_FloatConverter.intValue);
		}

		public void Write(double value)
		{
			NetworkWriter.s_FloatConverter.doubleValue = value;
			Write(NetworkWriter.s_FloatConverter.longValue);
		}

		public void Write(decimal value)
		{
			int[] bits = decimal.GetBits(value);
			Write(bits[0]);
			Write(bits[1]);
			Write(bits[2]);
			Write(bits[3]);
		}

		public void Write(string value)
		{
			if (value == null)
			{
				m_Buffer.WriteByte2(0, 0);
			}
			else
			{
				int byteCount = NetworkWriter.s_Encoding.GetByteCount(value);
				if (byteCount >= 32768)
				{
					throw new IndexOutOfRangeException("Serialize(string) too long: " + value.Length);
				}
				Write((ushort)byteCount);
				int bytes = NetworkWriter.s_Encoding.GetBytes(value, 0, value.Length, NetworkWriter.s_StringWriteBuffer, 0);
				m_Buffer.WriteBytes(NetworkWriter.s_StringWriteBuffer, (ushort)bytes);
			}
		}

		public void Write(bool value)
		{
			if (value)
			{
				m_Buffer.WriteByte(1);
			}
			else
			{
				m_Buffer.WriteByte(0);
			}
		}

		public void Write(byte[] buffer, int count)
		{
			if (count > 65535)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkWriter Write: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes.");
				}
			}
			else
			{
				m_Buffer.WriteBytes(buffer, (ushort)count);
			}
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			if (count > 65535)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkWriter Write: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes.");
				}
			}
			else
			{
				m_Buffer.WriteBytesAtOffset(buffer, (ushort)offset, (ushort)count);
			}
		}

		public void WriteBytesAndSize(byte[] buffer, int count)
		{
			if (buffer == null || count == 0)
			{
				Write(0);
			}
			else if (count > 65535)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkWriter WriteBytesAndSize: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes.");
				}
			}
			else
			{
				Write((ushort)count);
				m_Buffer.WriteBytes(buffer, (ushort)count);
			}
		}

		public void WriteBytesFull(byte[] buffer)
		{
			if (buffer == null)
			{
				Write(0);
			}
			else if (buffer.Length > 65535)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("NetworkWriter WriteBytes: buffer is too large (" + buffer.Length + ") bytes. The maximum buffer size is 64K bytes.");
				}
			}
			else
			{
				Write((ushort)buffer.Length);
				m_Buffer.WriteBytes(buffer, (ushort)buffer.Length);
			}
		}

		public void Write(Vector2 value)
		{
			Write(value.x);
			Write(value.y);
		}

		public void Write(Vector3 value)
		{
			Write(value.x);
			Write(value.y);
			Write(value.z);
		}

		public void Write(Vector4 value)
		{
			Write(value.x);
			Write(value.y);
			Write(value.z);
			Write(value.w);
		}

		public void Write(Color value)
		{
			Write(value.r);
			Write(value.g);
			Write(value.b);
			Write(value.a);
		}

		public void Write(Color32 value)
		{
			Write(value.r);
			Write(value.g);
			Write(value.b);
			Write(value.a);
		}

		public void Write(Quaternion value)
		{
			Write(value.x);
			Write(value.y);
			Write(value.z);
			Write(value.w);
		}

		public void Write(Rect value)
		{
			Write(value.xMin);
			Write(value.yMin);
			Write(value.width);
			Write(value.height);
		}

		public void Write(Plane value)
		{
			Write(value.normal);
			Write(value.distance);
		}

		public void Write(Ray value)
		{
			Write(value.direction);
			Write(value.origin);
		}

		public void Write(Matrix4x4 value)
		{
			Write(value.m00);
			Write(value.m01);
			Write(value.m02);
			Write(value.m03);
			Write(value.m10);
			Write(value.m11);
			Write(value.m12);
			Write(value.m13);
			Write(value.m20);
			Write(value.m21);
			Write(value.m22);
			Write(value.m23);
			Write(value.m30);
			Write(value.m31);
			Write(value.m32);
			Write(value.m33);
		}

		public void Write(NetworkHash128 value)
		{
			Write(value.i0);
			Write(value.i1);
			Write(value.i2);
			Write(value.i3);
			Write(value.i4);
			Write(value.i5);
			Write(value.i6);
			Write(value.i7);
			Write(value.i8);
			Write(value.i9);
			Write(value.i10);
			Write(value.i11);
			Write(value.i12);
			Write(value.i13);
			Write(value.i14);
			Write(value.i15);
		}

		public void Write(NetworkIdentity value)
		{
			if (value == null)
			{
				WritePackedUInt32(0U);
			}
			else
			{
				Write(value.netId);
			}
		}

		public void Write(Transform value)
		{
			if (value == null || value.gameObject == null)
			{
				WritePackedUInt32(0U);
			}
			else
			{
				NetworkIdentity component = value.gameObject.GetComponent<NetworkIdentity>();
				if (component != null)
				{
					Write(component.netId);
				}
				else
				{
					if (LogFilter.logWarn)
					{
						Debug.LogWarning("NetworkWriter " + value + " has no NetworkIdentity");
					}
					WritePackedUInt32(0U);
				}
			}
		}

		public void Write(GameObject value)
		{
			if (value == null)
			{
				WritePackedUInt32(0U);
			}
			else
			{
				NetworkIdentity component = value.GetComponent<NetworkIdentity>();
				if (component != null)
				{
					Write(component.netId);
				}
				else
				{
					if (LogFilter.logWarn)
					{
						Debug.LogWarning("NetworkWriter " + value + " has no NetworkIdentity");
					}
					WritePackedUInt32(0U);
				}
			}
		}

		public void Write(MessageBase msg)
		{
			msg.Serialize(this);
		}

		public void SeekZero()
		{
			m_Buffer.SeekZero();
		}

		public void StartMessage(short msgType)
		{
			SeekZero();
			m_Buffer.WriteByte2(0, 0);
			Write(msgType);
		}

		public void FinishMessage()
		{
			m_Buffer.FinishMessage();
		}

		private const int k_MaxStringLength = 32768;

		private NetBuffer m_Buffer;

		private static Encoding s_Encoding;

		private static byte[] s_StringWriteBuffer;

		private static UIntFloat s_FloatConverter;
	}
}
