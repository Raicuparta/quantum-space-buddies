using QSB.Messaging;
using QuantumUNET.Transport;

namespace QSB.OrbSync.Events
{
	public class OrbSlotMessage : PlayerMessage
	{
		public int SlotId { get; set; }
		public int OrbId { get; set; }
		public bool SlotState { get; set; }

		public override void Deserialize(QSBNetworkReader reader)
		{
			base.Deserialize(reader);
			SlotId = reader.ReadInt32();
			OrbId = reader.ReadInt32();
			SlotState = reader.ReadBoolean();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(SlotId);
			writer.Write(OrbId);
			writer.Write(SlotState);
		}
	}
}
