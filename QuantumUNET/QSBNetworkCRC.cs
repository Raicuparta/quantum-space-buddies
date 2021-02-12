using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace QuantumUNET
{
	public class QSBNetworkCRC
	{
		internal static QSBNetworkCRC singleton
		{
			get
			{
				if (s_Singleton == null)
				{
					s_Singleton = new QSBNetworkCRC();
				}
				return s_Singleton;
			}
		}

		public Dictionary<string, int> scripts { get; } = new Dictionary<string, int>();

		public static bool scriptCRCCheck
		{
			get => singleton.m_ScriptCRCCheck;
			set => singleton.m_ScriptCRCCheck = value;
		}

		public static void ReinitializeScriptCRCs(Assembly callingAssembly)
		{
			singleton.scripts.Clear();
			foreach (var type in callingAssembly.GetTypes())
			{
				if (type.GetBaseType() == typeof(QSBNetworkBehaviour))
				{
					var method = type.GetMethod(".cctor", BindingFlags.Static);
					method?.Invoke(null, new object[0]);
				}
			}
		}

		public static void RegisterBehaviour(string name, int channel) => singleton.scripts[name] = channel;

		internal static bool Validate(QSBCRCMessageEntry[] scripts, int numChannels) => singleton.ValidateInternal(scripts, numChannels);

		private bool ValidateInternal(QSBCRCMessageEntry[] remoteScripts, int numChannels)
		{
			bool result;
			if (scripts.Count != remoteScripts.Length)
			{
				Debug.LogWarning("Network configuration mismatch detected. The number of networked scripts on the client does not match the number of networked scripts on the server. This could be caused by lazy loading of scripts on the client. This warning can be disabled by the checkbox in NetworkManager Script CRC Check.");
				Dump(remoteScripts);
				result = false;
			}
			else
			{
				foreach (var crcmessageEntry in remoteScripts)
				{
					Debug.Log($"Script: {crcmessageEntry.name} Channel: {crcmessageEntry.channel}");
					if (scripts.ContainsKey(crcmessageEntry.name))
					{
						var num = scripts[crcmessageEntry.name];
						if (num != crcmessageEntry.channel)
						{
							Debug.LogError(
								$"HLAPI CRC Channel Mismatch. Script: {crcmessageEntry.name} LocalChannel: {num} RemoteChannel: {crcmessageEntry.channel}");
							Dump(remoteScripts);
							return false;
						}
					}
					if (crcmessageEntry.channel >= numChannels)
					{
						Debug.LogError(
							$"HLAPI CRC channel out of range! Script: {crcmessageEntry.name} Channel: {crcmessageEntry.channel}");
						Dump(remoteScripts);
						return false;
					}
				}
				result = true;
			}
			return result;
		}

		private void Dump(QSBCRCMessageEntry[] remoteScripts)
		{
			foreach (var text in scripts.Keys)
			{
				Debug.Log($"CRC Local Dump {text} : {scripts[text]}");
			}
			foreach (var crcmessageEntry in remoteScripts)
			{
				Debug.Log($"CRC Remote Dump {crcmessageEntry.name} : {crcmessageEntry.channel}");
			}
		}

		internal static QSBNetworkCRC s_Singleton;
		private bool m_ScriptCRCCheck;
	}
}