using QSB.Events;
using QSB.Messaging;
using QSB.SectorSync;
using QSB.Utility;
using QSB.WorldSync;
using System.Linq;

namespace QSB.Player.Events
{
	public class PlayerStatesRequestEvent : QSBEvent<PlayerMessage>
	{
		public override EventType Type => EventType.PlayerStatesRequest;

		public override void SetupListener() => GlobalMessenger.AddListener(EventNames.QSBPlayerStatesRequest, Handler);
		public override void CloseListener() => GlobalMessenger.RemoveListener(EventNames.QSBPlayerStatesRequest, Handler);

		private void Handler() => SendEvent(CreateMessage());

		private PlayerMessage CreateMessage() => new PlayerMessage
		{
			AboutId = LocalPlayerId,
			OnlySendToServer = true
		};

		public override void OnReceiveRemote(bool server, PlayerMessage message)
		{
			DebugLog.DebugWrite($"Get state request from {message.FromId} - isServer?{server}");
			GlobalMessenger.FireEvent(EventNames.QSBServerSendPlayerStates);
			foreach (var item in QSBPlayerManager.GetSyncObjects<TransformSync.TransformSync>()
				.Where(x => x != null && x.IsReady && x.ReferenceSector != null))
			{
				GlobalMessenger<uint, QSBSector>.FireEvent(EventNames.QSBSectorChange, item.NetId.Value, item.ReferenceSector);
			}

			if (!server)
			{
				return;
			}

			foreach (var condition in QSBWorldSync.DialogueConditions)
			{
				GlobalMessenger<string, bool>.FireEvent(EventNames.DialogueCondition, condition.Key, condition.Value);
			}

			foreach (var fact in QSBWorldSync.ShipLogFacts)
			{
				GlobalMessenger<string, bool, bool>.FireEvent(EventNames.QSBRevealFact, fact.Id, fact.SaveGame, false);
			}
		}
	}
}