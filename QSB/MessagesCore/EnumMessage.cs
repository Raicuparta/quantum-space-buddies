using QSB.Messaging;
using QSB.UNet.Networking;

namespace QSB.MessagesCore
{
    public class EnumMessage<T> : PlayerMessage
    {
        public T Value;

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            Value = (T)(object)reader.ReadInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)(object)Value);
        }
    }
}
