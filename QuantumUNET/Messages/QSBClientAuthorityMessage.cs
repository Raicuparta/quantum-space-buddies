using QuantumUNET.Transport;
using UnityEngine.Networking;

namespace QuantumUNET.Messages
{
	internal class QSBClientAuthorityMessage : QSBMessageBase
	{
		public NetworkInstanceId netId;
		public bool authority;

		public override void Deserialize(QSBNetworkReader reader)
		{
			netId = reader.ReadNetworkId();
			authority = reader.ReadBoolean();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.Write(netId);
			writer.Write(authority);
		}
	}
}