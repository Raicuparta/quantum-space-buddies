using System;

namespace QSB.UNet.Networking
{
	public class MsgType
	{
		public static string MsgTypeToString(short value)
		{
			string result;
			if (value < 0 || value > 47)
			{
				result = string.Empty;
			}
			else
			{
				string text = MsgType.msgLabels[(int)value];
				if (string.IsNullOrEmpty(text))
				{
					text = "[" + value + "]";
				}
				result = text;
			}
			return result;
		}

		public const short ObjectDestroy = 1;

		public const short Rpc = 2;

		public const short ObjectSpawn = 3;

		public const short Owner = 4;

		public const short Command = 5;

		public const short LocalPlayerTransform = 6;

		public const short SyncEvent = 7;

		public const short UpdateVars = 8;

		public const short SyncList = 9;

		public const short ObjectSpawnScene = 10;

		public const short NetworkInfo = 11;

		public const short SpawnFinished = 12;

		public const short ObjectHide = 13;

		public const short CRC = 14;

		public const short LocalClientAuthority = 15;

		public const short LocalChildTransform = 16;

		public const short Fragment = 17;

		public const short PeerClientAuthority = 18;

		internal const short UserMessage = 0;

		internal const short HLAPIMsg = 28;

		internal const short LLAPIMsg = 29;

		internal const short HLAPIResend = 30;

		internal const short HLAPIPending = 31;

		public const short InternalHighest = 31;

		public const short Connect = 32;

		public const short Disconnect = 33;

		public const short Error = 34;

		public const short Ready = 35;

		public const short NotReady = 36;

		public const short AddPlayer = 37;

		public const short RemovePlayer = 38;

		public const short Scene = 39;

		public const short Animation = 40;

		public const short AnimationParameters = 41;

		public const short AnimationTrigger = 42;

		public const short LobbyReadyToBegin = 43;

		public const short LobbySceneLoaded = 44;

		public const short LobbyAddPlayerFailed = 45;

		public const short LobbyReturnToLobby = 46;

		public const short ReconnectPlayer = 47;

		public const short Highest = 47;

		internal static string[] msgLabels = new string[]
		{
			"none",
			"ObjectDestroy",
			"Rpc",
			"ObjectSpawn",
			"Owner",
			"Command",
			"LocalPlayerTransform",
			"SyncEvent",
			"UpdateVars",
			"SyncList",
			"ObjectSpawnScene",
			"NetworkInfo",
			"SpawnFinished",
			"ObjectHide",
			"CRC",
			"LocalClientAuthority",
			"LocalChildTransform",
			"Fragment",
			"PeerClientAuthority",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"Connect",
			"Disconnect",
			"Error",
			"Ready",
			"NotReady",
			"AddPlayer",
			"RemovePlayer",
			"Scene",
			"Animation",
			"AnimationParams",
			"AnimationTrigger",
			"LobbyReadyToBegin",
			"LobbySceneLoaded",
			"LobbyAddPlayerFailed",
			"LobbyReturnToLobby",
			"ReconnectPlayer"
		};
	}
}
