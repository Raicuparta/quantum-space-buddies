using QSB.Events;
using QuantumUNET;
using QuantumUNET.Components;
using QuantumUNET.Messages;
using System;
using System.Linq;

namespace QSB.Messaging
{
	public class MessageHandler<T> where T : QSBMessageBase, new()
	{
		public event Action<T> OnClientReceiveMessage;
		public event Action<T> OnServerReceiveMessage;

		private readonly short _eventType;

		public MessageHandler(EventType eventType)
		{
			_eventType = (short)(eventType + QSBMsgType.Highest + 1);
			if (QSBNetworkManager.Instance.IsReady)
			{
				Init();
			}
			else
			{
				QSBNetworkManager.Instance.OnNetworkManagerReady += Init;
			}
		}

		private void Init()
		{
			if (QSBNetworkServer.handlers.Keys.Contains(_eventType))
			{
				QSBNetworkServer.handlers.Remove(_eventType);
				QSBNetworkManagerUNET.singleton.client.handlers.Remove(_eventType);
			}
			QSBNetworkServer.RegisterHandler(_eventType, OnServerReceiveMessageHandler);
			QSBNetworkManagerUNET.singleton.client.RegisterHandler(_eventType, OnClientReceiveMessageHandler);
		}

		public void SendToAll(T message)
		{
			if (!QSBNetworkManager.Instance.IsReady)
			{
				return;
			}
			QSBNetworkServer.SendToAll(_eventType, message);
		}

		public void SendToServer(T message)
		{
			if (!QSBNetworkManager.Instance.IsReady)
			{
				return;
			}
			QSBNetworkManagerUNET.singleton.client.Send(_eventType, message);
		}

		private void OnClientReceiveMessageHandler(QSBNetworkMessage netMsg)
		{
			var message = netMsg.ReadMessage<T>();
			OnClientReceiveMessage?.Invoke(message);
		}

		private void OnServerReceiveMessageHandler(QSBNetworkMessage netMsg)
		{
			var message = netMsg.ReadMessage<T>();
			OnServerReceiveMessage?.Invoke(message);
		}
	}
}