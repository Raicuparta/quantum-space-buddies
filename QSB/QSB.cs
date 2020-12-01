using OWML.Common;
using OWML.ModHelper;
using OWML.ModHelper.Events;
using QSB.ConversationSync;
using QSB.ElevatorSync;
using QSB.GeyserSync;
using QSB.OrbSync;
using QSB.Patches;
using QSB.SectorSync;
using QSB.TimeSync;
using QSB.UNet.Networking;
using QSB.Utility;
using UnityEngine;

namespace QSB
{
    public class QSB : ModBehaviour
    {
        public static IModBehaviour ModBehaviour { get; private set; }
        public static IModHelper Helper { get; private set; }
        public static string DefaultServerIP { get; private set; }
        public static int Port { get; private set; }
        public static bool DebugMode { get; private set; }
        public static AssetBundle NetworkAssetBundle { get; private set; }
        public static AssetBundle InstrumentAssetBundle { get; private set; }
        public static bool HasWokenUp { get; set; }

        private void Awake()
        {
            Application.runInBackground = true;

            var instance = TextTranslation.Get().GetValue<TextTranslation.TranslationTable>("m_table");
            instance.theUITable[(int)UITextType.PleaseUseController] =
                "<color=orange>Quantum Space Buddies</color> is best experienced with friends...";

            ModBehaviour = this;
        }

        private void Start()
        {
            Helper = ModHelper;
            DebugLog.ToConsole($"* Start of QSB version {Helper.Manifest.Version} - authored by {Helper.Manifest.Author}", MessageType.Info);

            NetworkAssetBundle = Helper.Assets.LoadBundle("assets/network");
            InstrumentAssetBundle = Helper.Assets.LoadBundle("assets/instruments");

            QSBPatchManager.Init();

            QSBPatchManager.DoPatchType(QSBPatchTypes.OnModStart);

            // Turns out these are very finicky about what order they go. QSBNetworkManager seems to 
            // want to go first-ish, otherwise the NetworkManager complains about the PlayerPrefab being 
            // null (even though it isn't...)
            gameObject.AddComponent<QSBNetworkManager>();
            gameObject.AddComponent<NetworkManagerHUD>();
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
