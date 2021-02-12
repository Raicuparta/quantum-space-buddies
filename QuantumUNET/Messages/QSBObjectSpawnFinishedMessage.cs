using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	internal class QSBObjectSpawnFinishedMessage : QSBMessageBase
	{
		public uint State;

		public override void Serialize(QSBNetworkWriter writer) => writer.WritePackedUInt32(State);

		public override void Deserialize(QSBNetworkReader reader) => State = reader.ReadPackedUInt32();
	}
}