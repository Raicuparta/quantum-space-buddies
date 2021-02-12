using QSB.Events;
using QSB.Player;
using System.Linq;
using UnityEngine;

namespace QSB.SectorSync
{
	public class SectorSync : MonoBehaviour
	{
		private const float CheckInterval = 0.5f;
		private float _checkTimer = CheckInterval;

		public void Update()
		{
			if (!QSBSectorManager.Instance.IsReady)
			{
				return;
			}
			_checkTimer += Time.unscaledDeltaTime;
			if (_checkTimer < CheckInterval)
			{
				return;
			}
			QSBPlayerManager.GetSyncObjects<TransformSync.TransformSync>()
				.Where(x => x.HasAuthority).ToList().ForEach(CheckTransformSyncSector);
			_checkTimer = 0;
		}

		private void CheckTransformSyncSector(TransformSync.TransformSync transformSync)
		{
			var syncedTransform = transformSync.SyncedTransform;
			if (syncedTransform == null || syncedTransform.position == Vector3.zero)
			{
				return;
			}
			var closestSector = QSBSectorManager.Instance.GetClosestSector(syncedTransform);
			if (closestSector == transformSync.ReferenceSector)
			{
				return;
			}
			transformSync.SetReferenceSector(closestSector);
			SendSector(transformSync.NetId.Value, closestSector);
		}

		private void SendSector(uint id, QSBSector sector) =>
			GlobalMessenger<uint, QSBSector>.FireEvent(EventNames.QSBSectorChange, id, sector);
	}
}