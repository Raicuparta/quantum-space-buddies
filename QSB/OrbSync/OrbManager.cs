using OWML.Common;
using QSB.UNet.Networking;
using QSB.Utility;
using QSB.WorldSync;
using System.Linq;
using UnityEngine;

namespace QSB.OrbSync
{
    public class OrbManager : MonoBehaviour
    {
        public static OrbManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void BuildOrbSlots()
        {
            DebugLog.DebugWrite("Building QSBOrbSlots...", MessageType.Info);
            WorldRegistry.RemoveObjects<QSBOrbSlot>();
            var orbSlots = Resources.FindObjectsOfTypeAll<NomaiInterfaceSlot>();
            for (var id = 0; id < orbSlots.Length; id++)
            {
                var qsbOrbSlot = WorldRegistry.GetObject<QSBOrbSlot>(id) ?? new QSBOrbSlot();
                qsbOrbSlot.Init(orbSlots[id], id);
            }

            DebugLog.DebugWrite($"Finished orb slot build with {orbSlots.Length} slots.", MessageType.Success);
        }

        public void BuildOrbs()
        {
            DebugLog.DebugWrite("Building orb syncs...", MessageType.Info);
            WorldRegistry.OldOrbList.Clear();
            WorldRegistry.OldOrbList = Resources.FindObjectsOfTypeAll<NomaiInterfaceOrb>().ToList();
            if (NetworkServer.active)
            {
                DebugLog.DebugWrite("- Is server, instantiating orb prefabs.");
                WorldRegistry.OrbSyncList.ForEach(x => NetworkServer.Destroy(x.gameObject));
                WorldRegistry.OrbSyncList.Clear();
                WorldRegistry.OldOrbList.ForEach(x => NetworkServer.Spawn(Instantiate(QSBNetworkManager.Instance.OrbPrefab)));
            }
            DebugLog.DebugWrite($"Finished orb build with {WorldRegistry.OldOrbList.Count} orbs.", MessageType.Success);
        }

        public void QueueBuildSlots()
        {
            DebugLog.DebugWrite("Queueing build of QSBOrbSlots...", MessageType.Info);
            QSB.Helper.Events.Unity.RunWhen(() => QSB.HasWokenUp, BuildOrbSlots);
        }

        public void QueueBuildOrbs()
        {
            DebugLog.DebugWrite("Queueing build of NetworkOrbs...", MessageType.Info);
            QSB.Helper.Events.Unity.RunWhen(() => NetworkServer.active, BuildOrbs);
        }
    }
}