using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	public class QSBStringMessage : QSBMessageBase
	{
		public string value;

		public QSBStringMessage(string v)
		{
			value = v;
		}

		public override void Serialize(QSBNetworkWriter writer) => writer.Write(value);

		public override void Deserialize(QSBNetworkReader reader) => value = reader.ReadString();
	}
}