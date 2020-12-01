using System;
using System.Text;
using UnityEngine;

namespace QSB.UNet.Networking
{
    /// <summary>
    ///   <para>General purpose serializer for UNET (for reading byte arrays).</para>
    /// </summary>
    public class NetworkReader
    {
        private const int k_MaxStringLength = 32768;
        private const int k_InitialStringBufferSize = 1024;
        private NetBuffer m_buf;
        private static byte[] s_StringReaderBuffer;
        private static Encoding s_Encoding;

        /// <summary>
        ///   <para>The current position within the buffer.</para>
        /// </summary>
        public uint Position
        {
            get
            {
                return m_buf.Position;
            }
        }

        public int Length
        {
            get
            {
                return m_buf.Length;
            }
        }

        /// <summary>
        ///   <para>Creates a new NetworkReader object.</para>
        /// </summary>
        /// <param name="buffer">A buffer to construct the reader with, this buffer is NOT copied.</param>
        public NetworkReader()
        {
            m_buf = new NetBuffer();
            NetworkReader.Initialize();
        }

        public NetworkReader(NetworkWriter writer)
        {
            m_buf = new NetBuffer(writer.AsArray());
            NetworkReader.Initialize();
        }

        /// <summary>
        ///   <para>Creates a new NetworkReader object.</para>
        /// </summary>
        /// <param name="buffer">A buffer to construct the reader with, this buffer is NOT copied.</param>
        public NetworkReader(byte[] buffer)
        {
            m_buf = new NetBuffer(buffer);
            NetworkReader.Initialize();
        }

        private static void Initialize()
        {
            if (NetworkReader.s_Encoding != null)
                return;
            NetworkReader.s_StringReaderBuffer = new byte[1024];
            NetworkReader.s_Encoding = (Encoding)new UTF8Encoding();
        }

        /// <summary>
        ///   <para>Sets the current position of the reader's stream to the start of the stream.</para>
        /// </summary>
        public void SeekZero()
        {
            m_buf.SeekZero();
        }

        internal void Replace(byte[] buffer)
        {
            m_buf.Replace(buffer);
        }

        /// <summary>
        ///   <para>Reads a 32-bit variable-length-encoded value.</para>
        /// </summary>
        /// <returns>
        ///   <para>The 32 bit value read.</para>
        /// </returns>
        public uint ReadPackedUInt32()
        {
            byte num1 = ReadByte();
            if ((int)num1 < 241)
                return (uint)num1;
            byte num2 = ReadByte();
            if ((int)num1 >= 241 && (int)num1 <= 248)
                return (uint)(240 + 256 * ((int)num1 - 241)) + (uint)num2;
            byte num3 = ReadByte();
            if ((int)num1 == 249)
                return (uint)(2288 + 256 * (int)num2) + (uint)num3;
            byte num4 = ReadByte();
            if ((int)num1 == 250)
                return (uint)((int)num2 + ((int)num3 << 8) + ((int)num4 << 16));
            byte num5 = ReadByte();
            if ((int)num1 >= 251)
                return (uint)((int)num2 + ((int)num3 << 8) + ((int)num4 << 16) + ((int)num5 << 24));
            throw new IndexOutOfRangeException("ReadPackedUInt32() failure: " + (object)num1);
        }

