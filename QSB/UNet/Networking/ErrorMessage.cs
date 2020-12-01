using System;

namespace QSB.UNet.Networking
{
	public class ErrorMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			errorCode = (int)reader.ReadUInt16();
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write((ushort)errorCode);
		}

		public int errorCode;
	}
}
