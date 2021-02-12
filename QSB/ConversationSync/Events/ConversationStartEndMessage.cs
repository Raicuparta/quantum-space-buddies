using QSB.Messaging;
using QuantumUNET.Transport;

namespace QSB.ConversationSync.Events
{
	public class ConversationStartEndMessage : PlayerMessage
	{
		public int CharacterId { get; set; }
		public uint PlayerId { get; set; }
		public bool State { get; set; }

		public override void Deserialize(QSBNetworkReader reader)
		{
			base.Deserialize(reader);
			CharacterId = reader.ReadInt32();
			PlayerId = reader.ReadUInt32();
			State = reader.ReadBoolean();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(CharacterId);
			writer.Write(PlayerId);
			writer.Write(State);
		}
	}
}