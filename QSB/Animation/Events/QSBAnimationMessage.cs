using QSB.UNet.Networking;

namespace QSB.Animation.Events
{
    class QSBAnimationMessage : MessageBase
    {
        public NetworkInstanceId netId;
        public int stateHash;
        public float normalizedTime;
        public byte[] parameters;

        public override void Deserialize(NetworkReader reader)
        {
            netId = reader.ReadNetworkId();
            stateHash = (int)reader.ReadPackedUInt32();
            normalizedTime = reader.ReadSingle();
            parameters = reader.ReadBytesAndSize();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(netId);
            writer.WritePackedUInt32((uint)stateHash);
            writer.Write(normalizedTime);
            if (parameters == null)
            {
                writer.WriteBytesAndSize(parameters, 0);
            }
            else
            {
                writer.WriteBytesAndSize(parameters, parameters.Length);
            }
        }
    }
}