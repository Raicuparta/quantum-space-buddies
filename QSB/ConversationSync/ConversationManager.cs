using OWML.Common;
using OWML.Utils;
using QSB.Events;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace QSB.ConversationSync
{
	public class ConversationManager : MonoBehaviour
	{
		public static ConversationManager Instance { get; private set; }
		public AssetBundle ConversationAssetBundle { get; private set; }
		public Dictionary<CharacterDialogueTree, GameObject> BoxMappings { get; } = new Dictionary<CharacterDialogueTree, GameObject>();

		private GameObject _boxPrefab;

		public void Start()
		{
			Instance = this;

			ConversationAssetBundle = QSBCore.Helper.Assets.LoadBundle("assets/conversation");

			_boxPrefab = ConversationAssetBundle.LoadAsset<GameObject>("assets/dialoguebubble.prefab");
			// TODO : make dynamic so it can be different sizes!
			var font = (Font)Resources.Load(@"fonts\english - latin\spacemono-bold");
			if (font == null)
			{
				DebugLog.ToConsole("Error - Font is null!", MessageType.Error);
			}
			_boxPrefab.GetComponent<Text>().font = font;
			_boxPrefab.GetComponent<Text>().color = Color.white;
		}

		public uint GetPlayerTalkingToTree(CharacterDialogueTree tree)
		{
			var treeIndex = QSBWorldSync.OldDialogueTrees.IndexOf(tree);
			return QSBPlayerManager.PlayerList.All(x => x.CurrentDialogueID != treeIndex)
				? uint.MaxValue
				: QSBPlayerManager.PlayerList.First(x => x.CurrentDialogueID == treeIndex).PlayerId;
		}

		public void SendPlayerOption(string text) =>
			GlobalMessenger<uint, string, ConversationType>
				.FireEvent(EventNames.QSBConversation, QSBPlayerManager.LocalPlayerId, text, ConversationType.Player);

		public void SendCharacterDialogue(int id, string text)
		{
			if (id == -1)
			{
				DebugLog.ToConsole("Warning - Tried to send conv. event with char id -1.", MessageType.Warning);
				return;
			}
			GlobalMessenger<uint, string, ConversationType>
				.FireEvent(EventNames.QSBConversation, (uint)id, text, ConversationType.Character);
		}

		public void CloseBoxPlayer() =>
			GlobalMessenger<uint, string, ConversationType>
				.FireEvent(EventNames.QSBConversation, QSBPlayerManager.LocalPlayerId, "", ConversationType.ClosePlayer);

		public void CloseBoxCharacter(int id) =>
			GlobalMessenger<uint, string, ConversationType>
				.FireEvent(EventNames.QSBConversation, (uint)id, "", ConversationType.CloseCharacter);

		public void SendConvState(int charId, bool state)
		{
			if (charId == -1)
			{
				DebugLog.ToConsole("Warning - Tried to send conv. start/end event with char id -1.", MessageType.Warning);
				return;
			}
			GlobalMessenger<int, uint, bool>
				.FireEvent(EventNames.QSBConversationStartEnd, charId, QSBPlayerManager.LocalPlayerId, state);
		}

		public void DisplayPlayerConversationBox(uint playerId, string text)
		{
			if (playerId == QSBPlayerManager.LocalPlayerId)
			{
				DebugLog.ToConsole("Error - Cannot display conversation box for local player!", MessageType.Error);
				return;
			}

			var player = QSBPlayerManager.GetPlayer(playerId);

			// Destroy old box if it exists
			var playerBox = player.CurrentDialogueBox;
			if (playerBox != null)
			{
				Destroy(playerBox);
			}

			QSBPlayerManager.GetPlayer(playerId).CurrentDialogueBox = CreateBox(player.Body.transform, 25, text);
		}

		public void DisplayCharacterConversationBox(int index, string text)
		{
			if (QSBWorldSync.OldDialogueTrees.ElementAtOrDefault(index) == null)
			{
				DebugLog.ToConsole($"Error - Tried to display character conversation box for id {index}! (Doesn't exist!)", MessageType.Error);
				return;
			}

			// Remove old box if it exists
			var oldDialogueTree = QSBWorldSync.OldDialogueTrees[index];
			if (BoxMappings.ContainsKey(oldDialogueTree))
			{
				Destroy(BoxMappings[oldDialogueTree]);
				BoxMappings.Remove(oldDialogueTree);
			}

			BoxMappings.Add(oldDialogueTree, CreateBox(oldDialogueTree.gameObject.transform, 2, text));
		}

		private GameObject CreateBox(Transform parent, float vertOffset, string text)
		{
			var newBox = Instantiate(_boxPrefab);
			newBox.SetActive(false);
			newBox.transform.parent = parent;
			newBox.transform.localPosition = new Vector3(0, vertOffset, 0);
			newBox.transform.rotation = parent.rotation;
			var lookAt = newBox.AddComponent<FaceActiveCamera>();
			lookAt.SetValue("_useLookAt", false);
			lookAt.SetValue("_localFacingVector", Vector3.back);
			lookAt.SetValue("_localRotationAxis", Vector3.up);
			newBox.GetComponent<Text>().text = text;
			newBox.SetActive(true);
			return newBox;
		}
	}
}