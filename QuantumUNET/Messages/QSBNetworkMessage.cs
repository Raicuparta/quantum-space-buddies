using QuantumUNET.Transport;
using System;

namespace QuantumUNET.Messages
{
	public class QSBNetworkMessage
	{
		public short MsgType;
		public QSBNetworkConnection Connection;
		public QSBNetworkReader Reader;
		public int ChannelId;

		public TMsg ReadMessage<TMsg>() where TMsg : QSBMessageBase, new()
		{
			var result = Activator.CreateInstance<TMsg>();
			result.Deserialize(Reader);
			return result;
		}

		public void ReadMessage<TMsg>(TMsg msg) where TMsg : QSBMessageBase =>
			msg.Deserialize(Reader);
	}
}