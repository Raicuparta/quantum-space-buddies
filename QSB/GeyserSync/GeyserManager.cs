using QSB.Patches;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.GeyserSync
{
	public class GeyserManager : MonoBehaviour
	{
		public void Awake()
		{
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
			QSBPatchManager.OnPatchType += OnPatchType;
		}

		public void OnDestroy()
		{
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
			QSBPatchManager.OnPatchType -= OnPatchType;
		}

		private void OnSceneLoaded(OWScene scene, bool isInUniverse)
		{
			var geyserControllers = Resources.FindObjectsOfTypeAll<GeyserController>();
			for (var id = 0; id < geyserControllers.Length; id++)
			{
				var qsbGeyser = QSBWorldSync.GetWorldObject<QSBGeyser>(id) ?? new QSBGeyser();
				qsbGeyser.Init(geyserControllers[id], id);
				QSBWorldSync.AddWorldObject(qsbGeyser);
			}
		}

		public void OnPatchType(QSBPatchTypes type)
		{
			if (type != QSBPatchTypes.OnNonServerClientConnect)
			{
				return;
			}
			QSBCore.Helper.HarmonyHelper.EmptyMethod<GeyserController>("Update");
		}
	}
}