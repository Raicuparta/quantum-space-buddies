using QSB.Events;
using QSB.WorldSync;
using QSB.WorldSync.Events;

namespace QSB.ElevatorSync.Events
{
	public class ElevatorEvent : QSBEvent<BoolWorldObjectMessage>
	{
		public override EventType Type => EventType.Elevator;

		public override void SetupListener() => GlobalMessenger<int, bool>.AddListener(EventNames.QSBStartLift, Handler);
		public override void CloseListener() => GlobalMessenger<int, bool>.RemoveListener(EventNames.QSBStartLift, Handler);

		private void Handler(int id, bool isGoingUp) => SendEvent(CreateMessage(id, isGoingUp));

		private BoolWorldObjectMessage CreateMessage(int id, bool isGoingUp) => new BoolWorldObjectMessage
		{
			State = isGoingUp,
			ObjectId = id
		};

		public override void OnReceiveRemote(bool server, BoolWorldObjectMessage message)
		{
			var elevator = QSBWorldSync.GetWorldObject<QSBElevator>(message.ObjectId);
			elevator?.RemoteCall(message.State);
		}
	}
}