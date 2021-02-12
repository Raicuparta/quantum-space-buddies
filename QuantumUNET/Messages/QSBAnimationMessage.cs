using QuantumUNET.Transport;
using UnityEngine.Networking;

namespace QuantumUNET.Messages
{
	internal class QSBAnimationMessage : QSBMessageBase
	{
		public NetworkInstanceId netId;
		public int stateHash;
		public float normalizedTime;
		public byte[] parameters;

		public override void Serialize(QSBNetworkWriter writer)
		{
			writer.Write(netId);
			writer.WritePackedUInt32((uint)stateHash);
			writer.Write(normalizedTime);
			writer.WriteBytesAndSize(parameters, parameters?.Length ?? 0);
		}

		public override void Deserialize(QSBNetworkReader reader)
		{
			netId = reader.ReadNetworkId();
			stateHash = (int)reader.ReadPackedUInt32();
			normalizedTime = reader.ReadSingle();
			parameters = reader.ReadBytesAndSize();
		}
	}
}