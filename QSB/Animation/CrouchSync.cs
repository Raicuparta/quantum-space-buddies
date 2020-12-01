using QSB.UNet.Networking;
using System;
using UnityEngine;

namespace QSB.Animation
{
    public class CrouchSync : NetworkBehaviour
    {
        public AnimFloatParam CrouchParam { get; } = new AnimFloatParam();

        private const float CrouchSendInterval = 0.1f;
        private const float CrouchChargeThreshold = 0.01f;
        private const float CrouchSmoothTime = 0.05f;
        private const int CrouchLayerIndex = 1;

        private float _sendTimer;
        private float _lastSentJumpChargeFraction;

        private AnimationSync _animationSync;
        private PlayerCharacterController _playerController;
        private Animator _bodyAnim;

        public void Init(AnimationSync animationSync, PlayerCharacterController playerController, Animator bodyAnim)
        {
            _animationSync = animationSync;
            _playerController = playerController;
            _bodyAnim = bodyAnim;
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                SyncLocalCrouch();
                return;
            }
            SyncRemoteCrouch();
        }

        private void SyncLocalCrouch()
        {
            if (_playerController == null)
            {
                return;
            }
            _sendTimer += Time.unscaledDeltaTime;
            if (_sendTimer < CrouchSendInterval)
            {
                return;
            }
            var jumpChargeFraction = _playerController.GetJumpChargeFraction();
            if (Math.Abs(jumpChargeFraction - _lastSentJumpChargeFraction) < CrouchChargeThreshold)
            {
                return;
            }
            _animationSync.SendCrouch(jumpChargeFraction);
            _lastSentJumpChargeFraction = jumpChargeFraction;
            _sendTimer = 0;
        }

        private void SyncRemoteCrouch()
        {
            if (_bodyAnim == null)
            {
                return;
            }
            CrouchParam.Smooth(CrouchSmoothTime);
            var jumpChargeFraction = CrouchParam.Current;
            _bodyAnim.SetLayerWeight(CrouchLayerIndex, jumpChargeFraction);
        }
    }
}