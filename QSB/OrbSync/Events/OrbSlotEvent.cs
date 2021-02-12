using QSB.Events;
using QSB.WorldSync;

namespace QSB.OrbSync.Events
{
	public class OrbSlotEvent : QSBEvent<OrbSlotMessage>
	{
		public override EventType Type => EventType.OrbSlot;

		public override void SetupListener() => GlobalMessenger<int, int, bool>.AddListener(EventNames.QSBOrbSlot, Handler);
		public override void CloseListener() => GlobalMessenger<int, int, bool>.RemoveListener(EventNames.QSBOrbSlot, Handler);

		private void Handler(int slotId, int orbId, bool slotState) => SendEvent(CreateMessage(slotId, orbId, slotState));

		private OrbSlotMessage CreateMessage(int slotId, int orbId, bool slotState) => new OrbSlotMessage
		{
			AboutId = LocalPlayerId,
			SlotId = slotId,
			OrbId = orbId,
			SlotState = slotState
		};

		public override void OnReceiveRemote(bool server, OrbSlotMessage message)
		{
			var orbSlot = QSBWorldSync.GetWorldObject<QSBOrbSlot>(message.SlotId);
			orbSlot?.SetState(message.SlotState, message.OrbId);
		}
	}
}