using OWML.Common;
using OWML.ModHelper.Events;
using QSB.Animation;
using QSB.DeathSync;
using QSB.ElevatorSync;
using QSB.EventsCore;
using QSB.GeyserSync;
using QSB.Instruments;
using QSB.OrbSync;
using QSB.Patches;
using QSB.Player;
using QSB.SectorSync;
using QSB.TimeSync;
using QSB.TransformSync;
using QSB.UNet.Networking;
using QSB.Utility;
using QSB.WorldSync;
using System;
using System.Linq;
using UnityEngine;

namespace QSB
{
    public class QSBNetworkManager : NetworkManager
    {
        private const int MaxConnections = 128;
        private const int MaxBufferedPackets = 64;

        public static QSBNetworkManager Instance { get; private set; }

        public event Action OnNetworkManagerReady;
        public bool IsReady { get; private set; }

        private QSBNetworkLobby _lobby;
        private AssetBundle _assetBundle;
        private GameObject _shipPrefab;
        private GameObject _cameraPrefab;
        private GameObject _probePrefab;
        public GameObject OrbPrefab;

        private void Awake()
        {
            Instance = this;

            _lobby = gameObject.AddComponent<QSBNetworkLobby>();
            _assetBundle = QSB.NetworkAssetBundle;

            playerPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkplayer.prefab");
            playerPrefab.AddComponent<NetworkIdentity>();
            playerPrefab.AddComponent<PlayerTransformSync>();
            playerPrefab.AddComponent<AnimationSync>();
            playerPrefab.AddComponent<WakeUpSync>();
            playerPrefab.AddComponent<InstrumentsManager>();

            _shipPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkship.prefab");
            _shipPrefab.AddComponent<NetworkIdentity>();
            _shipPrefab.AddComponent<ShipTransformSync>();
            spawnPrefabs.Add(_shipPrefab);

            _cameraPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkcameraroot.prefab");
            _cameraPrefab.AddComponent<NetworkIdentity>();
            _cameraPrefab.AddComponent<PlayerCameraSync>();
            spawnPrefabs.Add(_cameraPrefab);

            _probePrefab = _assetBundle.LoadAsset<GameObject>("assets/networkprobe.prefab");
            _probePrefab.AddComponent<NetworkIdentity>();
            _probePrefab.AddComponent<PlayerProbeSync>();
            spawnPrefabs.Add(_probePrefab);

            OrbPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkorb.prefab");
            OrbPrefab.AddComponent<NetworkIdentity>();
            OrbPrefab.AddComponent<NomaiOrbTransformSync>();
            spawnPrefabs.Add(OrbPrefab);

            ConfigureNetworkManager();
            QSBSceneManager.OnUniverseSceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
            => QSBSceneManager.OnUniverseSceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(OWScene scene)
        {
            DebugLog.DebugWrite("scene loaded");
            OrbManager.Instance.BuildOrbs();
            OrbManager.Instance.QueueBuildSlots();
            WorldRegistry.OldDialogueTrees.Clear();
            WorldRegistry.OldDialogueTrees = Resources.FindObjectsOfTypeAll<CharacterDialogueTree>().ToList();
        }

        private void ConfigureNetworkManager()
        {
            networkAddress = QSB.DefaultServerIP;
            networkPort = QSB.Port;
            maxConnections = MaxConnections;
            customConfig = true;
            connectionConfig.AddChannel(QosType.Reliable);
            connectionConfig.AddChannel(QosType.Unreliable);
            this.SetValue("m_MaxBufferedPackets", MaxBufferedPackets);
            channels.Add(QosType.Reliable);
            channels.Add(QosType.Unreliable);

            DebugLog.DebugWrite("Network Manager ready.", MessageType.Success);
        }

        public override void OnStartServer()
        {
            DebugLog.DebugWrite("~~ ON START SERVER ~~", MessageType.Info);
            if (WorldRegistry.OrbSyncList.Count == 0 && QSBSceneManager.IsInUniverse)
            {
                OrbManager.Instance.QueueBuildOrbs();
            }
            if (WorldRegistry.OldDialogueTrees.Count == 0 && QSBSceneManager.IsInUniverse)
            {
                WorldRegistry.OldDialogueTrees = Resources.FindObjectsOfTypeAll<CharacterDialogueTree>().ToList();
            }
        }

        public override void OnServerAddPlayer(NetworkConnection connection, short playerControllerId) // Called on the server when a client joins
        {
            DebugLog.DebugWrite("[S] Add player");
            base.OnServerAddPlayer(connection, playerControllerId);

            // These have to be in a constant order (for now, until we get a better netId getting system...)
            NetworkServer.SpawnWithClientAuthority(Instantiate(_shipPrefab), connection);
            NetworkServer.SpawnWithClientAuthority(Instantiate(_cameraPrefab), connection);
            NetworkServer.SpawnWithClientAuthority(Instantiate(_probePrefab), connection);
        }

        public override void OnClientConnect(NetworkConnection connection) // Called on the client when connecting to a server
        {
            base.OnClientConnect(connection);

            gameObject.AddComponent<SectorSync.SectorSync>();
            gameObject.AddComponent<RespawnOnDeath>();
            gameObject.AddComponent<PreventShipDestruction>();

            if (QSBSceneManager.IsInUniverse)
            {
                QSBSectorManager.Instance.RebuildSectors();
                OrbManager.Instance.QueueBuildSlots();
            }

            if (!NetworkServer.localClientActive)
            {
                QSBPatchManager.DoPatchType(QSBPatchTypes.OnNonServerClientConnect);
            }

            QSBPatchManager.DoPatchType(QSBPatchTypes.OnClientConnect);

            _lobby.CanEditName = false;

            OnNetworkManagerReady?.Invoke();
            IsReady = true;

            QSB.Helper.Events.Unity.RunWhen(() => PlayerTransformSync.LocalInstance != null, QSBEventManager.Init);

            QSB.Helper.Events.Unity.RunWhen(() => QSBEventManager.Ready,
                () => GlobalMessenger<string>.FireEvent(EventNames.QSBPlayerJoin, _lobby.PlayerName));

            QSB.Helper.Events.Unity.RunWhen(() => QSBEventManager.Ready,
                () => GlobalMessenger.FireEvent(EventNames.QSBPlayerStatesRequest));
        }

        public override void OnStopClient() // Called on the client when closing connection
        {
            DebugLog.ToConsole("Disconnecting from server...", MessageType.Info);
            Destroy(GetComponent<SectorSync.SectorSync>());
            Destroy(GetComponent<RespawnOnDeath>());
            Destroy(GetComponent<PreventShipDestruction>());
            QSBEventManager.Reset();
            QSBPlayerManager.PlayerList.ForEach(player => player.HudMarker?.Remove());

            foreach (var player in QSBPlayerManager.PlayerList)
            {
                QSBPlayerManager.GetPlayerNetIds(player).ForEach(CleanupNetworkBehaviour);
            }
            QSBPlayerManager.RemoveAllPlayers();

            WorldRegistry.RemoveObjects<QSBOrbSlot>();
            WorldRegistry.RemoveObjects<QSBElevator>();
            WorldRegistry.RemoveObjects<QSBGeyser>();
            WorldRegistry.RemoveObjects<QSBSector>();
            WorldRegistry.OrbSyncList.Clear();
            WorldRegistry.OldDialogueTrees.Clear();

            _lobby.CanEditName = true;
        }

        public override void OnServerDisconnect(NetworkConnection connection) // Called on the server when any client disconnects
        {
            var playerId = connection.playerControllers[0].gameObject.GetComponent<PlayerTransformSync>().netId.Value;
            var netIds = connection.clientOwnedObjects.Select(x => x.Value).ToArray();
            GlobalMessenger<uint, uint[]>.FireEvent(EventNames.QSBPlayerLeave, playerId, netIds);

            foreach (var item in WorldRegistry.OrbSyncList)
            {
                var identity = item.GetComponent<NetworkIdentity>();
                if (identity.clientAuthorityOwner == connection)
                {
                    identity.RemoveClientAuthority(connection);
                }
            }

            QSBPlayerManager.GetPlayer(playerId).HudMarker?.Remove();
            CleanupConnection(connection);
        }

        public override void OnStopServer()
        {
            Destroy(GetComponent<SectorSync.SectorSync>());
            Destroy(GetComponent<RespawnOnDeath>());
            Destroy(GetComponent<PreventShipDestruction>());
            QSBEventManager.Reset();
            DebugLog.ToConsole("[S] Server stopped!", MessageType.Info);
            QSBPlayerManager.PlayerList.ForEach(player => player.HudMarker?.Remove());
            NetworkServer.connections.ToList().ForEach(CleanupConnection);

            WorldRegistry.RemoveObjects<QSBOrbSlot>();
            WorldRegistry.RemoveObjects<QSBElevator>();
            WorldRegistry.RemoveObjects<QSBGeyser>();
            WorldRegistry.RemoveObjects<QSBSector>();

            base.OnStopServer();
        }

        private void CleanupConnection(NetworkConnection connection)
        {
            uint playerId;
            try
            {
                playerId = connection.playerControllers[0].gameObject.GetComponent<PlayerTransformSync>().netId.Value;
            }
            catch (Exception ex)
            {
                DebugLog.ToConsole("Error when getting playerId in CleanupConnection: " + ex.Message, MessageType.Error);
                return;
            }
            if (!QSBPlayerManager.PlayerExists(playerId))
            {
                return;
            }
            var playerName = QSBPlayerManager.GetPlayer(playerId).Name;
            DebugLog.ToConsole($"{playerName} disconnected.", MessageType.Info);
            QSBPlayerManager.RemovePlayer(playerId);

            var netIds = connection.clientOwnedObjects?.Select(x => x.Value).ToList();
            netIds.ForEach(CleanupNetworkBehaviour);
        }

        public void CleanupNetworkBehaviour(uint netId)
        {
            DebugLog.DebugWrite($"Cleaning up netId {netId}");
            // Multiple networkbehaviours can use the same networkidentity (same netId), so get all of them
            var networkBehaviours = FindObjectsOfType<NetworkBehaviour>()
                .Where(x => x != null && x.netId.Value == netId);
            foreach (var networkBehaviour in networkBehaviours)
            {
                var transformSync = networkBehaviour.GetComponent<TransformSync.TransformSync>();

                if (transformSync != null)
                {
                    DebugLog.DebugWrite($"  * Removing TransformSync from syncobjects");
                    QSBPlayerManager.PlayerSyncObjects.Remove(transformSync);
                    if (transformSync.SyncedTransform != null && netId != QSBPlayerManager.LocalPlayerId && !networkBehaviour.hasAuthority)
                    {
                        DebugLog.DebugWrite($"  * Destroying {transformSync.SyncedTransform.gameObject.name}");
                        Destroy(transformSync.SyncedTransform.gameObject);
                    }
                }

                var animationSync = networkBehaviour.GetComponent<AnimationSync>();

                if (animationSync != null)
                {
                    DebugLog.DebugWrite($"  * Removing AnimationSync from syncobjects");
                    QSBPlayerManager.PlayerSyncObjects.Remove(animationSync);
                }

                if (!networkBehaviour.hasAuthority)
                {
                    DebugLog.DebugWrite($"  * Destroying {networkBehaviour.gameObject.name}");
                    Destroy(networkBehaviour.gameObject);
                }
            }
        }
    }
}