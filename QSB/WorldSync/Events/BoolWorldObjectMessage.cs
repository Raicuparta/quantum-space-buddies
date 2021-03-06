﻿using QuantumUNET.Transport;

namespace QSB.WorldSync.Events
{
	public class BoolWorldObjectMessage : WorldObjectMessage
	{
		public bool State { get; set; }

		public override void Deserialize(QNetworkReader reader)
		{
			base.Deserialize(reader);
			State = reader.ReadBoolean();
		}

		public override void Serialize(QNetworkWriter writer)
		{
			base.Serialize(writer);
			writer.Write(State);
		}
	}
}