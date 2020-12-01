using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	internal class ObjectSpawnMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			netId = reader.ReadNetworkId();
			assetId = reader.ReadNetworkHash128();
			position = reader.ReadVector3();
			payload = reader.ReadBytesAndSize();
			uint num = 16U;
			if ((long)reader.Length - (long)((ulong)reader.Position) >= (long)((ulong)num))
			{
				rotation = reader.ReadQuaternion();
			}
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(netId);
			writer.Write(assetId);
			writer.Write(position);
			writer.WriteBytesFull(payload);
			writer.Write(rotation);
		}

		public NetworkInstanceId netId;

		public NetworkHash128 assetId;

		public Vector3 position;

		public byte[] payload;

		public Quaternion rotation;
	}
}
