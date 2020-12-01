using QSB.DeathSync;
using QSB.EventsCore;
using QSB.TimeSync.Events;
using QSB.UNet.Networking;
using QSB.Utility;
using UnityEngine;

namespace QSB.TimeSync
{
    public class WakeUpSync : NetworkBehaviour
    {
        public static WakeUpSync LocalInstance { get; private set; }

        private const float TimeThreshold = 0.5f;
        private const float MaxFastForwardSpeed = 60f;
        private const float MaxFastForwardDiff = 20f;
        private const float MinFastForwardSpeed = 2f;

        private enum State { NotLoaded, Loaded, FastForwarding, Pausing }
        private State _state = State.NotLoaded;

        private float _sendTimer;
        private float _serverTime;
        private float _timeScale;
        private bool _isInputEnabled = true;
        private bool _isFirstFastForward = true;
        private int _localLoopCount;
        private int _serverLoopCount;

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
        }

        private void Start()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (QSBSceneManager.IsInUniverse)
            {
                Init();
            }
            QSBSceneManager.OnSceneLoaded += OnSceneLoaded;

            GlobalMessenger.AddListener(EventNames.RestartTimeLoop, OnLoopStart);
            GlobalMessenger.AddListener(EventNames.WakeUp, OnWakeUp);
        }

        private void OnWakeUp()
        {
            DebugLog.DebugWrite("ON WAKE UP!");
            if (NetworkServer.active)
            {
                QSB.HasWokenUp = true;
            }
        }

        private void OnDestroy()
        {
            QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
            GlobalMessenger.RemoveListener(EventNames.RestartTimeLoop, OnLoopStart);
            GlobalMessenger.RemoveListener(EventNames.WakeUp, OnWakeUp);
        }

        private void OnSceneLoaded(OWScene scene, bool isInUniverse)
        {
            QSB.HasWokenUp = (scene == OWScene.EyeOfTheUniverse);
            if (isInUniverse)
            {
                Init();
            }
            else
            {
                _state = State.NotLoaded;
            }
        }

        private void OnLoopStart()
        {
            _localLoopCount++;
        }

        private void Init()
        {
            GlobalMessenger.FireEvent(EventNames.QSBPlayerStatesRequest);
            _state = State.Loaded;
            gameObject.AddComponent<PreserveTimeScale>();
            if (isServer)
            {
                SendServerTime();
            }
            else
            {
                WakeUpOrSleep();
            }
        }

        private void SendServerTime()
        {
            GlobalMessenger<float, int>.FireEvent(EventNames.QSBServerTime, Time.timeSinceLevelLoad, _localLoopCount);
        }

        public void OnClientReceiveMessage(ServerTimeMessage message)
        {
            _serverTime = message.ServerTime;
            _serverLoopCount = message.LoopCount;
            WakeUpOrSleep();
        }

        private void WakeUpOrSleep()
        {
            if (_state == State.NotLoaded || _localLoopCount != _serverLoopCount)
            {
                return;
            }

            var myTime = Time.timeSinceLevelLoad;
            var diff = myTime - _serverTime;

            if (diff > TimeThreshold)
            {
                StartPausing();
                return;
            }

            if (diff < -TimeThreshold)
            {
                StartFastForwarding();
            }
        }

        private void StartFastForwarding()
        {
            if (_state == State.FastForwarding)
            {
                return;
            }
            _timeScale = MaxFastForwardSpeed;
            _state = State.FastForwarding;
            TimeSyncUI.Start(TimeSyncType.Fastforwarding);
        }

        private void StartPausing()
        {
            if (_state == State.Pausing)
            {
                return;
            }
            _timeScale = 0f;
            _state = State.Pausing;
            SpinnerUI.Show();
            TimeSyncUI.Start(TimeSyncType.Pausing);
        }

        private void ResetTimeScale()
        {
            _timeScale = 1f;
            _state = State.Loaded;

            if (!_isInputEnabled)
            {
                EnableInput();
            }
            _isFirstFastForward = false;
            QSB.HasWokenUp = true;
            Physics.SyncTransforms();
            SpinnerUI.Hide();
            TimeSyncUI.Stop();
            GlobalMessenger.FireEvent(EventNames.QSBPlayerStatesRequest);
            RespawnOnDeath.Instance.Init();
        }

        private void DisableInput()
        {
            _isInputEnabled = false;
            OWInput.ChangeInputMode(InputMode.None);
        }

        private void EnableInput()
        {
            _isInputEnabled = true;
            OWInput.ChangeInputMode(InputMode.Character);
        }

        private void Update()
        {
            if (isServer)
            {
                UpdateServer();
            }
            else if (isLocalPlayer)
            {
                UpdateLocal();
            }
        }

        private void UpdateServer()
        {
            if (_state != State.Loaded)
            {
                return;
            }

            _sendTimer += Time.unscaledDeltaTime;
            if (_sendTimer > 1)
            {
                SendServerTime();
                _sendTimer = 0;
            }
        }

        private void UpdateLocal()
        {
            _serverTime += Time.unscaledDeltaTime;

            if (_state == State.NotLoaded)
            {
                return;
            }

            if (_state == State.FastForwarding)
            {
                var diff = _serverTime - Time.timeSinceLevelLoad;
                Time.timeScale = Mathf.Lerp(MinFastForwardSpeed, MaxFastForwardSpeed, Mathf.Abs(diff) / MaxFastForwardDiff);

                if (QSBSceneManager.CurrentScene == OWScene.SolarSystem && _isFirstFastForward)
                {
                    var spawnPoint = Locator.GetPlayerBody().GetComponent<PlayerSpawner>().GetInitialSpawnPoint().transform;
                    Locator.GetPlayerTransform().position = spawnPoint.position;
                    Locator.GetPlayerTransform().rotation = spawnPoint.rotation;
                    Physics.SyncTransforms();
                }
            }
            else
            {
                Time.timeScale = _timeScale;
            }

            var isDoneFastForwarding = _state == State.FastForwarding && Time.timeSinceLevelLoad >= _serverTime;
            var isDonePausing = _state == State.Pausing && Time.timeSinceLevelLoad < _serverTime;

            if (isDoneFastForwarding || isDonePausing)
            {
                ResetTimeScale();
            }

            if (!_isInputEnabled && OWInput.GetInputMode() != InputMode.None)
            {
                DisableInput();
            }
        }
    }
}