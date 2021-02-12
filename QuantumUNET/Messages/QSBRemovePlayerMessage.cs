using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	public class QSBRemovePlayerMessage : QSBMessageBase
	{
		public short PlayerControllerId;

		public override void Serialize(QSBNetworkWriter writer) => writer.Write((ushort)PlayerControllerId);

		public override void Deserialize(QSBNetworkReader reader) => PlayerControllerId = (short)reader.ReadUInt16();
	}
}