        /// <summary>
        ///   <para>Reads a 64-bit variable-length-encoded value.</para>
        /// </summary>
        /// <returns>
        ///   <para>The 64 bit value read.</para>
        /// </returns>
        public ulong ReadPackedUInt64()
        {
            byte num1 = ReadByte();
            if ((int)num1 < 241)
                return (ulong)num1;
            byte num2 = ReadByte();
            if ((int)num1 >= 241 && (int)num1 <= 248)
                return (ulong)(240L + 256L * ((long)num1 - 241L)) + (ulong)num2;
            byte num3 = ReadByte();
            if ((int)num1 == 249)
                return (ulong)(2288L + 256L * (long)num2) + (ulong)num3;
            byte num4 = ReadByte();
            if ((int)num1 == 250)
                return (ulong)((long)num2 + ((long)num3 << 8) + ((long)num4 << 16));
            byte num5 = ReadByte();
            if ((int)num1 == 251)
                return (ulong)((long)num2 + ((long)num3 << 8) + ((long)num4 << 16) + ((long)num5 << 24));
            byte num6 = ReadByte();
            if ((int)num1 == 252)
                return (ulong)((long)num2 + ((long)num3 << 8) + ((long)num4 << 16) + ((long)num5 << 24) + ((long)num6 << 32));
            byte num7 = ReadByte();
            if ((int)num1 == 253)
                return (ulong)((long)num2 + ((long)num3 << 8) + ((long)num4 << 16) + ((long)num5 << 24) + ((long)num6 << 32) + ((long)num7 << 40));
            byte num8 = ReadByte();
            if ((int)num1 == 254)
                return (ulong)((long)num2 + ((long)num3 << 8) + ((long)num4 << 16) + ((long)num5 << 24) + ((long)num6 << 32) + ((long)num7 << 40) + ((long)num8 << 48));
            byte num9 = ReadByte();
            if ((int)num1 == (int)byte.MaxValue)
                return (ulong)((long)num2 + ((long)num3 << 8) + ((long)num4 << 16) + ((long)num5 << 24) + ((long)num6 << 32) + ((long)num7 << 40) + ((long)num8 << 48) + ((long)num9 << 56));
            throw new IndexOutOfRangeException("ReadPackedUInt64() failure: " + (object)num1);
        }

        /// <summary>
        ///   <para>Reads a NetworkInstanceId from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The NetworkInstanceId read.</para>
        /// </returns>
        public NetworkInstanceId ReadNetworkId()
        {
            return new NetworkInstanceId(ReadPackedUInt32());
        }

        /// <summary>
        ///   <para>Reads a NetworkSceneId from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The NetworkSceneId read.</para>
        /// </returns>
        public NetworkSceneId ReadSceneId()
        {
            return new NetworkSceneId(ReadPackedUInt32());
        }

        /// <summary>
        ///   <para>Reads a byte from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The value read.</para>
        /// </returns>
        public byte ReadByte()
        {
            return m_buf.ReadByte();
        }

        /// <summary>
        ///   <para>Reads a signed byte from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public sbyte ReadSByte()
        {
            return (sbyte)m_buf.ReadByte();
        }

        /// <summary>
        ///   <para>Reads a signed 16 bit integer from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public short ReadInt16()
        {
            return (short)(ushort)((uint)(ushort)(0U | (uint)m_buf.ReadByte()) | (uint)(ushort)((uint)m_buf.ReadByte() << 8));
        }

        /// <summary>
        ///   <para>Reads an unsigned 16 bit integer from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public ushort ReadUInt16()
        {
            return (ushort)((uint)(ushort)(0U | (uint)m_buf.ReadByte()) | (uint)(ushort)((uint)m_buf.ReadByte() << 8));
        }

        /// <summary>
        ///   <para>Reads a signed 32bit integer from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public int ReadInt32()
        {
            return (int)(0U | (uint)m_buf.ReadByte() | (uint)m_buf.ReadByte() << 8 | (uint)m_buf.ReadByte() << 16 | (uint)m_buf.ReadByte() << 24);
        }

        /// <summary>
        ///   <para>Reads an unsigned 32 bit integer from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public uint ReadUInt32()
        {
            return 0U | (uint)m_buf.ReadByte() | (uint)m_buf.ReadByte() << 8 | (uint)m_buf.ReadByte() << 16 | (uint)m_buf.ReadByte() << 24;
        }

        /// <summary>
        ///   <para>Reads a signed 64 bit integer from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public long ReadInt64()
        {
            return (long)(0UL | (ulong)m_buf.ReadByte() | (ulong)m_buf.ReadByte() << 8 | (ulong)m_buf.ReadByte() << 16 | (ulong)m_buf.ReadByte() << 24 | (ulong)m_buf.ReadByte() << 32 | (ulong)m_buf.ReadByte() << 40 | (ulong)m_buf.ReadByte() << 48 | (ulong)m_buf.ReadByte() << 56);
        }

        /// <summary>
        ///   <para>Reads an unsigned 64 bit integer from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public ulong ReadUInt64()
        {
            return 0UL | (ulong)m_buf.ReadByte() | (ulong)m_buf.ReadByte() << 8 | (ulong)m_buf.ReadByte() << 16 | (ulong)m_buf.ReadByte() << 24 | (ulong)m_buf.ReadByte() << 32 | (ulong)m_buf.ReadByte() << 40 | (ulong)m_buf.ReadByte() << 48 | (ulong)m_buf.ReadByte() << 56;
        }

