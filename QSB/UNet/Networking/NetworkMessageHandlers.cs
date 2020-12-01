using System;
using System.Collections.Generic;
using UnityEngine;

namespace QSB.UNet.Networking
{
	internal class NetworkMessageHandlers
	{
		internal void RegisterHandlerSafe(short msgType, NetworkMessageDelegate handler)
		{
			if (handler == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RegisterHandlerSafe id:" + msgType + " handler is null");
				}
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterHandlerSafe id:",
						msgType,
						" handler:",
						handler.GetMethodName()
					}));
				}
				if (!m_MsgHandlers.ContainsKey(msgType))
				{
					m_MsgHandlers.Add(msgType, handler);
				}
			}
		}

		public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
		{
			if (handler == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RegisterHandler id:" + msgType + " handler is null");
				}
			}
			else if (msgType <= 31)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RegisterHandler: Cannot replace system message handler " + msgType);
				}
			}
			else
			{
				if (m_MsgHandlers.ContainsKey(msgType))
				{
					if (LogFilter.logDebug)
					{
						Debug.Log("RegisterHandler replacing " + msgType);
					}
					m_MsgHandlers.Remove(msgType);
				}
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterHandler id:",
						msgType,
						" handler:",
						handler.GetMethodName()
					}));
				}
				m_MsgHandlers.Add(msgType, handler);
			}
		}

		public void UnregisterHandler(short msgType)
		{
			m_MsgHandlers.Remove(msgType);
		}

		internal NetworkMessageDelegate GetHandler(short msgType)
		{
			NetworkMessageDelegate result;
			if (m_MsgHandlers.ContainsKey(msgType))
			{
				result = m_MsgHandlers[msgType];
			}
			else
			{
				result = null;
			}
			return result;
		}

		internal Dictionary<short, NetworkMessageDelegate> GetHandlers()
		{
			return m_MsgHandlers;
		}

		internal void ClearMessageHandlers()
		{
			m_MsgHandlers.Clear();
		}

		private Dictionary<short, NetworkMessageDelegate> m_MsgHandlers = new Dictionary<short, NetworkMessageDelegate>();
	}
}
