using QSB.UNet.Networking;

namespace QSB.Messaging
{
    public class ToggleMessage : PlayerMessage
    {
        public bool ToggleValue { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            ToggleValue = reader.ReadBoolean();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ToggleValue);
        }
    }
}