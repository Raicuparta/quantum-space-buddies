using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	public class QSBEmptyMessage : QSBMessageBase
	{
		public override void Serialize(QSBNetworkWriter writer) { }
		public override void Deserialize(QSBNetworkReader reader) { }
	}
}