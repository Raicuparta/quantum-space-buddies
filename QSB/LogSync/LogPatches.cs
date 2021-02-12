using QSB.Events;
using QSB.Patches;

namespace QSB.LogSync
{
	public class LogPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		public static void RevealFact(string id, bool saveGame, bool showNotification, bool __result)
		{
			if (!__result)
			{
				return;
			}
			GlobalMessenger<string, bool, bool>.FireEvent(EventNames.QSBRevealFact, id, saveGame, showNotification);
		}

		public override void DoPatches() => QSBCore.Helper.HarmonyHelper.AddPostfix<ShipLogManager>("RevealFact", typeof(LogPatches), nameof(RevealFact));
	}
}