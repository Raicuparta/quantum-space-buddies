using QSB.Events;
using QSB.Messaging;
using QSB.Player;
using QSB.Utility;

namespace QSB.DeathSync.Events
{
	public class PlayerDeathEvent : QSBEvent<EnumMessage<DeathType>>
	{
		public override EventType Type => EventType.PlayerDeath;

		public override void SetupListener() => GlobalMessenger<DeathType>.AddListener(EventNames.QSBPlayerDeath, Handler);
		public override void CloseListener() => GlobalMessenger<DeathType>.RemoveListener(EventNames.QSBPlayerDeath, Handler);

		private void Handler(DeathType type) => SendEvent(CreateMessage(type));

		private EnumMessage<DeathType> CreateMessage(DeathType type) => new EnumMessage<DeathType>
		{
			AboutId = LocalPlayerId,
			Value = type
		};

		public override void OnReceiveRemote(bool server, EnumMessage<DeathType> message)
		{
			var playerName = QSBPlayerManager.GetPlayer(message.AboutId).Name;
			var deathMessage = Necronomicon.GetPhrase(message.Value);
			DebugLog.ToAll(string.Format(deathMessage, playerName));
		}
	}
}