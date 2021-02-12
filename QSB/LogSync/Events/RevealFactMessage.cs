using QSB.Messaging;
using QuantumUNET.Transport;

namespace QSB.LogSync.Events
{
	public class RevealFactMessage : PlayerMessage
	{
		public string FactId { get; set; }
		public bool SaveGame { get; set; }
		public bool ShowNotification { get; set; }

		public override void Deserialize(QSBNetworkReader reader)
		{
			base.Deserialize(reader);
			FactId = reader.ReadString();
			SaveGame = reader.ReadBoolean();
			ShowNotification = reader.ReadBoolean();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(FactId);
			writer.Write(SaveGame);
			writer.Write(ShowNotification);
		}
	}
}
