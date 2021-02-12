using QSB.Animation;
using QSB.Instruments;
using QSB.Player;
using UnityEngine;

namespace QSB.TransformSync
{
	public class PlayerTransformSync : TransformSync
	{
		public static PlayerTransformSync LocalInstance { get; private set; }

		static PlayerTransformSync()
		{
			AnimControllerPatch.Init();
		}

		public override void OnStartLocalPlayer() => LocalInstance = this;

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Player.HudMarker?.Remove();
			QSBPlayerManager.RemovePlayer(PlayerId);
		}

		private Transform GetPlayerModel() =>
			Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2");

		protected override Transform InitLocalTransform()
		{
			var body = GetPlayerModel();

			GetComponent<AnimationSync>().InitLocal(body);
			GetComponent<InstrumentsManager>().InitLocal(body);

			Player.Body = body.gameObject;

			return body;
		}

		protected override Transform InitRemoteTransform()
		{
			var body = Instantiate(GetPlayerModel());

			GetComponent<AnimationSync>().InitRemote(body);
			GetComponent<InstrumentsManager>().InitRemote(body);

			var marker = body.gameObject.AddComponent<PlayerHUDMarker>();
			marker.Init(Player);

			body.gameObject.AddComponent<PlayerMapMarker>().PlayerName = Player.Name;

			Player.Body = body.gameObject;

			return body;
		}

		public override bool IsReady => Locator.GetPlayerTransform() != null
			&& Player != null
			&& QSBPlayerManager.PlayerExists(Player.PlayerId)
			&& Player.IsReady
			&& NetId.Value != uint.MaxValue
			&& NetId.Value != 0U;
	}
}