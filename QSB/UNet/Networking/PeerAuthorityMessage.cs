using System;

namespace QSB.UNet.Networking
{
	public class PeerAuthorityMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			connectionId = (int)reader.ReadPackedUInt32();
			netId = reader.ReadNetworkId();
			authorityState = reader.ReadBoolean();
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32((uint)connectionId);
			writer.Write(netId);
			writer.Write(authorityState);
		}

		public int connectionId;

		public NetworkInstanceId netId;

		public bool authorityState;
	}
}
