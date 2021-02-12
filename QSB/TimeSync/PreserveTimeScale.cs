using OWML.Utils;
using QuantumUNET;

namespace QSB.TimeSync
{
	public class PreserveTimeScale : QSBNetworkBehaviour
	{
		public void Start()
		{
			QSBCore.Helper.Menus.PauseMenu.GetTitleButton("Button-EndCurrentLoop").Hide(); // Remove the meditation button

			// Allow server to sleep at campfires
			if (IsServer)
			{
				return;
			}

			var campfires = FindObjectsOfType<Campfire>();
			foreach (var campfire in campfires)
			{
				campfire.SetValue("_canSleepHere", false); // Stop players from sleeping at campfires
			}
		}
	}
}