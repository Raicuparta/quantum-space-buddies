using OWML.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace QSB.TimeSync
{
	internal class TimeSyncUI : MonoBehaviour
	{
		public static TimeSyncUI Instance;

		public static float TargetTime;

		private Canvas _canvas;
		private Text _text;
		private float _startTime;
		private bool _isSetUp;
		private TimeSyncType _currentType;

		public void Awake()
		{
			Instance = this;
			enabled = false;

			QSBSceneManager.OnUniverseSceneLoaded += OnUniverseSceneLoad;
		}

		private void OnUniverseSceneLoad(OWScene scene)
		{
			_isSetUp = true;
			var obj = Resources.FindObjectsOfTypeAll<SleepTimerUI>()[0];
			_canvas = obj.GetValue<Canvas>("_canvas");
			_text = obj.GetValue<Text>("_text");
			_canvas.enabled = false;
		}

		public void OnDestroy()
		{
			QSBSceneManager.OnUniverseSceneLoaded -= OnUniverseSceneLoad;
			if (_canvas.enabled)
			{
				Canvas.willRenderCanvases -= OnWillRenderCanvases;
			}
		}

		public static void Start(TimeSyncType type) =>
			QSBCore.Helper.Events.Unity.RunWhen(() => Instance._isSetUp, () => Instance.StartTimeSync(type));

		public static void Stop() =>
			QSBCore.Helper.Events.Unity.RunWhen(() => Instance._isSetUp, () => Instance.EndTimeSync());

		private void StartTimeSync(TimeSyncType type)
		{
			_currentType = type;
			_startTime = Time.timeSinceLevelLoad;
			enabled = true;
			_canvas.enabled = true;
			Canvas.willRenderCanvases += OnWillRenderCanvases;
		}

		private void EndTimeSync()
		{
			_currentType = TimeSyncType.None;
			enabled = false;
			_canvas.enabled = false;
			Canvas.willRenderCanvases -= OnWillRenderCanvases;
		}

		private void OnWillRenderCanvases()
		{
			if (!_isSetUp)
			{
				return;
			}
			var totalSeconds = Mathf.Max(TargetTime - Time.timeSinceLevelLoad, 0f);
			var minutes = Mathf.FloorToInt(totalSeconds / 60f);
			var seconds = Mathf.FloorToInt(totalSeconds) % 60;
			var text = "";
			switch (_currentType)
			{
				case TimeSyncType.Fastforwarding:
					text = $"{minutes:D2}:{seconds:D2}"
						+ Environment.NewLine
						+ "Fast-forwarding to match server time...";
					break;

				case TimeSyncType.Pausing:
					text = "Pausing to match server time...";
					break;
			}
			_text.text = text;
		}
	}
}