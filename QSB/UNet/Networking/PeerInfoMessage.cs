using System;
using System.Collections.Generic;

namespace QSB.UNet.Networking
{
	public class PeerInfoMessage : MessageBase
	{
		public override void Deserialize(NetworkReader reader)
		{
			connectionId = (int)reader.ReadPackedUInt32();
			address = reader.ReadString();
			port = (int)reader.ReadPackedUInt32();
			isHost = reader.ReadBoolean();
			isYou = reader.ReadBoolean();
			uint num = reader.ReadPackedUInt32();
			if (num > 0U)
			{
				List<PeerInfoPlayer> list = new List<PeerInfoPlayer>();
				for (uint num2 = 0U; num2 < num; num2 += 1U)
				{
					PeerInfoPlayer item;
					item.netId = reader.ReadNetworkId();
					item.playerControllerId = (short)reader.ReadPackedUInt32();
					list.Add(item);
				}
				playerIds = list.ToArray();
			}
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32((uint)connectionId);
			writer.Write(address);
			writer.WritePackedUInt32((uint)port);
			writer.Write(isHost);
			writer.Write(isYou);
			if (playerIds == null)
			{
				writer.WritePackedUInt32(0U);
			}
			else
			{
				writer.WritePackedUInt32((uint)playerIds.Length);
				for (int i = 0; i < playerIds.Length; i++)
				{
					writer.Write(playerIds[i].netId);
					writer.WritePackedUInt32((uint)playerIds[i].playerControllerId);
				}
			}
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"PeerInfo conn:",
				connectionId,
				" addr:",
				address,
				":",
				port,
				" host:",
				isHost,
				" isYou:",
				isYou
			});
		}

		public int connectionId;

		public string address;

		public int port;

		public bool isHost;

		public bool isYou;

		public PeerInfoPlayer[] playerIds;
	}
}
