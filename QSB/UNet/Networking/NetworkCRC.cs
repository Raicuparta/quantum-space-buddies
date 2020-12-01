using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace QSB.UNet.Networking
{
	public class NetworkCRC
	{
		internal static NetworkCRC singleton
		{
			get
			{
				if (NetworkCRC.s_Singleton == null)
				{
					NetworkCRC.s_Singleton = new NetworkCRC();
				}
				return NetworkCRC.s_Singleton;
			}
		}

		public Dictionary<string, int> scripts
		{
			get
			{
				return m_Scripts;
			}
		}

		public static bool scriptCRCCheck
		{
			get
			{
				return NetworkCRC.singleton.m_ScriptCRCCheck;
			}
			set
			{
				NetworkCRC.singleton.m_ScriptCRCCheck = value;
			}
		}

		public static void ReinitializeScriptCRCs(Assembly callingAssembly)
		{
			NetworkCRC.singleton.m_Scripts.Clear();
			foreach (Type type in callingAssembly.GetTypes())
			{
				if (type.GetBaseType() == typeof(NetworkBehaviour))
				{
					MethodInfo method = type.GetMethod(".cctor", BindingFlags.Static);
					if (method != null)
					{
						method.Invoke(null, new object[0]);
					}
				}
			}
		}

		public static void RegisterBehaviour(string name, int channel)
		{
			NetworkCRC.singleton.m_Scripts[name] = channel;
		}

		internal static bool Validate(CRCMessageEntry[] scripts, int numChannels)
		{
			return NetworkCRC.singleton.ValidateInternal(scripts, numChannels);
		}

		private bool ValidateInternal(CRCMessageEntry[] remoteScripts, int numChannels)
		{
			bool result;
			if (m_Scripts.Count != remoteScripts.Length)
			{
				if (LogFilter.logWarn)
				{
					Debug.LogWarning("Network configuration mismatch detected. The number of networked scripts on the client does not match the number of networked scripts on the server. This could be caused by lazy loading of scripts on the client. This warning can be disabled by the checkbox in NetworkManager Script CRC Check.");
				}
				Dump(remoteScripts);
				result = false;
			}
			else
			{
				foreach (CRCMessageEntry crcmessageEntry in remoteScripts)
				{
					if (LogFilter.logDebug)
					{
						Debug.Log(string.Concat(new object[]
						{
							"Script: ",
							crcmessageEntry.name,
							" Channel: ",
							crcmessageEntry.channel
						}));
					}
					if (m_Scripts.ContainsKey(crcmessageEntry.name))
					{
						int num = m_Scripts[crcmessageEntry.name];
						if (num != (int)crcmessageEntry.channel)
						{
							if (LogFilter.logError)
							{
								Debug.LogError(string.Concat(new object[]
								{
									"HLAPI CRC Channel Mismatch. Script: ",
									crcmessageEntry.name,
									" LocalChannel: ",
									num,
									" RemoteChannel: ",
									crcmessageEntry.channel
								}));
							}
							Dump(remoteScripts);
							return false;
						}
					}
					if ((int)crcmessageEntry.channel >= numChannels)
					{
						if (LogFilter.logError)
						{
							Debug.LogError(string.Concat(new object[]
							{
								"HLAPI CRC channel out of range! Script: ",
								crcmessageEntry.name,
								" Channel: ",
								crcmessageEntry.channel
							}));
						}
						Dump(remoteScripts);
						return false;
					}
				}
				result = true;
			}
			return result;
		}

		private void Dump(CRCMessageEntry[] remoteScripts)
		{
			foreach (string text in m_Scripts.Keys)
			{
				Debug.Log(string.Concat(new object[]
				{
					"CRC Local Dump ",
					text,
					" : ",
					m_Scripts[text]
				}));
			}
			foreach (CRCMessageEntry crcmessageEntry in remoteScripts)
			{
				Debug.Log(string.Concat(new object[]
				{
					"CRC Remote Dump ",
					crcmessageEntry.name,
					" : ",
					crcmessageEntry.channel
				}));
			}
		}

		internal static NetworkCRC s_Singleton;

		private Dictionary<string, int> m_Scripts = new Dictionary<string, int>();

		private bool m_ScriptCRCCheck;
	}
}
