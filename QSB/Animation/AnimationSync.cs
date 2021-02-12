using OWML.Common;
using OWML.Utils;
using QSB.Events;
using QSB.Player;
using QSB.Utility;
using QuantumUNET.Components;
using System.Linq;
using UnityEngine;

namespace QSB.Animation
{
	public class AnimationSync : PlayerSyncObject
	{
		private Animator _anim;
		private Animator _bodyAnim;
		private QSBNetworkAnimator _netAnim;

		private RuntimeAnimatorController _suitedAnimController;
		private AnimatorOverrideController _unsuitedAnimController;
		private GameObject _suitedGraphics;
		private GameObject _unsuitedGraphics;
		private PlayerCharacterController _playerController;
		private CrouchSync _crouchSync;

		private RuntimeAnimatorController _chertController;
		private readonly RuntimeAnimatorController _eskerController;
		private readonly RuntimeAnimatorController _feldsparController;
		private readonly RuntimeAnimatorController _gabbroController;
		private RuntimeAnimatorController _riebeckController;

		public AnimatorMirror Mirror { get; private set; }
		public AnimationType CurrentType { get; set; }

		protected void Awake()
		{
			_anim = gameObject.AddComponent<Animator>();
			_netAnim = gameObject.AddComponent<QSBNetworkAnimator>();
			_netAnim.enabled = false;
			_netAnim.animator = _anim;

			QSBSceneManager.OnUniverseSceneLoaded += OnUniverseSceneLoaded;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (_playerController == null)
			{
				return;
			}
			_playerController.OnJump -= OnJump;
			_playerController.OnBecomeGrounded -= OnBecomeGrounded;
			_playerController.OnBecomeUngrounded -= OnBecomeUngrounded;

			QSBSceneManager.OnUniverseSceneLoaded -= OnUniverseSceneLoaded;
		}

		private void OnUniverseSceneLoaded(OWScene obj) => LoadControllers();

		private void LoadControllers()
		{
			var bundle = QSBCore.InstrumentAssetBundle;
			_chertController = bundle.LoadAsset("assets/Chert/Traveller_Chert.controller") as RuntimeAnimatorController;
			_riebeckController = bundle.LoadAsset("assets/Riebeck/Traveller_Riebeck.controller") as RuntimeAnimatorController;
		}

		private void InitCommon(Transform body)
		{
			if (QSBSceneManager.IsInUniverse)
			{
				LoadControllers();
			}
			_netAnim.enabled = true;
			_bodyAnim = body.GetComponent<Animator>();
			Mirror = body.gameObject.AddComponent<AnimatorMirror>();
			if (IsLocalPlayer)
			{
				Mirror.Init(_bodyAnim, _anim);
			}
			else
			{
				Mirror.Init(_anim, _bodyAnim);
			}

			for (var i = 0; i < _anim.parameterCount; i++)
			{
				_netAnim.SetParameterAutoSend(i, true);
			}

			var playerAnimController = body.GetComponent<PlayerAnimController>();
			_suitedAnimController = AnimControllerPatch.SuitedAnimController;
			_unsuitedAnimController = playerAnimController.GetValue<AnimatorOverrideController>("_unsuitedAnimOverride");
			_suitedGraphics = playerAnimController.GetValue<GameObject>("_suitedGroup");
			_unsuitedGraphics = playerAnimController.GetValue<GameObject>("_unsuitedGroup");
		}

		public void InitLocal(Transform body)
		{
			InitCommon(body);

			_playerController = body.parent.GetComponent<PlayerCharacterController>();
			_playerController.OnJump += OnJump;
			_playerController.OnBecomeGrounded += OnBecomeGrounded;
			_playerController.OnBecomeUngrounded += OnBecomeUngrounded;

			InitCrouchSync();
		}

