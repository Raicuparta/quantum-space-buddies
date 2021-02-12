using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	public abstract class QSBMessageBase
	{
		public virtual void Serialize(QSBNetworkWriter writer) { }

		public virtual void Deserialize(QSBNetworkReader reader) { }
	}
}