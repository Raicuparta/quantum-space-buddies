using QSB.Messaging;
using QSB.UNet.Networking;

namespace QSB.MessagesCore
{
    public class FloatMessage : PlayerMessage
    {
        public float Value;

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            Value = reader.ReadSingle();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Value);
        }
    }
}