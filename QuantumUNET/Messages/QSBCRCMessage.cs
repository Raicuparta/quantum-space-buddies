using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	internal class QSBCRCMessage : QSBMessageBase
	{
		public QSBCRCMessageEntry[] scripts;

		public override void Deserialize(QSBNetworkReader reader)
		{
			var num = (int)reader.ReadUInt16();
			scripts = new QSBCRCMessageEntry[num];
			for (var i = 0; i < scripts.Length; i++)
			{
				var crcmessageEntry = default(QSBCRCMessageEntry);
				crcmessageEntry.name = reader.ReadString();
				crcmessageEntry.channel = reader.ReadByte();
				scripts[i] = crcmessageEntry;
			}
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.Write((ushort)scripts.Length);
			for (var i = 0; i < scripts.Length; i++)
			{
				writer.Write(scripts[i].name);
				writer.Write(scripts[i].channel);
			}
		}
	}
}