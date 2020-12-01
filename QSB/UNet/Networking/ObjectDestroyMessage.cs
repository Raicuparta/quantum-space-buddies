using System;

namespace QSB.UNet.Networking
{
	internal class ObjectDestroyMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			netId = reader.ReadNetworkId();
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(netId);
		}

		public NetworkInstanceId netId;
	}
}
