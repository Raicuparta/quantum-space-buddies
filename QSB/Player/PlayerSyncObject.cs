using QSB.UNet.Networking;

namespace QSB.Player
{
    public abstract class PlayerSyncObject : NetworkBehaviour
    {
        public uint AttachedNetId => GetComponent<NetworkIdentity>()?.netId.Value ?? uint.MaxValue;
        public uint PlayerId => this.GetPlayerOfObject();
        public uint PreviousPlayerId { get; set; }
        public PlayerInfo Player => QSBPlayerManager.GetPlayer(PlayerId);
    }
}
