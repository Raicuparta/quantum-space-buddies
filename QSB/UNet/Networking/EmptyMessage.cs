using System;

namespace QSB.UNet.Networking
{
	public class EmptyMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
		}

		public override void Serialize(NetworkWriter writer)
		{
		}
	}
}
