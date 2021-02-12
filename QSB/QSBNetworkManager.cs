using OWML.Common;
using OWML.Utils;
using QSB.Animation;
using QSB.DeathSync;
using QSB.ElevatorSync;
using QSB.Events;
using QSB.GeyserSync;
using QSB.Instruments;
using QSB.OrbSync;
using QSB.Patches;
using QSB.Player;
using QSB.SectorSync;
using QSB.TimeSync;
using QSB.TransformSync;
using QSB.Utility;
using QSB.WorldSync;
using QuantumUNET;
using QuantumUNET.Components;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB
{
	public class QSBNetworkManager : QSBNetworkManagerUNET
	{
		public static QSBNetworkManager Instance { get; private set; }

		public event Action OnNetworkManagerReady;

		public bool IsReady { get; private set; }
		public GameObject OrbPrefab { get; set; }

		private const int MaxConnections = 128;
		private const int MaxBufferedPackets = 64;

		private QSBNetworkLobby _lobby;
		private AssetBundle _assetBundle;
		private GameObject _shipPrefab;
		private GameObject _cameraPrefab;
		private GameObject _probePrefab;

		public void Awake()
		{
			base.Awake();
			Instance = this;

			_lobby = gameObject.AddComponent<QSBNetworkLobby>();
			_assetBundle = QSBCore.NetworkAssetBundle;

			playerPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkplayer.prefab");
			SetupNetworkId(playerPrefab);
			SetupNetworkTransform(playerPrefab);
			playerPrefab.AddComponent<PlayerTransformSync>();
			playerPrefab.AddComponent<AnimationSync>();
			playerPrefab.AddComponent<WakeUpSync>();
			playerPrefab.AddComponent<InstrumentsManager>();

			_shipPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkship.prefab");
			SetupNetworkId(_shipPrefab);
			SetupNetworkTransform(_shipPrefab);
			_shipPrefab.AddComponent<ShipTransformSync>();
			spawnPrefabs.Add(_shipPrefab);

			_cameraPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkcameraroot.prefab");
			SetupNetworkId(_cameraPrefab);
			SetupNetworkTransform(_cameraPrefab);
			_cameraPrefab.AddComponent<PlayerCameraSync>();
			spawnPrefabs.Add(_cameraPrefab);

			_probePrefab = _assetBundle.LoadAsset<GameObject>("assets/networkprobe.prefab");
			SetupNetworkId(_probePrefab);
			SetupNetworkTransform(_probePrefab);
			_probePrefab.AddComponent<PlayerProbeSync>();
			spawnPrefabs.Add(_probePrefab);

			OrbPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkorb.prefab");
			SetupNetworkId(OrbPrefab);
			SetupNetworkTransform(OrbPrefab);
			OrbPrefab.AddComponent<NomaiOrbTransformSync>();
			spawnPrefabs.Add(OrbPrefab);

			ConfigureNetworkManager();
			QSBSceneManager.OnUniverseSceneLoaded += OnSceneLoaded;
		}

		private void SetupNetworkId(GameObject go)
		{
			var ident = go.AddComponent<QSBNetworkIdentity>();
			ident.LocalPlayerAuthority = true;
			ident.SetValue("m_AssetId", go.GetComponent<NetworkIdentity>().assetId);
			ident.SetValue("m_SceneId", go.GetComponent<NetworkIdentity>().sceneId);
		}

		private void SetupNetworkTransform(GameObject go)
		{
			var trans = go.AddComponent<QSBNetworkTransform>();
			trans.SendInterval = 0.1f;
			trans.SyncRotationAxis = QSBNetworkTransform.AxisSyncMode.AxisXYZ;
			Destroy(go.GetComponent<NetworkTransform>());
			Destroy(go.GetComponent<NetworkIdentity>());
		}

		public void OnDestroy() =>
			QSBSceneManager.OnUniverseSceneLoaded -= OnSceneLoaded;

		private void OnSceneLoaded(OWScene scene)
		{
			OrbManager.Instance.BuildOrbs();
			OrbManager.Instance.QueueBuildSlots();
			QSBWorldSync.OldDialogueTrees.Clear();
			QSBWorldSync.OldDialogueTrees = Resources.FindObjectsOfTypeAll<CharacterDialogueTree>().ToList();
		}

		private void ConfigureNetworkManager()
		{
			networkAddress = QSBCore.DefaultServerIP;
			networkPort = QSBCore.Port;
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
			DebugLog.DebugWrite("OnStartServer", MessageType.Info);
			if (QSBWorldSync.OrbSyncList.Count == 0 && QSBSceneManager.IsInUniverse)
			{
				OrbManager.Instance.QueueBuildOrbs();
			}
			if (QSBWorldSync.OldDialogueTrees.Count == 0 && QSBSceneManager.IsInUniverse)
			{
				QSBWorldSync.OldDialogueTrees = Resources.FindObjectsOfTypeAll<CharacterDialogueTree>().ToList();
			}
		}

		public override void OnServerAddPlayer(QSBNetworkConnection connection, short playerControllerId) // Called on the server when a client joins
		{
			DebugLog.DebugWrite($"OnServerAddPlayer {playerControllerId}", MessageType.Info);
			base.OnServerAddPlayer(connection, playerControllerId);

			QSBNetworkServer.SpawnWithClientAuthority(Instantiate(_shipPrefab), connection);
			QSBNetworkServer.SpawnWithClientAuthority(Instantiate(_cameraPrefab), connection);
			QSBNetworkServer.SpawnWithClientAuthority(Instantiate(_probePrefab), connection);
		}

		public override void OnStartClient(QSBNetworkClient _)
		{
			DebugLog.DebugWrite($"Setting defaultServerIP to {networkAddress}");
			var config = QSBCore.Helper.Config;
			config.SetSettingsValue("defaultServerIP", networkAddress);
			QSBCore.Helper.Storage.Save(config, Constants.ModConfigFileName);
		}

		public override void OnClientConnect(QSBNetworkConnection connection) // Called on the client when connecting to a server
		{
			DebugLog.DebugWrite("OnClientConnect", MessageType.Info);
			base.OnClientConnect(connection);

			QSBEventManager.Init();

			gameObject.AddComponent<SectorSync.SectorSync>();
			gameObject.AddComponent<RespawnOnDeath>();
			gameObject.AddComponent<PreventShipDestruction>();

			if (QSBSceneManager.IsInUniverse)
			{
				QSBSectorManager.Instance.RebuildSectors();
				OrbManager.Instance.QueueBuildSlots();
			}

			if (!QSBNetworkServer.localClientActive)
			{
				QSBPatchManager.DoPatchType(QSBPatchTypes.OnNonServerClientConnect);
			}

			QSBPatchManager.DoPatchType(QSBPatchTypes.OnClientConnect);

			_lobby.CanEditName = false;

			OnNetworkManagerReady?.Invoke();
			IsReady = true;

			QSBCore.Helper.Events.Unity.RunWhen(() => QSBEventManager.Ready && PlayerTransformSync.LocalInstance != null,
				() => GlobalMessenger<string>.FireEvent(EventNames.QSBPlayerJoin, _lobby.PlayerName));

			if (!QSBCore.IsServer)
			{
				QSBCore.Helper.Events.Unity.RunWhen(() => QSBEventManager.Ready && PlayerTransformSync.LocalInstance != null,
				() => GlobalMessenger.FireEvent(EventNames.QSBPlayerStatesRequest));
			}
		}

		public override void OnStopClient() // Called on the client when closing connection
		{
			DebugLog.DebugWrite("OnStopClient", MessageType.Info);
			DebugLog.ToConsole("Disconnecting from server...", MessageType.Info);
			Destroy(GetComponent<SectorSync.SectorSync>());
			Destroy(GetComponent<RespawnOnDeath>());
			Destroy(GetComponent<PreventShipDestruction>());
			QSBEventManager.Reset();
			QSBPlayerManager.PlayerList.ForEach(player => player.HudMarker?.Remove());

			QSBPlayerManager.RemoveAllPlayers();

			QSBWorldSync.RemoveWorldObjects<QSBOrbSlot>();
			QSBWorldSync.RemoveWorldObjects<QSBElevator>();
			QSBWorldSync.RemoveWorldObjects<QSBGeyser>();
			QSBWorldSync.RemoveWorldObjects<QSBSector>();
			QSBWorldSync.OrbSyncList.Clear();
			QSBWorldSync.OldDialogueTrees.Clear();

			_lobby.CanEditName = true;
		}

		public override void OnServerDisconnect(QSBNetworkConnection connection) // Called on the server when any client disconnects
		{
			base.OnServerDisconnect(connection);
			DebugLog.DebugWrite("OnServerDisconnect", MessageType.Info);

			foreach (var item in QSBWorldSync.OrbSyncList)
			{
				var identity = item.GetComponent<QSBNetworkIdentity>();
				if (identity.ClientAuthorityOwner == connection)
				{
					identity.RemoveClientAuthority(connection);
				}
			}
		}

		public override void OnStopServer()
		{
			DebugLog.DebugWrite("OnStopServer", MessageType.Info);
			Destroy(GetComponent<SectorSync.SectorSync>());
			Destroy(GetComponent<RespawnOnDeath>());
			Destroy(GetComponent<PreventShipDestruction>());
			QSBEventManager.Reset();
			DebugLog.ToConsole("Server stopped!", MessageType.Info);
			QSBPlayerManager.PlayerList.ForEach(player => player.HudMarker?.Remove());

			QSBWorldSync.RemoveWorldObjects<QSBOrbSlot>();
			QSBWorldSync.RemoveWorldObjects<QSBElevator>();
			QSBWorldSync.RemoveWorldObjects<QSBGeyser>();
			QSBWorldSync.RemoveWorldObjects<QSBSector>();

			base.OnStopServer();
		}
	}
}