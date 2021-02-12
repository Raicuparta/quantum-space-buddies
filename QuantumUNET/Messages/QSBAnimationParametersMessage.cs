using QuantumUNET.Transport;
using UnityEngine.Networking;

namespace QuantumUNET.Messages
{
	internal class QSBAnimationParametersMessage : QSBMessageBase
	{
		public NetworkInstanceId netId;
		public byte[] parameters;

		public override void Deserialize(QSBNetworkReader reader)
		{
			netId = reader.ReadNetworkId();
			parameters = reader.ReadBytesAndSize();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.Write(netId);
			writer.WriteBytesAndSize(parameters, parameters?.Length ?? 0);
		}
	}
}