using OWML.Common;
using QSB.Utility;
using QSB.WorldSync;
using System.Linq;
using UnityEngine;

namespace QSB.SectorSync
{
	public class QSBSectorManager : MonoBehaviour
	{
		public static QSBSectorManager Instance { get; private set; }

		public bool IsReady { get; private set; }

		private readonly Sector.Name[] _sectorBlacklist =
		{
			Sector.Name.Ship
		};

		public void Awake()
		{
			Instance = this;
			QSBSceneManager.OnUniverseSceneLoaded += (OWScene scene) => RebuildSectors();
			DebugLog.DebugWrite("Sector Manager ready.", MessageType.Success);
		}

		public void OnDestroy() => QSBSceneManager.OnUniverseSceneLoaded -= (OWScene scene) => RebuildSectors();

		public void RebuildSectors()
		{
			DebugLog.DebugWrite("Rebuilding sectors...", MessageType.Warning);
			QSBWorldSync.RemoveWorldObjects<QSBSector>();
			var sectors = Resources.FindObjectsOfTypeAll<Sector>().ToList();
			for (var id = 0; id < sectors.Count; id++)
			{
				var qsbSector = QSBWorldSync.GetWorldObject<QSBSector>(id) ?? new QSBSector();
				qsbSector.Init(sectors[id], id);
				QSBWorldSync.AddWorldObject(qsbSector);
			}
			IsReady = QSBWorldSync.GetWorldObjects<QSBSector>().Any();
		}

		public QSBSector GetClosestSector(Transform trans) // trans rights \o/
		{
			return QSBWorldSync.GetWorldObjects<QSBSector>()
				.Where(sector => sector.Sector != null
					&& !_sectorBlacklist.Contains(sector.Type)
					&& sector.Transform.gameObject.activeInHierarchy)
				.OrderBy(sector => Vector3.Distance(sector.Position, trans.position))
				.First();
		}
	}
}