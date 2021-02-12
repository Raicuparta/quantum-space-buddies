using OWML.Common;
using QSB.Animation.Events;
using QSB.ConversationSync.Events;
using QSB.DeathSync.Events;
using QSB.ElevatorSync.Events;
using QSB.GeyserSync.Events;
using QSB.LogSync.Events;
using QSB.OrbSync.Events;
using QSB.Player.Events;
using QSB.TimeSync.Events;
using QSB.Tools.Events;
using QSB.Utility;
using System.Collections.Generic;

namespace QSB.Events
{
	public static class QSBEventManager
	{
		public static bool Ready { get; private set; }

		private static List<IQSBEvent> _eventList = new List<IQSBEvent>();

		public static void Init()
		{
			_eventList = new List<IQSBEvent>
			{
				new PlayerReadyEvent(),
				new PlayerJoinEvent(),
				new PlayerSuitEvent(),
				new PlayerFlashlightEvent(),
				new PlayerSignalscopeEvent(),
				new PlayerTranslatorEvent(),
				new PlayerProbeLauncherEvent(),
				new PlayerProbeEvent(),
				new PlayerSectorEvent(),
				new PlayerDeathEvent(),
				new PlayerStatesRequestEvent(),
				new ElevatorEvent(),
				new GeyserEvent(),
				new ServerTimeEvent(),
				new CrouchEvent(),
				new OrbSlotEvent(),
				new OrbUserEvent(),
				new ConversationEvent(),
				new ConversationStartEndEvent(),
				new ChangeAnimTypeEvent(),
				new ServerSendPlayerStatesEvent(),
				new DialogueConditionEvent(),
				new RevealFactEvent()
			};

			_eventList.ForEach(ev => ev.SetupListener());

			Ready = true;

			DebugLog.DebugWrite("Event Manager ready.", MessageType.Success);
		}

		public static void Reset()
		{
			Ready = false;
			_eventList.ForEach(ev => ev.CloseListener());
			_eventList = new List<IQSBEvent>();
		}
	}
}