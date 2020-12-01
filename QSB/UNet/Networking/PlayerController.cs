using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	public class PlayerController
	{
		public PlayerController()
		{
		}

		internal PlayerController(GameObject go, short playerControllerId)
		{
			gameObject = go;
			unetView = go.GetComponent<NetworkIdentity>();
			playerControllerId = playerControllerId;
		}

		public bool IsValid
		{
			get
			{
				return playerControllerId != -1;
			}
		}

		public override string ToString()
		{
			return string.Format("ID={0} NetworkIdentity NetID={1} Player={2}", new object[]
			{
				playerControllerId,
				(!(unetView != null)) ? "null" : unetView.netId.ToString(),
				(!(gameObject != null)) ? "null" : gameObject.name
			});
		}

		internal const short kMaxLocalPlayers = 8;

		public short playerControllerId = -1;

		public NetworkIdentity unetView;

		public GameObject gameObject;

		public const int MaxPlayersPerClient = 32;
	}
}
