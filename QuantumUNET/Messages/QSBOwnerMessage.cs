using QuantumUNET.Transport;
using UnityEngine.Networking;

namespace QuantumUNET.Messages
{
	internal class QSBOwnerMessage : QSBMessageBase
	{
		public NetworkInstanceId NetId;
		public short PlayerControllerId;

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.Write(NetId);
			writer.WritePackedUInt32((uint)PlayerControllerId);
		}

		public override void Deserialize(QSBNetworkReader reader)
		{
			NetId = reader.ReadNetworkId();
			PlayerControllerId = (short)reader.ReadPackedUInt32();
		}
	}
}