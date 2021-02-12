using QSB.Messaging;
using QuantumUNET.Transport;

namespace QSB.ConversationSync.Events
{
	public class DialogueConditionMessage : PlayerMessage
	{
		public string ConditionName { get; set; }
		public bool ConditionState { get; set; }

		public override void Deserialize(QSBNetworkReader reader)
		{
			base.Deserialize(reader);
			ConditionName = reader.ReadString();
			ConditionState = reader.ReadBoolean();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(ConditionName);
			writer.Write(ConditionState);
		}
	}
}