        /// <summary>
        ///   <para>Reads a float from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public float ReadSingle()
        {
            return FloatConversion.ToSingle(ReadUInt32());
        }

        /// <summary>
        ///   <para>Reads a double from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public double ReadDouble()
        {
            return FloatConversion.ToDouble(ReadUInt64());
        }

        /// <summary>
        ///   <para>Reads a string from the stream. (max of 32k bytes).</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public string ReadString()
        {
            ushort num = ReadUInt16();
            if ((int)num == 0)
                return string.Empty;
            if ((int)num >= 32768)
                throw new IndexOutOfRangeException("ReadString() too long: " + (object)num);
            while ((int)num > NetworkReader.s_StringReaderBuffer.Length)
                NetworkReader.s_StringReaderBuffer = new byte[NetworkReader.s_StringReaderBuffer.Length * 2];
            m_buf.ReadBytes(NetworkReader.s_StringReaderBuffer, (uint)num);
            return new string(NetworkReader.s_Encoding.GetChars(NetworkReader.s_StringReaderBuffer, 0, (int)num));
        }

        /// <summary>
        ///   <para>Reads a char from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>Value read.</para>
        /// </returns>
        public char ReadChar()
        {
            return (char)m_buf.ReadByte();
        }

        /// <summary>
        ///   <para>Reads a boolean from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The value read.</para>
        /// </returns>
        public bool ReadBoolean()
        {
            return (int)m_buf.ReadByte() == 1;
        }

        /// <summary>
        ///   <para>Reads a number of bytes from the stream.</para>
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>
        ///   <para>Bytes read. (this is a copy).</para>
        /// </returns>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new IndexOutOfRangeException("NetworkReader ReadBytes " + (object)count);
            byte[] buffer = new byte[count];
            m_buf.ReadBytes(buffer, (uint)count);
            return buffer;
        }

        /// <summary>
        ///   <para>This read a 16-bit byte count and a array of bytes of that size from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The bytes read from the stream.</para>
        /// </returns>
        public byte[] ReadBytesAndSize()
        {
            ushort num = ReadUInt16();
            if ((int)num == 0)
                return (byte[])null;
            return ReadBytes((int)num);
        }

