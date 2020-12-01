using System;

namespace QSB.UNet.Networking
{
	public class RemovePlayerMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			playerControllerId = (short)reader.ReadUInt16();
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write((ushort)playerControllerId);
		}

		public short playerControllerId;
	}
}
