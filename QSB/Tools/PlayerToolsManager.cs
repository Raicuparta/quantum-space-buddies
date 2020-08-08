﻿using System.Linq;
using OWML.ModHelper.Events;
using UnityEngine;

namespace QSB.Tools
{
    public class PlayerToolsManager
    {
        private static Transform _cameraBody;
        private static Transform _toolStowTransform;
        private static Transform _toolHoldTransform;
        private static Material _playerToolsMaterial;
        private static Material _lightbulbMaterial;

        private static readonly Vector3 FlashlightOffset = new Vector3(0.7196316f, -0.2697681f, 0.3769455f);
        private static readonly Vector3 ProbeLauncherOffset = new Vector3(0.5745087f, -0.26f, 0.4453125f);
        private static readonly Vector3 SignalscopeScale = new Vector3(1.5f, 1.5f, 1.5f);
        private static readonly Vector3 TranslatorScale = new Vector3(0.75f, 0.75f, 0.75f);

        public static void Init(Transform camera)
        {
            _cameraBody = camera;
            CreateStowTransforms();

            _playerToolsMaterial = GameObject.Find("PlayerSuit_Jetpack").GetComponent<MeshRenderer>().materials[0];
            _lightbulbMaterial = GameObject.Find("Props_HEA_Lantern (10)/Lantern_Lamp").GetComponent<MeshRenderer>().materials[0];

            CreateFlashlight();
            CreateSignalscope();
            CreateProbeLauncher();

            QSB.Helper.Events.Subscribe<NomaiTranslatorProp>(OWML.Common.Events.AfterStart);
            QSB.Helper.Events.OnEvent += OnEvent;
        }

        private static void OnEvent(MonoBehaviour behaviour, OWML.Common.Events ev)
        {
            if (behaviour is NomaiTranslatorProp translatorProp && ev == OWML.Common.Events.AfterStart)
            {
                CreateTranslator(translatorProp);
            }
        }

        public static void CreateProbe(Transform body, PlayerInfo player)
        {
            var newProbe = body.gameObject.AddComponent<QSBProbe>();
            newProbe.Init(player);

            player.Probe = newProbe;
        }

        private static void CreateStowTransforms()
        {
            var stow = new GameObject("ToolStowTransform");
            _toolStowTransform = stow.transform;
            stow.transform.parent = _cameraBody;
            stow.transform.localPosition = Vector3.zero;
            stow.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

            var hold = new GameObject("ToolHoldTransform");
            _toolHoldTransform = hold.transform;
            hold.transform.parent = _cameraBody;
            hold.transform.localPosition = Vector3.zero;
            hold.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }

        private static void CreateFlashlight()
        {
            var flashlightRoot = Object.Instantiate(GameObject.Find("FlashlightRoot"));
            flashlightRoot.SetActive(false);
            var oldComponent = flashlightRoot.GetComponent<Flashlight>();
            var component = flashlightRoot.AddComponent<QSBFlashlight>();

            component.Init(oldComponent);
            oldComponent.enabled = false;

            flashlightRoot.transform.parent = _cameraBody;
            flashlightRoot.transform.localPosition = FlashlightOffset;
            flashlightRoot.SetActive(true);
        }

        private static void CreateSignalscope()
        {
            var signalscopeRoot = Object.Instantiate(GameObject.Find("Signalscope"));
            signalscopeRoot.SetActive(false);

            Object.Destroy(signalscopeRoot.GetComponent<SignalscopePromptController>());
            Object.Destroy(signalscopeRoot.transform.Find("Props_HEA_Signalscope")
                .Find("Props_HEA_Signalscope_Prepass").gameObject);

            var oldSignalscope = signalscopeRoot.GetComponent<Signalscope>();
            var tool = signalscopeRoot.AddComponent<QSBTool>();
            tool.MoveSpring = oldSignalscope.GetValue<DampedSpringQuat>("_moveSpring");
            tool.StowTransform = _toolStowTransform;
            tool.HoldTransform = _toolHoldTransform;
            tool.ArrivalDegrees = 5f;
            tool.Type = ToolType.Signalscope;
            tool.ToolGameObject = signalscopeRoot.transform.Find("Props_HEA_Signalscope").gameObject;
            oldSignalscope.enabled = false;

            GetRenderer(signalscopeRoot, "Props_HEA_Signalscope").material = _playerToolsMaterial;

            signalscopeRoot.transform.parent = _cameraBody;
            signalscopeRoot.transform.localPosition = Vector3.zero;
            signalscopeRoot.transform.localScale = SignalscopeScale;
            signalscopeRoot.SetActive(true);
        }

