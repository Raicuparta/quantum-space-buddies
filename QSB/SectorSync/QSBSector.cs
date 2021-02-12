using QSB.WorldSync;
using UnityEngine;

namespace QSB.SectorSync
{
	public class QSBSector : WorldObject
	{
		public Sector Sector { get; private set; }
		public Sector.Name Type => Sector.GetName();
		public string Name => Sector.name;
		public Transform Transform => Sector.transform;
		public Vector3 Position => Transform.position;

		public void Init(Sector sector, int id)
		{
			Sector = sector;
			ObjectId = id;
		}
	}
}