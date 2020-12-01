using OWML.Common;
using QSB.EventsCore;
using QSB.TransformSync;
using QSB.UNet.Networking;
using QSB.Utility;
using QSB.WorldSync;
using QSB.WorldSync.Events;
using System.Linq;

namespace QSB.OrbSync.Events
{
    public class OrbUserEvent : QSBEvent<WorldObjectMessage>
    {
        public override EventType Type => EventType.OrbUser;

        public override void SetupListener() => GlobalMessenger<int>.AddListener(EventNames.QSBOrbUser, Handler);

        public override void CloseListener() => GlobalMessenger<int>.RemoveListener(EventNames.QSBOrbUser, Handler);

        private void Handler(int id) => SendEvent(CreateMessage(id));

        private WorldObjectMessage CreateMessage(int id) => new WorldObjectMessage
        {
            AboutId = LocalPlayerId,
            ObjectId = id
        };

        public override void OnServerReceive(WorldObjectMessage message)
        {
            var fromPlayer = NetworkServer.connections
                .First(x => x.playerControllers[0].gameObject.GetComponent<PlayerTransformSync>().netId.Value == message.FromId);
            if (WorldRegistry.OrbSyncList.Count == 0)
            {
                DebugLog.ToConsole($"Error - OrbSyncList is empty. (ID {message.ObjectId})", MessageType.Error);
                return;
            }
            var orb = WorldRegistry.OrbSyncList
                .First(x => x.AttachedOrb == WorldRegistry.OldOrbList[message.ObjectId]);
            if (orb == null)
            {
                DebugLog.ToConsole($"Error - No orb found for user event. (ID {message.ObjectId})", MessageType.Error);
                return;
            }
            var orbIdentity = orb.GetComponent<NetworkIdentity>();
            if (orbIdentity == null)
            {
                DebugLog.ToConsole($"Error - Orb identity is null. (ID {message.ObjectId})", MessageType.Error);
                return;
            }
            if (orbIdentity.clientAuthorityOwner != null)
            {
                orbIdentity.RemoveClientAuthority(orbIdentity.clientAuthorityOwner);
            }
            orbIdentity.AssignClientAuthority(fromPlayer);
            orb.enabled = true;
        }

        public override void OnReceiveRemote(WorldObjectMessage message)
        {
            if (WorldRegistry.OrbSyncList.Count < message.ObjectId)
            {
                DebugLog.DebugWrite($"Error - Orb id {message.ObjectId} out of range of orb sync list {WorldRegistry.OrbSyncList.Count}.", MessageType.Error);
                return;
            }
            var orb = WorldRegistry.OrbSyncList
                .First(x => x.AttachedOrb == WorldRegistry.OldOrbList[message.ObjectId]);
            orb.enabled = true;
        }

        public override void OnReceiveLocal(WorldObjectMessage message)
        {
            if (NetworkServer.active)
            {
                OnServerReceive(message);
            }
        }
    }
}
