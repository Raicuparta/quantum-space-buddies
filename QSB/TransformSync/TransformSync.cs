using OWML.Common;
using QSB.Player;
using QSB.SectorSync;
using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.TransformSync
{
	public abstract class TransformSync : PlayerSyncObject
	{
		public abstract bool IsReady { get; }

		protected abstract Transform InitLocalTransform();
		protected abstract Transform InitRemoteTransform();

		public Transform SyncedTransform { get; private set; }
		public QSBSector ReferenceSector { get; set; }

		private const float SmoothTime = 0.1f;
		private bool _isInitialized;
		private Vector3 _positionSmoothVelocity;
		private Quaternion _rotationSmoothVelocity;
		private bool _isVisible;

		protected override void Start()
		{
			DebugLog.DebugWrite("start of " + GetType().Name);
			base.Start();
			var lowestBound = QSBPlayerManager.GetSyncObjects<PlayerTransformSync>()
				.Where(x => x.NetId.Value <= NetId.Value).OrderBy(x => x.NetId.Value).Last();
			NetIdentity.SetRootIdentity(lowestBound.NetIdentity);

			DontDestroyOnLoad(gameObject);
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (!HasAuthority && SyncedTransform != null)
			{
				Destroy(SyncedTransform.gameObject);
			}
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(OWScene scene, bool isInUniverse) =>
			_isInitialized = false;

		protected void Init()
		{
			SyncedTransform = HasAuthority ? InitLocalTransform() : InitRemoteTransform();
			SetReferenceSector(QSBSectorManager.Instance.GetClosestSector(SyncedTransform));
			_isInitialized = true;
			_isVisible = true;
		}

		public void Update()
		{
			if (!_isInitialized && IsReady)
			{
				Init();
			}
			else if (_isInitialized && !IsReady)
			{
				_isInitialized = false;
				return;
			}

			if (!_isInitialized)
			{
				return;
			}

			if (SyncedTransform == null)
			{
				DebugLog.ToConsole($"Warning - SyncedTransform {Player.PlayerId}.{GetType().Name} is null.", MessageType.Warning);
				return;
			}

			UpdateTransform();
		}

		protected virtual void UpdateTransform()
		{
			if (HasAuthority) // If this script is attached to the client's own body on the client's side.
			{
				if (ReferenceSector == null || ReferenceSector.Sector == null)
				{
					DebugLog.ToConsole($"Error - ReferenceSector has null value for {Player.PlayerId}.{GetType().Name}", MessageType.Error);
					return;
				}
				transform.position = ReferenceSector.Transform.InverseTransformPoint(SyncedTransform.position);
				transform.rotation = ReferenceSector.Transform.InverseTransformRotation(SyncedTransform.rotation);
				return;
			}

			// If this script is attached to any other body, eg the representations of other players
			if (SyncedTransform.position == Vector3.zero)
			{
				Hide();
			}
			else
			{
				Show();
			}

			SyncedTransform.localPosition = Vector3.SmoothDamp(SyncedTransform.localPosition, transform.position, ref _positionSmoothVelocity, SmoothTime);
			SyncedTransform.localRotation = QuaternionHelper.SmoothDamp(SyncedTransform.localRotation, transform.rotation, ref _rotationSmoothVelocity, Time.deltaTime);
		}

		public void SetReferenceSector(QSBSector sector)
		{
			if (sector == ReferenceSector)
			{
				return;
			}
			_positionSmoothVelocity = Vector3.zero;
			ReferenceSector = sector;
			if (!HasAuthority)
			{
				SyncedTransform.SetParent(sector.Transform, true);
				transform.position = sector.Transform.InverseTransformPoint(SyncedTransform.position);
				transform.rotation = sector.Transform.InverseTransformRotation(SyncedTransform.rotation);
			}
		}

		private void Show()
		{
			if (!_isVisible)
			{
				SyncedTransform.gameObject.Show();
				_isVisible = true;
			}
		}

		private void Hide()
		{
			if (_isVisible)
			{
				SyncedTransform.gameObject.Hide();
				_isVisible = false;
			}
		}
	}
}