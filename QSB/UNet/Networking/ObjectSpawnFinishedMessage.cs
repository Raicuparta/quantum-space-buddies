using System;

namespace QSB.UNet.Networking
{
	internal class ObjectSpawnFinishedMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			state = reader.ReadPackedUInt32();
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32(state);
		}

		public uint state;
	}
}
