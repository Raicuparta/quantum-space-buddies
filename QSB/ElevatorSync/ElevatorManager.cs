using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.ElevatorSync
{
	public class ElevatorManager : MonoBehaviour
	{
		public static ElevatorManager Instance { get; private set; }

		private List<Elevator> _elevators;

		public void Awake()
		{
			Instance = this;
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
		}

		public void OnDestroy() => QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;

		private void OnSceneLoaded(OWScene scene, bool isInUniverse)
		{
			_elevators = Resources.FindObjectsOfTypeAll<Elevator>().ToList();
			for (var id = 0; id < _elevators.Count; id++)
			{
				var qsbElevator = QSBWorldSync.GetWorldObject<QSBElevator>(id) ?? new QSBElevator();
				qsbElevator.Init(_elevators[id], id);
				QSBWorldSync.AddWorldObject(qsbElevator);
			}
		}

		public int GetId(Elevator elevator) => _elevators.IndexOf(elevator);
	}
}