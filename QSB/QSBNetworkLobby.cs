using OWML.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB
{
	public class QSBNetworkLobby : NetworkBehaviour
	{
		public bool CanEditName { get; set; }
		public string PlayerName { get; private set; }

		private readonly string[] _defaultNames = {
			"Arkose",
			"Chert",
			"Esker",
			"Hal",
			"Hornfels",
			"Feldspar",
			"Gabbro",
			"Galena",
			"Gneiss",
			"Gossan",
			"Marl",
			"Mica",
			"Moraine",
			"Porphy",
			"Riebeck",
			"Rutile",
			"Slate",
			"Spinel",
			"Tektite",
			"Tephra",
			"Tuff",
			"Jinha"
		};

		public void Awake()
		{
			PlayerName = GetPlayerName();
			CanEditName = true;
			QSBCore.Helper.HarmonyHelper.EmptyMethod<NetworkManagerHUD>("Update");
		}

		private string GetPlayerName()
		{
			var profileManager = StandaloneProfileManager.SharedInstance;
			profileManager.Initialize();
			var profile = profileManager.GetValue<StandaloneProfileManager.ProfileData>("_currentProfile");
			var profileName = profile?.profileName;
			return !string.IsNullOrEmpty(profileName)
				? profileName
				: _defaultNames.OrderBy(x => Guid.NewGuid()).First();
		}

		public void OnGUI()
		{
			GUI.Label(new Rect(10, 10, 200f, 20f), "Name:");
			if (CanEditName)
			{
				PlayerName = GUI.TextField(new Rect(60, 10, 145, 20f), PlayerName);
			}
			else
			{
				GUI.Label(new Rect(60, 10, 145, 20f), PlayerName);
			}
		}
	}
}