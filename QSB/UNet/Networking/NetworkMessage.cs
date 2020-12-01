using System;

namespace QSB.UNet.Networking
{
	public class NetworkMessage
	{
		public static string Dump(byte[] payload, int sz)
		{
			string text = "[";
			for (int i = 0; i < sz; i++)
			{
				text = text + payload[i] + " ";
			}
			return text + "]";
		}

		public TMsg ReadMessage<TMsg>() where TMsg : MessageBase, new()
		{
			TMsg result = Activator.CreateInstance<TMsg>();
			result.Deserialize(reader);
			return result;
		}

		public void ReadMessage<TMsg>(TMsg msg) where TMsg : MessageBase
		{
			msg.Deserialize(reader);
		}

		public const int MaxMessageSize = 65535;

		public short msgType;

		public NetworkConnection conn;

		public NetworkReader reader;

		public int channelId;
	}
}
