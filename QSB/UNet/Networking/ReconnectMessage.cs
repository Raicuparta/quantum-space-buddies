using System;

namespace QSB.UNet.Networking
{
	public class ReconnectMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			oldConnectionId = (int)reader.ReadPackedUInt32();
			playerControllerId = (short)reader.ReadPackedUInt32();
			netId = reader.ReadNetworkId();
			msgData = reader.ReadBytesAndSize();
			msgSize = msgData.Length;
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32((uint)oldConnectionId);
			writer.WritePackedUInt32((uint)playerControllerId);
			writer.Write(netId);
			writer.WriteBytesAndSize(msgData, msgSize);
		}

		public int oldConnectionId;

		public short playerControllerId;

		public NetworkInstanceId netId;

		public int msgSize;

		public byte[] msgData;
	}
}
