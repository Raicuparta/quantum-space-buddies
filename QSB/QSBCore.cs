using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using QSB.ConversationSync;
using QSB.ElevatorSync;
using QSB.GeyserSync;
using QSB.OrbSync;
using QSB.Patches;
using QSB.SectorSync;
using QSB.TimeSync;
using QSB.Utility;
using QuantumUNET;
using QuantumUNET.Components;
using UnityEngine;

/*
	Copyright (C) 2020 
			Henry Pointer (_nebula / misternebula), 
			Aleksander Waage (AmazingAlek), 
			Ricardo Lopes (Raicuparta)
	
	This program is free software: you can redistribute it and/or
	modify it under the terms of the GNU Affero General Public License
	as published by the Free Software Foundation, either version 3 of
	the License, or (at your option) any later version.

	This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
	without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
	See the GNU Affero General Public License for more details.

	You should have received a copy of the GNU Affero General Public License along with this program. If not, see https://www.gnu.org/licenses/.
*/

namespace QSB
{
	public class QSBCore : ModBehaviour
	{
		public static IModHelper Helper { get; private set; }
		public static string DefaultServerIP { get; private set; }
		public static int Port { get; private set; }
		public static bool DebugMode { get; private set; }
		public static AssetBundle NetworkAssetBundle { get; private set; }
		public static AssetBundle InstrumentAssetBundle { get; private set; }
		public static bool HasWokenUp { get; set; }
		public static bool IsServer => QSBNetworkServer.active;

		public void Awake()
		{
			Application.runInBackground = true;

			var instance = TextTranslation.Get().GetValue<TextTranslation.TranslationTable>("m_table");
			instance.theUITable[(int)UITextType.PleaseUseController] =
				"<color=orange>Quantum Space Buddies</color> is best experienced with friends...";
		}

		public void Start()
		{
			Helper = ModHelper;
			DebugLog.ToConsole($"* Start of QSB version {Helper.Manifest.Version} - authored by {Helper.Manifest.Author}", MessageType.Info);

			NetworkAssetBundle = Helper.Assets.LoadBundle("assets/network");
			InstrumentAssetBundle = Helper.Assets.LoadBundle("assets/instruments");

			QSBPatchManager.Init();
			QSBPatchManager.DoPatchType(QSBPatchTypes.OnModStart);

			gameObject.AddComponent<QSBNetworkManager>();
			gameObject.AddComponent<QSBNetworkManagerHUD>();
			gameObject.AddComponent<DebugActions>();
			gameObject.AddComponent<ElevatorManager>();
			gameObject.AddComponent<GeyserManager>();
			gameObject.AddComponent<OrbManager>();
			gameObject.AddComponent<QSBSectorManager>();
			gameObject.AddComponent<ConversationManager>();
			gameObject.AddComponent<QSBInputManager>();
			gameObject.AddComponent<TimeSyncUI>();

			// Stop players being able to pause
			Helper.HarmonyHelper.EmptyMethod(typeof(OWTime).GetMethod("Pause"));
		}

		public void Update() =>
			QSBNetworkIdentity.UNetStaticUpdate();

		public override void Configure(IModConfig config)
		{
			DefaultServerIP = config.GetSettingsValue<string>("defaultServerIP");
			Port = config.GetSettingsValue<int>("port");
			if (QSBNetworkManager.Instance != null)
			{
				QSBNetworkManager.Instance.networkPort = Port;
			}
			DebugMode = config.GetSettingsValue<bool>("debugMode");
		}
	}
}