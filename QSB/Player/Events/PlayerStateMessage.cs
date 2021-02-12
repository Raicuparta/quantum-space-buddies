using QSB.Messaging;
using QuantumUNET.Transport;

namespace QSB.Player.Events
{
	public class PlayerStateMessage : PlayerMessage
	{
		public string PlayerName { get; set; }
		public bool PlayerReady { get; set; }
		public State PlayerState { get; set; }

		public override void Deserialize(QSBNetworkReader reader)
		{
			base.Deserialize(reader);
			PlayerName = reader.ReadString();
			PlayerReady = reader.ReadBoolean();
			PlayerState = (State)reader.ReadInt32();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(PlayerName);
			writer.Write(PlayerReady);
			writer.Write((int)PlayerState);
		}
	}
}