using OWML.Utils;
using QSB.Events;
using QSB.Patches;

namespace QSB.ElevatorSync
{
	public class ElevatorPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnModStart;

		public static void StartLift(Elevator __instance)
		{
			var isGoingUp = __instance.GetValue<bool>("_goingToTheEnd");
			var id = ElevatorManager.Instance.GetId(__instance);
			GlobalMessenger<int, bool>.FireEvent(EventNames.QSBStartLift, id, isGoingUp);
		}

		public override void DoPatches() => QSBCore.Helper.HarmonyHelper.AddPostfix<Elevator>("StartLift", typeof(ElevatorPatches), nameof(StartLift));
	}
}