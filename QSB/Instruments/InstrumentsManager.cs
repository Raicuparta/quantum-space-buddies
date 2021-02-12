using OWML.Common;
using QSB.Animation;
using QSB.Events;
using QSB.Instruments.QSBCamera;
using QSB.Player;
using QSB.Utility;
using UnityEngine;

namespace QSB.Instruments
{
	public class InstrumentsManager : PlayerSyncObject
	{
		private Transform _rootObj;
		private AnimationType _savedType;
		private GameObject _chertDrum;

		public void InitLocal(Transform root)
		{
			_rootObj = root;
			gameObject.AddComponent<CameraManager>();

			QSBInputManager.ChertTaunt += OnChertTaunt;
			QSBInputManager.EskerTaunt += OnEskerTaunt;
			QSBInputManager.FeldsparTaunt += OnFeldsparTaunt;
			QSBInputManager.GabbroTaunt += OnGabbroTaunt;
			QSBInputManager.RiebeckTaunt += OnRiebeckTaunt;
			QSBInputManager.ExitTaunt += ReturnToPlayer;

			QSBCore.Helper.Events.Unity.RunWhen(() => Locator.GetPlayerBody() != null, SetupInstruments);
		}

		public void InitRemote(Transform root)
		{
			_rootObj = root;
			QSBCore.Helper.Events.Unity.RunWhen(() => Locator.GetPlayerBody() != null, SetupInstruments);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (!IsLocalPlayer)
			{
				return;
			}
			QSBInputManager.ChertTaunt -= OnChertTaunt;
			QSBInputManager.EskerTaunt -= OnEskerTaunt;
			QSBInputManager.FeldsparTaunt -= OnFeldsparTaunt;
			QSBInputManager.GabbroTaunt -= OnGabbroTaunt;
			QSBInputManager.RiebeckTaunt -= OnRiebeckTaunt;
			QSBInputManager.ExitTaunt -= ReturnToPlayer;
		}

		private void OnChertTaunt() => StartInstrument(AnimationType.Chert);
		private void OnEskerTaunt() => StartInstrument(AnimationType.Esker);
		private void OnFeldsparTaunt() => StartInstrument(AnimationType.Feldspar);
		private void OnGabbroTaunt() => StartInstrument(AnimationType.Gabbro);
		private void OnRiebeckTaunt() => StartInstrument(AnimationType.Riebeck);

		private void SetupInstruments()
		{
			var bundle = QSBCore.InstrumentAssetBundle;
			_chertDrum = MakeChertDrum(bundle);
		}

		private GameObject MakeChertDrum(AssetBundle bundle)
		{
			var drum = new GameObject();
			var mf = drum.AddComponent<MeshFilter>();
			mf.sharedMesh = bundle.LoadAsset("assets/Chert/hourglasstwinsmeshescharacters2.asset") as Mesh;
			var mr = drum.AddComponent<MeshRenderer>();
			if (QSBSceneManager.CurrentScene == OWScene.SolarSystem)
			{
				mr.sharedMaterial = GameObject.Find("NewDrum:polySurface2").GetComponent<MeshRenderer>().material;
			}
			else if (QSBSceneManager.CurrentScene == OWScene.EyeOfTheUniverse)
			{
				//mr.sharedMaterial = GameObject.Find("Props_HEA_Drums").GetComponent<MeshRenderer>().material;
				// TODO : fix for instrument release
				mr.sharedMaterial = null;
			}
			drum.transform.parent = _rootObj;
			drum.transform.rotation = _rootObj.rotation;
			drum.transform.localPosition = Vector3.zero;
			drum.transform.localScale = new Vector3(16.0f, 16.5f, 16.0f);
			drum.SetActive(false);

			return drum;
		}

		public void StartInstrument(AnimationType type)
		{
			if (!IsLocalPlayer)
			{
				DebugLog.ToConsole("Error - Tried to start instrument on non-local player!", MessageType.Error);
				return;
			}
			if (Player.PlayingInstrument || !Locator.GetPlayerController().IsGrounded())
			{
				return;
			}
			_savedType = Player.AnimationSync.CurrentType;
			CameraManager.Instance.SwitchTo3rdPerson();
			SwitchToType(type);
		}

		public void ReturnToPlayer()
		{
			if (!Player.PlayingInstrument)
			{
				return;
			}
			CameraManager.Instance.SwitchTo1stPerson();
			SwitchToType(_savedType);
		}

		public void SwitchToType(AnimationType type)
		{
			GlobalMessenger<uint, AnimationType>.FireEvent(EventNames.QSBChangeAnimType, QSBPlayerManager.LocalPlayerId, type);
			QSBPlayerManager.LocalPlayer.AnimationSync.SetAnimationType(type);
			CheckInstrumentProps(type);
		}

		public void CheckInstrumentProps(AnimationType type)
		{
			switch (type)
			{
				case AnimationType.Chert:
					_chertDrum?.SetActive(true);
					break;
				case AnimationType.PlayerSuited:
				case AnimationType.PlayerUnsuited:
					_chertDrum?.SetActive(false);
					break;
			}
		}
	}
}