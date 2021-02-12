using OWML.Common;
using QSB.Events;
using QSB.SectorSync;
using QSB.Utility;
using QSB.WorldSync;
using QSB.WorldSync.Events;

namespace QSB.Player.Events
{
	public class PlayerSectorEvent : QSBEvent<WorldObjectMessage>
	{
		public override EventType Type => EventType.PlayerSectorChange;

		public override void SetupListener() => GlobalMessenger<uint, QSBSector>.AddListener(EventNames.QSBSectorChange, Handler);
		public override void CloseListener() => GlobalMessenger<uint, QSBSector>.RemoveListener(EventNames.QSBSectorChange, Handler);

		private void Handler(uint netId, QSBSector sector) => SendEvent(CreateMessage(netId, sector));

		private WorldObjectMessage CreateMessage(uint netId, QSBSector sector) => new WorldObjectMessage
		{
			AboutId = netId,
			ObjectId = sector.ObjectId
		};

		public override void OnReceiveRemote(bool server, WorldObjectMessage message)
		{
			if (!QSBSceneManager.IsInUniverse)
			{
				return;
			}
			var sector = QSBWorldSync.GetWorldObject<QSBSector>(message.ObjectId);

			if (sector == null)
			{
				DebugLog.ToConsole($"Sector with index id {message.ObjectId} not found!", MessageType.Warning);
				return;
			}

			var transformSync = QSBPlayerManager.GetSyncObject<TransformSync.TransformSync>(message.AboutId);

			QSBCore.Helper.Events.Unity.RunWhen(() => transformSync?.SyncedTransform != null,
				() => transformSync?.SetReferenceSector(sector));
		}
	}
}