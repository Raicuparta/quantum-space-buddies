using QSB.WorldSync;
using QuantumUNET;
using UnityEngine;

namespace QSB.TransformSync
{
	public class NomaiOrbTransformSync : QSBNetworkBehaviour
	{
		public NomaiInterfaceOrb AttachedOrb { get; private set; }
		public Transform OrbTransform { get; private set; }

		private int Index => QSBWorldSync.OrbSyncList.IndexOf(this);

		private bool _isInitialized;
		private bool _isReady;
		private Transform _orbParent;

		public override void OnStartClient()
		{
			DontDestroyOnLoad(this);
			QSBWorldSync.OrbSyncList.Add(this);

			QSBCore.Helper.Events.Unity.RunWhen(() => QSBCore.HasWokenUp, () => QSBCore.Helper.Events.Unity.FireOnNextUpdate(OnReady));
		}

		private void OnReady()
		{
			AttachedOrb = QSBWorldSync.OldOrbList[Index];
			_isReady = true;
		}

		public void OnDestroy() => QSBWorldSync.OrbSyncList.Remove(this);

		protected void Init()
		{
			OrbTransform = AttachedOrb.transform;
			_orbParent = AttachedOrb.GetAttachedOWRigidbody().GetOrigParent();
			_isInitialized = true;
		}

		public void Update()
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

		private void UpdateTransform()
		{
			if (HasAuthority)
			{
				transform.position = _orbParent.InverseTransformPoint(OrbTransform.position);
				transform.rotation = OrbTransform.rotation;
				return;
			}
			if (transform.position != Vector3.zero)
			{
				OrbTransform.position = _orbParent.TransformPoint(transform.position);
				OrbTransform.rotation = transform.rotation;
			}
		}
	}
}