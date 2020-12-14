﻿using QSB.Messages;
using QuantumUNET;

namespace QSB.ConversationSync.Events
{
	public class ConversationMessage : PlayerMessage
	{
		public ConversationType Type { get; set; }
		public int ObjectId { get; set; }
		public string Message { get; set; }

		public override void Deserialize(QSBNetworkReader reader)
		{
			base.Deserialize(reader);
			ObjectId = reader.ReadInt32();
			Type = (ConversationType)reader.ReadInt32();
			Message = reader.ReadString();
		}

		public override void Serialize(QSBNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(ObjectId);
			writer.Write((int)Type);
			writer.Write(Message);
		}
	}
}