        private static void CreateTranslator(NomaiTranslatorProp translatorProp)
        {
            var translatorRoot = Object.Instantiate(translatorProp.gameObject);
            translatorRoot.SetActive(false);

            var group = translatorRoot.transform.Find("TranslatorGroup");
            var model = group.Find("Props_HEA_Translator");

            Object.Destroy(translatorRoot.GetComponent<NomaiTranslatorProp>());
            Object.Destroy(group.Find("Canvas").gameObject);
            Object.Destroy(group.Find("Lighting").gameObject);
            Object.Destroy(group.Find("TranslatorBeams").gameObject);
            Object.Destroy(model.Find("Props_HEA_Translator_Pivot_RotatingPart")
                .Find("Props_HEA_Translator_RotatingPart")
                .Find("Props_HEA_Translator_RotatingPart_Prepass").gameObject);
            Object.Destroy(model.Find("Props_HEA_Translator_Prepass").gameObject);

            var oldTranslator = translatorRoot.GetComponent<NomaiTranslator>();
            var tool = translatorRoot.AddComponent<QSBTool>();
            tool.MoveSpring = oldTranslator.GetValue<DampedSpringQuat>("_moveSpring");
            tool.StowTransform = _toolStowTransform;
            tool.HoldTransform = _toolHoldTransform;
            tool.ArrivalDegrees = 5f;
            tool.Type = ToolType.Translator;
            tool.ToolGameObject = group.gameObject;
            oldTranslator.enabled = false;

            GetRenderer(translatorRoot, "Props_HEA_Translator_Geo").material = _playerToolsMaterial;
            GetRenderer(translatorRoot, "Props_HEA_Translator_RotatingPart").material = _playerToolsMaterial;
            GetRenderer(translatorRoot, "Props_HEA_Translator_Button_L").material = _lightbulbMaterial;
            GetRenderer(translatorRoot, "Props_HEA_Translator_Button_R").material = _lightbulbMaterial;

            translatorRoot.transform.parent = _cameraBody;
            translatorRoot.transform.localPosition = Vector3.zero;
            translatorRoot.transform.localScale = TranslatorScale;
            translatorRoot.SetActive(true);
        }

        private static void CreateProbeLauncher()
        {
            var launcherRoot = Object.Instantiate(GameObject.Find("PlayerCamera/ProbeLauncher"));
            launcherRoot.SetActive(false);

            var launcher = launcherRoot.transform.Find("Props_HEA_ProbeLauncher");

            Object.Destroy(launcherRoot.GetComponent<ProbePromptController>());
            Object.Destroy(launcherRoot.GetComponent<ProbeLauncherEffects>());
            Object.Destroy(launcherRoot.transform.Find("Props_HEA_ProbeLauncher_ProbeCamera").gameObject);
            Object.Destroy(launcherRoot.transform.Find("preLaunchCamera").gameObject);
            Object.Destroy(launcherRoot.transform.Find("LaunchParticleEffect_Underwater").gameObject);
            Object.Destroy(launcherRoot.transform.Find("LaunchParticleEffect").gameObject);
            Object.Destroy(launcher.Find("Props_HEA_ProbeLauncher_Prepass").gameObject);
            Object.Destroy(launcher.Find("Props_HEA_Probe_Prelaunch").Find("Props_HEA_Probe_Prelaunch_Prepass").gameObject);

            var oldLauncher = launcherRoot.GetComponent<PlayerProbeLauncher>();
            var tool = launcherRoot.AddComponent<QSBTool>();
            tool.MoveSpring = oldLauncher.GetValue<DampedSpringQuat>("_moveSpring");
            tool.StowTransform = _toolStowTransform;
            tool.HoldTransform = _toolHoldTransform;
            tool.ArrivalDegrees = 5f;
            tool.Type = ToolType.ProbeLauncher;
            tool.ToolGameObject = launcher.gameObject;
            oldLauncher.enabled = false;

            GetRenderer(launcherRoot, "PressureGauge_Arrow").material = _playerToolsMaterial;
            GetRenderer(launcherRoot, "ProbeLauncherChassis").material = _playerToolsMaterial;

            launcherRoot.transform.parent = _cameraBody;
            launcherRoot.transform.localPosition = ProbeLauncherOffset;
            launcherRoot.SetActive(true);
        }

        private static MeshRenderer GetRenderer(GameObject root, string gameobjectName)
        {
            return root.GetComponentsInChildren<MeshRenderer>(true).First(x => x.name == gameobjectName);
        }
    }
}