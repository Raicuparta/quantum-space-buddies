﻿using QSB.Utility;
using QuantumUNET;

namespace QSB.Player
{
	public abstract class PlayerSyncObject : QNetworkBehaviour
	{
		public uint AttachedNetId => NetIdentity?.NetId.Value ?? uint.MaxValue;
		public uint PlayerId => NetIdentity.RootIdentity?.NetId.Value ?? NetIdentity.NetId.Value;
		public PlayerInfo Player => QSBPlayerManager.GetPlayer(PlayerId);

		protected virtual void Start() => QSBPlayerManager.AddSyncObject(this);
		protected virtual void OnDestroy()
		{
			DebugLog.DebugWrite($"OnDestroy of {GetType().Name} for {PlayerId}");
			QSBPlayerManager.RemoveSyncObject(this);
		}
	}
}