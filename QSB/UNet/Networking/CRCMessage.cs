using System;

namespace QSB.UNet.Networking
{
	internal class CRCMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			int num = (int)reader.ReadUInt16();
			scripts = new CRCMessageEntry[num];
			for (int i = 0; i < scripts.Length; i++)
			{
				CRCMessageEntry crcmessageEntry = default(CRCMessageEntry);
				crcmessageEntry.name = reader.ReadString();
				crcmessageEntry.channel = reader.ReadByte();
				scripts[i] = crcmessageEntry;
			}
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write((ushort)scripts.Length);
			for (int i = 0; i < scripts.Length; i++)
			{
				writer.Write(scripts[i].name);
				writer.Write(scripts[i].channel);
			}
		}

		public CRCMessageEntry[] scripts;
	}
}
