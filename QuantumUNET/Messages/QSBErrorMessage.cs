using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	public class QSBErrorMessage : QSBMessageBase
	{
		public int errorCode;

		public override void Serialize(QSBNetworkWriter writer) => writer.Write((ushort)errorCode);

		public override void Deserialize(QSBNetworkReader reader) => errorCode = reader.ReadUInt16();
	}
}