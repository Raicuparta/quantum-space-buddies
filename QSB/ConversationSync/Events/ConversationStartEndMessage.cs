using QSB.Messaging;
using QSB.UNet.Networking;

namespace QSB.ConversationSync.Events
{
    public class ConversationStartEndMessage : PlayerMessage
    {
        public int CharacterId { get; set; }
        public uint PlayerId { get; set; }
        public bool State { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            CharacterId = reader.ReadInt32();
            PlayerId = reader.ReadUInt32();
            State = reader.ReadBoolean();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(CharacterId);
            writer.Write(PlayerId);
            writer.Write(State);
        }
    }
}
