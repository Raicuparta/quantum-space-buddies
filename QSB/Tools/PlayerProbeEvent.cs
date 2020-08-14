using QSB.Events;
using QSB.Messaging;

namespace QSB.Tools
{
    public class PlayerProbeEvent : QSBEvent<ProbeMessage>
    {
        public override MessageType Type => MessageType.ProbeActiveChange;

        public override void SetupListener()
        {
            GlobalMessenger<SurveyorProbe>.AddListener(EventNames.LaunchProbe, probe => SendEvent(CreateMessage(ProbeData.Launch)));
            GlobalMessenger<SurveyorProbe>.AddListener(EventNames.RetrieveProbe, probe => SendEvent(CreateMessage(ProbeData.Retrieve)));
        }

        private ProbeMessage CreateMessage(ProbeData value) => new ProbeMessage
        {
            SenderId = LocalPlayerId,
            Value = value
        };

        public override void OnReceiveRemote(ProbeMessage message)
        {
            var player = PlayerRegistry.GetPlayer(message.SenderId);
            switch (message.Value)
            {
                case ProbeData.Launch:
                    player.UpdateState(State.ProbeActive, true);
                    player.Probe.SetState(true);
                    break;
                case ProbeData.Retrieve:
                    player.UpdateState(State.ProbeActive, false);
                    player.Probe.SetState(false);
                    break;
                case ProbeData.Anchor:
                    break;
                case ProbeData.Warp:
                    break;
            }
        }

        public override void OnReceiveLocal(ProbeMessage message)
        {
            switch (message.Value)
            {
                case ProbeData.Launch:
                    PlayerRegistry.LocalPlayer.UpdateState(State.ProbeActive, true);
                    break;
                case ProbeData.Retrieve:
                    PlayerRegistry.LocalPlayer.UpdateState(State.ProbeActive, false);
                    break;
            }
        }
    }

    public enum ProbeData
    {
        Launch,
        Retrieve,
        Anchor,
        Warp
    }
}
