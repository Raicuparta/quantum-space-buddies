using QSB.Tools;
using UnityEngine.Networking;

namespace QSB.Tools
{
    public class ProbeMessage : PlayerMessage
    {
        public ProbeData Value { get; set; }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            Value = reader.ReadInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Value);
        }
    }
}