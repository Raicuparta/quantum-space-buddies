using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Networking.Match;

namespace QSB.UNet.Networking
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[AddComponentMenu("Network/NetworkManagerHUD")]
	[RequireComponent(typeof(NetworkManager))]
	public class NetworkManagerHUD : MonoBehaviour
	{
		private void Awake()
		{
			manager = base.GetComponent<NetworkManager>();
		}

		private void OnGUI()
		{
			if (showGUI)
			{
				int num = 10 + offsetX;
				int num2 = 40 + offsetY;
				bool flag = manager.client == null || manager.client.connection == null || manager.client.connection.connectionId == -1;
				if (!manager.IsClientConnected() && !NetworkServer.active)
				{
					if (flag)
					{
						if (Application.platform != RuntimePlatform.WebGLPlayer)
						{
							if (GUI.Button(new Rect((float)num, (float)num2, 200f, 20f), "LAN Host(H)"))
							{
								manager.StartHost();
							}
							num2 += 24;
						}
						if (GUI.Button(new Rect((float)num, (float)num2, 105f, 20f), "LAN Client(C)"))
						{
							manager.StartClient();
						}
						manager.networkAddress = GUI.TextField(new Rect((float)(num + 100), (float)num2, 95f, 20f), manager.networkAddress);
						num2 += 24;
						if (Application.platform == RuntimePlatform.WebGLPlayer)
						{
							GUI.Box(new Rect((float)num, (float)num2, 200f, 25f), "(  WebGL cannot be server  )");
							num2 += 24;
						}
						else
						{
							if (GUI.Button(new Rect((float)num, (float)num2, 200f, 20f), "LAN Server Only(S)"))
							{
								manager.StartServer();
							}
							num2 += 24;
						}
					}
					else
					{
						GUI.Label(new Rect((float)num, (float)num2, 200f, 20f), string.Concat(new object[]
						{
							"Connecting to ",
							manager.networkAddress,
							":",
							manager.networkPort,
							".."
						}));
						num2 += 24;
						if (GUI.Button(new Rect((float)num, (float)num2, 200f, 20f), "Cancel Connection Attempt"))
						{
							manager.StopClient();
						}
					}
				}
				else
				{
					if (NetworkServer.active)
					{
						string text = "Server: port=" + manager.networkPort;
						if (manager.useWebSockets)
						{
							text += " (Using WebSockets)";
						}
						GUI.Label(new Rect((float)num, (float)num2, 300f, 20f), text);
						num2 += 24;
					}
					if (manager.IsClientConnected())
					{
						GUI.Label(new Rect((float)num, (float)num2, 300f, 20f), string.Concat(new object[]
						{
							"Client: address=",
							manager.networkAddress,
							" port=",
							manager.networkPort
						}));
						num2 += 24;
					}
				}
				if (manager.IsClientConnected() && !ClientScene.ready)
				{
					if (GUI.Button(new Rect((float)num, (float)num2, 200f, 20f), "Client Ready"))
					{
						ClientScene.Ready(manager.client.connection);
						if (ClientScene.localPlayers.Count == 0)
						{
							ClientScene.AddPlayer(0);
						}
					}
					num2 += 24;
				}
				if (NetworkServer.active || manager.IsClientConnected())
				{
					if (GUI.Button(new Rect((float)num, (float)num2, 200f, 20f), "Stop (X)"))
					{
						manager.StopHost();
					}
					num2 += 24;
				}
				if (!NetworkServer.active && !manager.IsClientConnected() && flag)
				{
					num2 += 10;
					if (Application.platform == RuntimePlatform.WebGLPlayer)
					{
						GUI.Box(new Rect((float)(num - 5), (float)num2, 220f, 25f), "(WebGL cannot use Match Maker)");
					}
				}
			}
		}

		public NetworkManager manager;

		[SerializeField]
		public bool showGUI = true;

		[SerializeField]
		public int offsetX;

		[SerializeField]
		public int offsetY;

		private bool m_ShowServer;
	}
}
