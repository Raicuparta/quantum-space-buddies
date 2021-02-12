using QuantumUNET.Transport;

namespace QuantumUNET.Messages
{
	internal class QSBAddPlayerMessage : QSBMessageBase
	{
		public short playerControllerId;
		public int msgSize;
		public byte[] msgData;

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.Write(playerControllerId);
			writer.WriteBytesAndSize(msgData, msgSize);
		}

		public override void Deserialize(QSBNetworkReader reader)
		{
			playerControllerId = reader.ReadInt16();
			msgData = reader.ReadBytesAndSize();
			msgSize = msgData?.Length ?? 0;
		}
	}
}