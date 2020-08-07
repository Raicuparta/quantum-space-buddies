using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QSB.Console
{
    public class ChatHandler : MonoBehaviour
    {
        readonly Vector2 _boxSize = new Vector2(790, 30);
        const int _fontSize = 16;

        Dictionary<Message, GameObject> _boxList = new Dictionary<Message, GameObject>();
        List<Message> _messages = new List<Message>();

        GameObject _board;
        GameObject _input;

        private Text _entryBox;

        void Awake()
        {
            _board = new GameObject("Board");
            _board.transform.parent = gameObject.transform;

            var rt = _board.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, -480);
            rt.localPosition = new Vector3(410, -232, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 0);
            rt.pivot = new Vector2(.5f, .5f);
            rt.localScale = Vector3.one;

            _board.AddComponent<CanvasRenderer>();
            var ri = _board.AddComponent<RawImage>();
            ri.color = new Color32(0, 0, 0, 100);
            ri.raycastTarget = false;

            var eventSys = new GameObject("EventSystem");
            eventSys.transform.parent = _board.transform;

            var sys = eventSys.AddComponent<EventSystem>();
            sys.sendNavigationEvents = false;
            sys.pixelDragThreshold = 10;

            var input = eventSys.AddComponent<StandaloneInputModule>();
            input.horizontalAxis = "Mouse_X";
            input.verticalAxis = "Mouse_Y";
            input.submitButton = "Joystick4Axis9";
            input.cancelButton = "Joystick4Axis10";
            input.inputActionsPerSecond = 10;
            input.repeatDelay = 0.5f;
            input.forceModuleActive = false;

            _input = new GameObject("InputField");
            _input.transform.parent = _board.transform;
            var irt = _input.AddComponent<RectTransform>();
            irt.sizeDelta = new Vector2(500, 30);
            irt.localPosition = new Vector3(0, 320, 0);

            irt.localScale = Vector3.one;

            _input.AddComponent<CanvasRenderer>();

            _input.AddComponent<Image>();

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.parent = _input.transform;

            var prt = placeholder.AddComponent<RectTransform>();
            prt.anchorMax = new Vector2(1, 1);
            prt.anchorMin = new Vector2(0, 0);
            prt.pivot = new Vector2(.5f, .5f);
            prt.sizeDelta = new Vector2(0, 0);
            prt.transform.localPosition = Vector3.zero;
            prt.transform.localScale = Vector3.one;

            var text = placeholder.AddComponent<Text>();
            text.text = "Enter text...";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = Color.black;

            var textGO = new GameObject("Text");
            textGO.transform.parent = _input.transform;

            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMax = new Vector2(1, 1);
            trt.anchorMin = new Vector2(0, 0);
            trt.pivot = new Vector2(.5f, .5f);
            trt.sizeDelta = new Vector2(0, 0);
            trt.transform.localPosition = Vector3.zero;
            trt.transform.localScale = Vector3.one;

            _entryBox = textGO.AddComponent<Text>();
            _entryBox.text = "";
            _entryBox.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _entryBox.color = Color.black;

            var inf = _input.AddComponent<InputField>();
            inf.interactable = true;
            inf.placeholder = text;
            inf.textComponent = _entryBox;

            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mod) => ResetLog();
        }

        public void ResetLog()
        {

            foreach (var item in _boxList)
            {
                Destroy(item.Value);
            }
            _boxList.Clear();

            _messages = new List<Message>();

            foreach (var item in GameObject.FindObjectsOfType<RawImage>())
            {
                if (item.gameObject.name == "OWConsoleMessageBox")
                {
                    GameObject.Destroy(item.gameObject);
                }
            }
        }

        private void DisplayMessages(string searchTerm = "")
        {
            foreach (var item in _boxList)
            {
                Destroy(item.Value);
            }
            _boxList.Clear();

            foreach (var item in GameObject.FindObjectsOfType<RawImage>())
            {
                if (item.gameObject.name == "OWConsoleMessageBox")
                {
                    GameObject.Destroy(item.gameObject);
                }
            }


            float lastYPos = 300;
            foreach (var item in _messages.Skip(_messages.Count - 15).Take(15))
            {
                var box = CreateBox(item.Text, item.PlayerName);
                box.GetComponent<RectTransform>().localPosition = new Vector3(0, lastYPos - 35, 0);
                lastYPos -= 35;
                _boxList.Add(item, box);
            }
        }

        public void PostMessage(string message, string playerName)
        {
            _messages.Add(new Message { Text = message, PlayerName = playerName});

            DisplayMessages(_entryBox.text);
        }

        GameObject CreateBox(string text, string mod)
        {
            // Create dark message box
            GameObject box = new GameObject("OWConsoleMessageBox");
            box.transform.parent = _board.transform;

            var rt = box.AddComponent<RectTransform>();
            rt.sizeDelta = _boxSize;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;

            box.AddComponent<CanvasRenderer>();

            var ri = box.AddComponent<RawImage>();
            ri.color = new Color32(0, 0, 0, 90);
            ri.raycastTarget = false;

            // Create gameobject with text component
            GameObject words = new GameObject("Words");
            words.transform.parent = box.transform;

            var rt2 = words.AddComponent<RectTransform>();
            rt2.sizeDelta = new Vector2(_boxSize.x - 10, _boxSize.y);
            rt2.localScale = Vector3.one;
            rt2.localPosition = Vector3.zero;

            words.AddComponent<CanvasRenderer>();

            var textC = words.AddComponent<Text>();
            textC.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textC.fontSize = _fontSize;
            textC.text = "[" + mod + "] : " + text;
            textC.alignment = TextAnchor.UpperLeft;

            return box;
        }
    }

    class Message
    {
        public string Text { get; set; }
        public string PlayerName { get; set; }
    }
}
