using QSB.Events;
using QSB.WorldSync;
using QuantumUNET;

namespace QSB.GeyserSync
{
	public class QSBGeyser : WorldObject
	{
		private GeyserController _geyserController;

		public void Init(GeyserController geyserController, int id)
		{
			ObjectId = id;
			_geyserController = geyserController;

			geyserController.OnGeyserActivateEvent += () => HandleEvent(true);
			geyserController.OnGeyserDeactivateEvent += () => HandleEvent(false);
		}

		private void HandleEvent(bool state)
		{
			if (QSBNetworkServer.active)
			{
				GlobalMessenger<int, bool>.FireEvent(EventNames.QSBGeyserState, ObjectId, state);
			}
		}

		public void SetState(bool state)
		{
			if (state)
			{
				_geyserController?.ActivateGeyser();
				return;
			}
			_geyserController?.DeactivateGeyser();
		}
	}
}