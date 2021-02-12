using OWML.Common;
using QSB.OrbSync;
using QSB.TransformSync;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QSB.WorldSync
{
	public static class QSBWorldSync
	{
		public static List<NomaiOrbTransformSync> OrbSyncList { get; } = new List<NomaiOrbTransformSync>();
		public static List<NomaiInterfaceOrb> OldOrbList { get; set; } = new List<NomaiInterfaceOrb>();
		public static List<CharacterDialogueTree> OldDialogueTrees { get; set; } = new List<CharacterDialogueTree>();
		public static Dictionary<string, bool> DialogueConditions { get; } = new Dictionary<string, bool>();
		public static List<FactReveal> ShipLogFacts { get; } = new List<FactReveal>();

		private static readonly List<WorldObject> WorldObjects = new List<WorldObject>();

		public static void AddWorldObject(WorldObject worldObject)
		{
			if (WorldObjects.Contains(worldObject))
			{
				return;
			}
			WorldObjects.Add(worldObject);
		}

		public static IEnumerable<T> GetWorldObjects<T>() => WorldObjects.OfType<T>();

		public static T GetWorldObject<T>(int id) where T : WorldObject => GetWorldObjects<T>().FirstOrDefault(x => x.ObjectId == id);

		public static void RemoveWorldObjects<T>() where T : WorldObject => WorldObjects.RemoveAll(x => x.GetType() == typeof(T));

		public static void HandleSlotStateChange(NomaiInterfaceSlot slot, NomaiInterfaceOrb affectingOrb, bool state)
		{
			var slotList = GetWorldObjects<QSBOrbSlot>().ToList();
			if (!slotList.Any())
			{
				return;
			}
			var qsbSlot = slotList.First(x => x.InterfaceSlot == slot);
			var orbSync = OrbSyncList.First(x => x.AttachedOrb == affectingOrb);
			if (orbSync.HasAuthority)
			{
				qsbSlot.HandleEvent(state, OldOrbList.IndexOf(affectingOrb));
			}
		}

		public static void RaiseEvent(object instance, string eventName)
		{
			if (!(instance.GetType()
				.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)?
				.GetValue(instance) is MulticastDelegate multiDelegate))
			{
				return;
			}
			var delegateList = multiDelegate.GetInvocationList().ToList();
			foreach (var del in delegateList)
			{
				del.DynamicInvoke(instance);
			}
		}

		public static void SetDialogueCondition(string name, bool state)
		{
			if (!QSBCore.IsServer)
			{
				DebugLog.ToConsole("Warning - Cannot write to condition dict when not server!", MessageType.Warning);
				return;
			}
			DialogueConditions[name] = state;
		}

		public static void AddFactReveal(string id, bool saveGame, bool showNotification)
		{
			if (!QSBCore.IsServer)
			{
				DebugLog.ToConsole("Warning - Cannot write to fact list when not server!", MessageType.Warning);
				return;
			}
			if (ShipLogFacts.Any(x => x.Id == id))
			{
				DebugLog.ToConsole($"Warning - Fact with id {id} already exists in list!", MessageType.Warning);
				return;
			}
			ShipLogFacts.Add(new FactReveal
			{
				Id = id,
				SaveGame = saveGame,
				ShowNotification = showNotification
			});
		}
	}
}