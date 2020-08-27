using OWML.Common;
using QSB.Animation;
using QSB.DeathSync;
using QSB.Events;
using QSB.GeyserSync;
using QSB.OrbSync;
using QSB.TimeSync;
using QSB.TransformSync;
using QSB.Utility;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB
{
    public class QSBNetworkManager : NetworkManager
    {
        private const int MaxConnections = 128;

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
            playerPrefab.AddComponent<PlayerTransformSync>();
            playerPrefab.AddComponent<AnimationSync>();
            playerPrefab.AddComponent<WakeUpSync>();
            DebugLog.LogState("PlayerPrefab", playerPrefab);

            _shipPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkship.prefab");
            _shipPrefab.AddComponent<ShipTransformSync>();
            spawnPrefabs.Add(_shipPrefab);
            DebugLog.LogState("ShipPrefab", _shipPrefab);

            _cameraPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkcameraroot.prefab");
            _cameraPrefab.AddComponent<PlayerCameraSync>();
            spawnPrefabs.Add(_cameraPrefab);
            DebugLog.LogState("CameraPrefab", _cameraPrefab);

            _probePrefab = _assetBundle.LoadAsset<GameObject>("assets/networkprobe.prefab");
            _probePrefab.AddComponent<PlayerProbeSync>();
            spawnPrefabs.Add(_probePrefab);
            DebugLog.LogState("ProbePrefab", _probePrefab);

            OrbPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkorb.prefab");
            OrbPrefab.AddComponent<NomaiOrbTransformSync>();
            spawnPrefabs.Add(OrbPrefab);
            DebugLog.LogState("OrbPrefab", OrbPrefab);

            ConfigureNetworkManager();
            QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(OWScene scene, bool inUniverse)
        {
            var orbs = Resources.FindObjectsOfTypeAll<NomaiInterfaceOrb>();
            for (int i = 0; i < orbs.Length; i++)
            {
                var obj = Instantiate(OrbPrefab);
                obj.GetComponent<NomaiOrbTransformSync>().Index = i;
                NetworkServer.Spawn(obj);
            }
        }

        private void ConfigureNetworkManager()
        {
            networkAddress = QSB.DefaultServerIP;
            networkPort = QSB.Port;
            maxConnections = MaxConnections;
            customConfig = true;
            connectionConfig.AddChannel(QosType.Reliable);
            connectionConfig.AddChannel(QosType.Unreliable);
            channels.Add(QosType.Reliable);
            channels.Add(QosType.Unreliable);
        }

        public override void OnServerAddPlayer(NetworkConnection connection, short playerControllerId) // Called on the server when a client joins
        {
            DebugLog.DebugWrite("[S] Add player");
            base.OnServerAddPlayer(connection, playerControllerId);

            // These have to be in a constant order (for now, until we get a better netId getting system...)
            NetworkServer.SpawnWithClientAuthority(Instantiate(_shipPrefab), connection);
            NetworkServer.SpawnWithClientAuthority(Instantiate(_cameraPrefab), connection);
            NetworkServer.SpawnWithClientAuthority(Instantiate(_probePrefab), connection);

            gameObject.AddComponent<Events.PlayerState>();
        }

        public override void OnClientConnect(NetworkConnection connection) // Called on the client when connecting to a server
        {
            base.OnClientConnect(connection);

            gameObject.AddComponent<SectorSync>();
            gameObject.AddComponent<RespawnOnDeath>();
            gameObject.AddComponent<PreventShipDestruction>();

            if (NetworkClient.active && !NetworkServer.active)
            {
                gameObject.AddComponent<Events.PlayerState>();
                GeyserManager.Instance.EmptyUpdate();
                OrbSlotManager.Instance.StopChecking();
                WakeUpPatches.AddPatches();
            }

            _lobby.CanEditName = false;

            OnNetworkManagerReady?.Invoke();
            IsReady = true;

            QSB.Helper.Events.Unity.RunWhen(() => PlayerTransformSync.LocalInstance != null, EventList.Init);

            QSB.Helper.Events.Unity.RunWhen(() => EventList.Ready,
                () => GlobalMessenger<string>.FireEvent(EventNames.QSBPlayerJoin, _lobby.PlayerName));
        }

        public override void OnStopClient() // Called on the client when closing connection
        {
            DebugLog.ToConsole("Disconnecting from server...", MessageType.Info);
            Destroy(GetComponent<SectorSync>());
            Destroy(GetComponent<RespawnOnDeath>());
            Destroy(GetComponent<PreventShipDestruction>());
            EventList.Reset();
            PlayerRegistry.PlayerList.ForEach(player => player.HudMarker?.Remove());

            foreach (var player in PlayerRegistry.PlayerList.Where(x => x.PlayerId != PlayerRegistry.LocalPlayerId).ToList())
            {
                PlayerRegistry.GetPlayerNetIds(player).ForEach(CleanupNetworkBehaviour);
                PlayerRegistry.RemovePlayer(player.PlayerId);
            }

            _lobby.CanEditName = true;
        }

        public override void OnServerDisconnect(NetworkConnection connection) // Called on the server when any client disconnects
        {
            var playerId = connection.playerControllers[0].gameObject.GetComponent<PlayerTransformSync>().netId.Value;
            var netIds = connection.clientOwnedObjects.Select(x => x.Value).ToArray();
            GlobalMessenger<uint, uint[]>.FireEvent(EventNames.QSBPlayerLeave, playerId, netIds);
            PlayerRegistry.GetPlayer(playerId).HudMarker?.Remove();
            CleanupConnection(connection);
        }

        public override void OnStopServer()
        {
            Destroy(GetComponent<SectorSync>());
            Destroy(GetComponent<RespawnOnDeath>());
            Destroy(GetComponent<PreventShipDestruction>());
            EventList.Reset();
            DebugLog.ToConsole("[S] Server stopped!", MessageType.Info);
            PlayerRegistry.PlayerList.ForEach(player => player.HudMarker?.Remove());
            NetworkServer.connections.ToList().ForEach(CleanupConnection);
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
            if (!PlayerRegistry.PlayerExists(playerId))
            {
                return;
            }
            var playerName = PlayerRegistry.GetPlayer(playerId).Name;
            DebugLog.ToConsole($"{playerName} disconnected.", MessageType.Info);
            PlayerRegistry.RemovePlayer(playerId);

            if (playerId != PlayerRegistry.LocalPlayerId) // We don't want to delete the local player!
            {
                var netIds = connection.clientOwnedObjects?.Select(x => x.Value).ToList();
                netIds.ForEach(CleanupNetworkBehaviour);
            }
        }

        public void CleanupNetworkBehaviour(uint netId)
        {
            DebugLog.DebugWrite($"Cleaning up object {netId}");
            // Multiple networkbehaviours can use the same networkidentity (same netId), so get all of them
            var networkBehaviours = FindObjectsOfType<NetworkBehaviour>()
                .Where(x => x != null && x.netId.Value == netId);
            foreach (var networkBehaviour in networkBehaviours)
            {
                var transformSync = networkBehaviour.GetComponent<TransformSync.TransformSync>();

                if (transformSync != null)
                {
                    PlayerRegistry.PlayerSyncObjects.Remove(transformSync);
                    if (transformSync.SyncedTransform != null)
                    {
                        Destroy(transformSync.SyncedTransform.gameObject);
                    }
                }
                Destroy(networkBehaviour.gameObject);
            }
        }

    }
}
