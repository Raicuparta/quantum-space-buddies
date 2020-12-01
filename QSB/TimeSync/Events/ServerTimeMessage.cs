using QSB.Messaging;
using QSB.UNet.Networking;

namespace QSB.TimeSync.Events
{
    public class ServerTimeMessage : PlayerMessage
    {
        public float ServerTime { get; set; }
        public int LoopCount { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            ServerTime = reader.ReadSingle();
            LoopCount = reader.ReadInt16();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ServerTime);
            writer.Write(LoopCount);
        }
    }
}