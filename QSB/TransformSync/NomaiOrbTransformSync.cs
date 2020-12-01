using QSB.UNet.Networking;
using QSB.Utility;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.TransformSync
{
    public class NomaiOrbTransformSync : NetworkBehaviour
    {
        public NomaiInterfaceOrb AttachedOrb { get; private set; }
        public Transform OrbTransform { get; private set; }

        private int Index => WorldRegistry.OrbSyncList.IndexOf(this);

        private const int MaxUpdatesBeforeDisable = 5;

        private bool _isInitialized;
        private bool _isReady;
        private Transform _orbParent;
        private int _updateCount;

        public override void OnStartClient()
        {
            DontDestroyOnLoad(this);
            DebugLog.DebugWrite($"onstartclient orb netid {netId.Value}");
            WorldRegistry.OrbSyncList.Add(this);

            QSB.Helper.Events.Unity.RunWhen(() => QSB.HasWokenUp, () => QSB.Helper.Events.Unity.FireOnNextUpdate(OnReady));
        }

        private void OnReady()
        {
            DebugLog.DebugWrite($"onready orb netid {netId.Value}, index {Index}");
            AttachedOrb = WorldRegistry.OldOrbList[Index];
            _isReady = true;
        }

        private void OnDestroy()
        {
            DebugLog.DebugWrite($"ondestroy orb netid {netId.Value}");
            WorldRegistry.OrbSyncList.Remove(this);
        }

        protected void Init()
        {
            OrbTransform = AttachedOrb.transform;
            _orbParent = AttachedOrb.GetAttachedOWRigidbody().GetOrigParent();
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized && _isReady)
            {
                Init();
            }
            else if (_isInitialized && !_isReady)
            {
                _isInitialized = false;
            }

            if (OrbTransform == null || !_isInitialized)
            {
                return;
            }

            UpdateTransform();
        }

        protected virtual void UpdateTransform()
        {
            if (hasAuthority)
            {
                transform.position = _orbParent.InverseTransformPoint(OrbTransform.position);
                transform.rotation = _orbParent.InverseTransformRotation(OrbTransform.rotation);
                return;
            }
            OrbTransform.position = _orbParent.TransformPoint(transform.position);
            OrbTransform.rotation = _orbParent.InverseTransformRotation(OrbTransform.rotation);

            if (transform.localPosition == Vector3.zero)
            {
                _updateCount++;
            }
            if (_updateCount >= MaxUpdatesBeforeDisable)
            {
                enabled = false;
                _updateCount = 0;
            }
        }
    }
}