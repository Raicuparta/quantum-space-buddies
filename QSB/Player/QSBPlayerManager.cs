using OWML.Common;
using QSB.Player.Events;
using QSB.TransformSync;
using QSB.UNet.Networking;
using QSB.Utility;
using System.Collections.Generic;
using System.Linq;

namespace QSB.Player
{
    public static class QSBPlayerManager
    {
        public static uint LocalPlayerId => PlayerTransformSync.LocalInstance.GetComponent<NetworkIdentity>()?.netId.Value ?? uint.MaxValue;
        public static PlayerInfo LocalPlayer => GetPlayer(LocalPlayerId);
        public static List<PlayerInfo> PlayerList { get; } = new List<PlayerInfo>();

        public static List<PlayerSyncObject> PlayerSyncObjects { get; } = new List<PlayerSyncObject>();

        public static PlayerInfo GetPlayer(uint id)
        {
            if (id == uint.MaxValue || id == 0U)
            {
                return default;
            }
            var player = PlayerList.FirstOrDefault(x => x.PlayerId == id);
            if (player != null)
            {
                return player;
            }
            DebugLog.DebugWrite($"Creating player id {id}", MessageType.Info);
            player = new PlayerInfo(id);
            PlayerList.Add(player);
            return player;
        }

        public static void RemovePlayer(uint id)
        {
            DebugLog.DebugWrite($"Removing player {GetPlayer(id).Name} id {id}", MessageType.Info);
            PlayerList.Remove(GetPlayer(id));
        }

        public static void RemoveAllPlayers()
        {
            DebugLog.DebugWrite($"Removing all players.", MessageType.Info);
            PlayerList.Clear();
        }

        public static bool PlayerExists(uint id)
        {
            return id != uint.MaxValue && PlayerList.Any(x => x.PlayerId == id);
        }

        public static void HandleFullStateMessage(PlayerStateMessage message)
        {
            var player = GetPlayer(message.AboutId);
            player.Name = message.PlayerName;
            player.IsReady = message.PlayerReady;
            player.State = message.PlayerState;
            if (LocalPlayer.IsReady)
            {
                player.UpdateStateObjects();
            }
        }

        public static IEnumerable<T> GetSyncObjects<T>() where T : PlayerSyncObject
        {
            return PlayerSyncObjects.OfType<T>().Where(x => x != null);
        }

        public static T GetSyncObject<T>(uint id) where T : PlayerSyncObject
        {
            return GetSyncObjects<T>().FirstOrDefault(x => x != null && x.AttachedNetId == id);
        }

        public static bool IsBelongingToLocalPlayer(uint id)
        {
            return id == LocalPlayerId ||
                PlayerSyncObjects.Any(x => x != null && x.AttachedNetId == id && x.isLocalPlayer);
        }

        public static uint GetPlayerOfObject(this PlayerSyncObject syncObject)
        {
            if (PlayerList.Count == 0)
            {
                DebugLog.ToConsole($"Error - No players exist to find owner of object. (Attached NetID : {syncObject.AttachedNetId})", MessageType.Error);
                syncObject.PreviousPlayerId = uint.MaxValue;
                return uint.MaxValue;
            }
            // Get all Player IDs
            var playerIds = PlayerList.Select(x => x.PlayerId).ToList();
            // Get highest ID below the given syncobject's netid. A netid cannot belong to a playerid above it, only below or equal to it.
            var lowerBound = playerIds.Where(x => x <= syncObject.AttachedNetId).ToList().Max();
            if (playerIds.Min() > syncObject.AttachedNetId)
            {
                DebugLog.ToConsole($"Warning - Minimum playerid is greater than syncobject's netid. (Attached NetID : {syncObject.AttachedNetId})", MessageType.Warning);
                syncObject.PreviousPlayerId = uint.MaxValue;
                return uint.MaxValue;
            }
            // If the player list count is not the same as the count of the same type syncobject (eg. 3 players and 4 PlayerTransformSyncs) 
            // and the highest ID below the syncobject's id is the same as the highest player id.
            if (PlayerList.Count != PlayerSyncObjects.Count(x => x.GetType() == syncObject.GetType()) && lowerBound == playerIds.Max())
            {
                // If the previous player id was not the error value, return it. To smooth over discrepancies between player list and object list.
                if (syncObject.PreviousPlayerId != uint.MaxValue)
                {
                    return syncObject.PreviousPlayerId;
                }
                // If the syncobject is a PlayerTransformSync, make a player.
                if (syncObject.GetType() == typeof(PlayerTransformSync) && syncObject.AttachedNetId != 0U)
                {
                    return GetPlayer(syncObject.AttachedNetId).PlayerId;
                }
                DebugLog.ToConsole($"Warning - Unequal player:syncobject count. ({PlayerList.Count}:{PlayerSyncObjects.Count(x => x.GetType() == syncObject.GetType())}) (Attached NetID : {syncObject.AttachedNetId})", MessageType.Warning);
                syncObject.PreviousPlayerId = uint.MaxValue;
                return uint.MaxValue;
            }
            if (syncObject.PreviousPlayerId == uint.MaxValue)
            {
                DebugLog.ToConsole($"Warning - Syncobject previously had uint.MaxValue as it's playerid. (Attached NetID : {syncObject.AttachedNetId})", MessageType.Warning);
            }
            syncObject.PreviousPlayerId = lowerBound;
            return lowerBound;
        }

        public static List<uint> GetPlayerNetIds(PlayerInfo player)
        {
            if (PlayerSyncObjects.Count == 0)
            {
                return default;
            }
            int count = 0;
            int totalCount = PlayerSyncObjects.Count;
            PlayerSyncObjects.RemoveAll(x => x == null);
            PlayerSyncObjects.RemoveAll(x => x.GetComponent<NetworkIdentity>() == null);
            if (PlayerSyncObjects.Count != totalCount)
            {
                DebugLog.ToConsole($"Warning - Removed {totalCount - PlayerSyncObjects.Count} items from PlayerSyncObjects.", MessageType.Warning);
            }
            foreach (var item in PlayerSyncObjects.DistinctBy(x => x.AttachedNetId))
            {
                if (item.PlayerId == player.PlayerId)
                {
                    count++;
                }
            }
            return Enumerable.Range((int)player.PlayerId, count).Select(x => (uint)x).ToList();
        }
    }
}