        /// <summary>
        ///   <para>Reads a Unity Vector2 object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The vector read from the stream.</para>
        /// </returns>
        public Vector2 ReadVector2()
        {
            return new Vector2(ReadSingle(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a Unity Vector3 objects.</para>
        /// </summary>
        /// <returns>
        ///   <para>The vector read from the stream.</para>
        /// </returns>
        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a Unity Vector4 object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The vector read from the stream.</para>
        /// </returns>
        public Vector4 ReadVector4()
        {
            return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a unity Color objects.</para>
        /// </summary>
        /// <returns>
        ///   <para>The color read from the stream.</para>
        /// </returns>
        public Color ReadColor()
        {
            return new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a unity color32 objects.</para>
        /// </summary>
        /// <returns>
        ///   <para>The colo read from the stream.</para>
        /// </returns>
        public Color32 ReadColor32()
        {
            return new Color32(ReadByte(), ReadByte(), ReadByte(), ReadByte());
        }

        /// <summary>
        ///   <para>Reads a Unity Quaternion object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The quaternion read from the stream.</para>
        /// </returns>
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a Unity Rect object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The rect read from the stream.</para>
        /// </returns>
        public Rect ReadRect()
        {
            return new Rect(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a unity Plane object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The plane read from the stream.</para>
        /// </returns>
        public Plane ReadPlane()
        {
            return new Plane(ReadVector3(), ReadSingle());
        }

        /// <summary>
        ///   <para>Reads a Unity Ray object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The ray read from the stream.</para>
        /// </returns>
        public Ray ReadRay()
        {
            return new Ray(ReadVector3(), ReadVector3());
        }

        /// <summary>
        ///   <para>Reads a unity Matrix4x4 object.</para>
        /// </summary>
        /// <returns>
        ///   <para>The matrix read from the stream.</para>
        /// </returns>
        public Matrix4x4 ReadMatrix4x4()
        {
            return new Matrix4x4() { m00 = ReadSingle(), m01 = ReadSingle(), m02 = ReadSingle(), m03 = ReadSingle(), m10 = ReadSingle(), m11 = ReadSingle(), m12 = ReadSingle(), m13 = ReadSingle(), m20 = ReadSingle(), m21 = ReadSingle(), m22 = ReadSingle(), m23 = ReadSingle(), m30 = ReadSingle(), m31 = ReadSingle(), m32 = ReadSingle(), m33 = ReadSingle() };
        }

        /// <summary>
        ///   <para>Reads a NetworkHash128 assetId.</para>
        /// </summary>
        /// <returns>
        ///   <para>The assetId object read from the stream.</para>
        /// </returns>
        public NetworkHash128 ReadNetworkHash128()
        {
            NetworkHash128 networkHash128;
            networkHash128.i0 = ReadByte();
            networkHash128.i1 = ReadByte();
            networkHash128.i2 = ReadByte();
            networkHash128.i3 = ReadByte();
            networkHash128.i4 = ReadByte();
            networkHash128.i5 = ReadByte();
            networkHash128.i6 = ReadByte();
            networkHash128.i7 = ReadByte();
            networkHash128.i8 = ReadByte();
            networkHash128.i9 = ReadByte();
            networkHash128.i10 = ReadByte();
            networkHash128.i11 = ReadByte();
            networkHash128.i12 = ReadByte();
            networkHash128.i13 = ReadByte();
            networkHash128.i14 = ReadByte();
            networkHash128.i15 = ReadByte();
            return networkHash128;
        }

        /// <summary>
        ///   <para>Reads a reference to a Transform from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The transform object read.</para>
        /// </returns>
        public Transform ReadTransform()
        {
            NetworkInstanceId netId = ReadNetworkId();
            if (netId.IsEmpty())
                return (Transform)null;
            GameObject localObject = ClientScene.FindLocalObject(netId);
            if (!((UnityEngine.Object)localObject == (UnityEngine.Object)null))
                return localObject.transform;
            if (LogFilter.logDebug)
                Debug.Log((object)("ReadTransform netId:" + (object)netId));
            return (Transform)null;
        }

        /// <summary>
        ///   <para>Reads a reference to a GameObject from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The GameObject referenced.</para>
        /// </returns>
        public GameObject ReadGameObject()
        {
            NetworkInstanceId netId = ReadNetworkId();
            if (netId.IsEmpty())
                return (GameObject)null;
            GameObject gameObject = !NetworkServer.active ? ClientScene.FindLocalObject(netId) : NetworkServer.FindLocalObject(netId);
            if ((UnityEngine.Object)gameObject == (UnityEngine.Object)null && LogFilter.logDebug)
                Debug.Log((object)("ReadGameObject netId:" + (object)netId + "go: null"));
            return gameObject;
        }

        /// <summary>
        ///   <para>Reads a reference to a NetworkIdentity from the stream.</para>
        /// </summary>
        /// <returns>
        ///   <para>The NetworkIdentity object read.</para>
        /// </returns>
        public NetworkIdentity ReadNetworkIdentity()
        {
            NetworkInstanceId netId = ReadNetworkId();
            if (netId.IsEmpty())
                return (NetworkIdentity)null;
            GameObject gameObject = !NetworkServer.active ? ClientScene.FindLocalObject(netId) : NetworkServer.FindLocalObject(netId);
            if (!((UnityEngine.Object)gameObject == (UnityEngine.Object)null))
                return gameObject.GetComponent<NetworkIdentity>();
            if (LogFilter.logDebug)
                Debug.Log((object)("ReadNetworkIdentity netId:" + (object)netId + "go: null"));
            return (NetworkIdentity)null;
        }

        /// <summary>
        ///   <para>Returns a string representation of the reader's buffer.</para>
        /// </summary>
        /// <returns>
        ///   <para>Buffer contents.</para>
        /// </returns>
        public override string ToString()
        {
            return m_buf.ToString();
        }

        public TMsg ReadMessage<TMsg>() where TMsg : MessageBase, new()
        {
            TMsg instance = Activator.CreateInstance<TMsg>();
            instance.Deserialize(this);
            return instance;
        }
    }
}