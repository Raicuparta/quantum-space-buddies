using QSB.UNet.Networking;

namespace QSB.Messaging
{
    public class PlayerMessage : MessageBase
    {
        public uint FromId { get; set; }
        public uint AboutId { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            FromId = reader.ReadUInt32();
            AboutId = reader.ReadUInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(FromId);
            writer.Write(AboutId);
        }
    }
}
