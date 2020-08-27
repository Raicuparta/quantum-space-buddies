using System;
using System.Runtime.InteropServices;
using QSB.Animation;
using QSB.Utility;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.TransformSync
{
    public class PlayerTransformSync : TransformSync
    {
        public static PlayerTransformSync LocalInstance { get; private set; }

        protected override uint PlayerIdOffset => 0;

        static PlayerTransformSync()
        {
            AnimControllerPatch.Init();
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
        }

        private Transform GetPlayerModel()
        {
            return Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2");
        }

        protected override Transform InitLocalTransform()
        {
            var body = GetPlayerModel();

            GetComponent<AnimationSync>().InitLocal(body);

            return body;
        }

        protected override Transform InitRemoteTransform()
        {
            var body = Instantiate(GetPlayerModel());

            GetComponent<AnimationSync>().InitRemote(body);

            var marker = body.gameObject.AddComponent<PlayerHUDMarker>();
            marker.Init(Player);

            return body;
        }

        public override bool IsReady => Locator.GetPlayerTransform() != null && PlayerRegistry.PlayerExists(PlayerId) && Player.IsReady;
    }
}
