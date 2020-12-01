using QSB.Messaging;
using QSB.UNet.Networking;

namespace QSB.Player.Events
{
    public class PlayerJoinMessage : PlayerMessage
    {
        public string PlayerName { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            PlayerName = reader.ReadString();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PlayerName);
        }
    }
}