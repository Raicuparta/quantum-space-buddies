using QSB.Messaging;
using QSB.Player;
using QSB.TransformSync;
using QSB.UNet.Networking;

namespace QSB.EventsCore
{
    public abstract class QSBEvent<T> : IQSBEvent where T : PlayerMessage, new()
    {
        public abstract EventType Type { get; }
        public uint LocalPlayerId => QSBPlayerManager.LocalPlayerId;
        private readonly MessageHandler<T> _eventHandler;

        protected QSBEvent()
        {
            _eventHandler = new MessageHandler<T>(Type);
            _eventHandler.OnClientReceiveMessage += OnClientReceive;
            _eventHandler.OnServerReceiveMessage += OnServerReceive;
        }

        public abstract void SetupListener();

        public abstract void CloseListener();

        public virtual void OnReceiveRemote(T message)
        {
        }

        public virtual void OnReceiveLocal(T message)
        {
        }

        public virtual void OnServerReceive(T message)
        {
            _eventHandler.SendToAll(message);
        }

        public void SendEvent(T message)
        {
            message.FromId = QSBPlayerManager.LocalPlayerId;
            QSB.Helper.Events.Unity.RunWhen(() => PlayerTransformSync.LocalInstance != null, () => Send(message));
        }

        private void Send(T message)
        {
            if (NetworkServer.active)
            {
                _eventHandler.SendToAll(message);
            }
            else
            {
                _eventHandler.SendToServer(message);
            }
        }

        private void OnClientReceive(T message)
        {
            if (message.FromId == QSBPlayerManager.LocalPlayerId ||
                QSBPlayerManager.IsBelongingToLocalPlayer(message.AboutId))
            {
                OnReceiveLocal(message);
                return;
            }

            OnReceiveRemote(message);
        }
    }
}