		public void InitRemote(Transform body)
		{
			InitCommon(body);

			var playerAnimController = body.GetComponent<PlayerAnimController>();
			playerAnimController.enabled = false;

			playerAnimController.SetValue("_suitedGroup", new GameObject());
			playerAnimController.SetValue("_unsuitedGroup", new GameObject());
			playerAnimController.SetValue("_baseAnimController", null);
			playerAnimController.SetValue("_unsuitedAnimOverride", null);
			playerAnimController.SetValue("_rightArmHidden", false);

			var rightArmObjects = playerAnimController.GetValue<GameObject[]>("_rightArmObjects").ToList();
			rightArmObjects.ForEach(rightArmObject => rightArmObject.layer = LayerMask.NameToLayer("Default"));

			body.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_Head").gameObject.layer = 0;
			body.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_Helmet").gameObject.layer = 0;

			SetAnimationType(AnimationType.PlayerUnsuited);

			InitCrouchSync();

			var ikSync = body.gameObject.AddComponent<PlayerHeadRotationSync>();
			QSBCore.Helper.Events.Unity.RunWhen(() => Player.Camera != null, () => ikSync.Init(Player.Camera.transform));
		}

		private void InitCrouchSync()
		{
			_crouchSync = gameObject.AddComponent<CrouchSync>();
			_crouchSync.Init(this, _playerController, _bodyAnim);
		}

		private void OnJump() => _netAnim.SetTrigger("Jump");

		private void OnBecomeGrounded() => _netAnim.SetTrigger("Grounded");

		private void OnBecomeUngrounded() => _netAnim.SetTrigger("Ungrounded");

		public void SendCrouch(float value = 0) => GlobalMessenger<float>.FireEvent(EventNames.QSBCrouch, value);

		public void HandleCrouch(float value) => _crouchSync.CrouchParam.Target = value;

		private void SuitUp()
		{
			GlobalMessenger<uint, AnimationType>.FireEvent(EventNames.QSBChangeAnimType, PlayerId, AnimationType.PlayerSuited);
			SetAnimationType(AnimationType.PlayerSuited);
		}

		private void SuitDown()
		{
			GlobalMessenger<uint, AnimationType>.FireEvent(EventNames.QSBChangeAnimType, PlayerId, AnimationType.PlayerUnsuited);
			SetAnimationType(AnimationType.PlayerUnsuited);
		}

		public void SetSuitState(bool state)
		{
			if (!Player.IsReady)
			{
				return;
			}
			if (state)
			{
				SuitUp();
				return;
			}
			SuitDown();
		}

		public void SetAnimationType(AnimationType type)
		{
			if (CurrentType == type)
			{
				return;
			}
			CurrentType = type;
			if (_unsuitedAnimController == null)
			{
				DebugLog.ToConsole($"Error - Unsuited controller is null. ({PlayerId})", MessageType.Error);
			}
			if (_suitedAnimController == null)
			{
				DebugLog.ToConsole($"Error - Suited controller is null. ({PlayerId})", MessageType.Error);
			}
			RuntimeAnimatorController controller = default;
			switch (type)
			{
				case AnimationType.PlayerSuited:
					controller = _suitedAnimController;
					_unsuitedGraphics?.SetActive(false);
					_suitedGraphics?.SetActive(true);
					break;

				case AnimationType.PlayerUnsuited:
					controller = _unsuitedAnimController;
					_unsuitedGraphics?.SetActive(true);
					_suitedGraphics?.SetActive(false);
					break;

				case AnimationType.Chert:
					controller = _chertController;
					break;

				case AnimationType.Esker:
					controller = _eskerController;
					break;

				case AnimationType.Feldspar:
					controller = _feldsparController;
					break;

				case AnimationType.Gabbro:
					controller = _gabbroController;
					break;

				case AnimationType.Riebeck:
					controller = _riebeckController;
					break;
			}
			_anim.runtimeAnimatorController = controller;
			_bodyAnim.runtimeAnimatorController = controller;
			if (type != AnimationType.PlayerSuited && type != AnimationType.PlayerUnsuited)
			{
				_bodyAnim.SetTrigger("Playing");
				_anim.SetTrigger("Playing");
			}
			else
			{
				// Avoids "jumping" when exiting instrument and putting on suit
				_bodyAnim.SetTrigger("Grounded");
				_anim.SetTrigger("Grounded");
			}
			_netAnim.animator = _anim; // Probably not needed.
			Mirror.RebuildFloatParams();
			for (var i = 0; i < _anim.parameterCount; i++)
			{
				_netAnim.SetParameterAutoSend(i, true);
			}
		}
	}
}