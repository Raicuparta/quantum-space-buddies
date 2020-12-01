using System;
using System.Collections.Generic;
using UnityEngine;

namespace QSB.UNet.Networking
{
	internal class NetworkScene
	{
		internal Dictionary<NetworkInstanceId, NetworkIdentity> localObjects
		{
			get
			{
				return m_LocalObjects;
			}
		}

		internal static Dictionary<NetworkHash128, GameObject> guidToPrefab
		{
			get
			{
				return NetworkScene.s_GuidToPrefab;
			}
		}

		internal static Dictionary<NetworkHash128, SpawnDelegate> spawnHandlers
		{
			get
			{
				return NetworkScene.s_SpawnHandlers;
			}
		}

		internal static Dictionary<NetworkHash128, UnSpawnDelegate> unspawnHandlers
		{
			get
			{
				return NetworkScene.s_UnspawnHandlers;
			}
		}

		internal void Shutdown()
		{
			ClearLocalObjects();
			NetworkScene.ClearSpawners();
		}

		internal void SetLocalObject(NetworkInstanceId netId, GameObject obj, bool isClient, bool isServer)
		{
			if (LogFilter.logDev)
			{
				Debug.Log(string.Concat(new object[]
				{
					"SetLocalObject ",
					netId,
					" ",
					obj
				}));
			}
			if (obj == null)
			{
				m_LocalObjects[netId] = null;
			}
			else
			{
				NetworkIdentity networkIdentity = null;
				if (m_LocalObjects.ContainsKey(netId))
				{
					networkIdentity = m_LocalObjects[netId];
				}
				if (networkIdentity == null)
				{
					networkIdentity = obj.GetComponent<NetworkIdentity>();
					m_LocalObjects[netId] = networkIdentity;
				}
				networkIdentity.UpdateClientServer(isClient, isServer);
			}
		}

		internal GameObject FindLocalObject(NetworkInstanceId netId)
		{
			if (m_LocalObjects.ContainsKey(netId))
			{
				NetworkIdentity networkIdentity = m_LocalObjects[netId];
				if (networkIdentity != null)
				{
					return networkIdentity.gameObject;
				}
			}
			return null;
		}

		internal bool GetNetworkIdentity(NetworkInstanceId netId, out NetworkIdentity uv)
		{
			bool result;
			if (m_LocalObjects.ContainsKey(netId) && m_LocalObjects[netId] != null)
			{
				uv = m_LocalObjects[netId];
				result = true;
			}
			else
			{
				uv = null;
				result = false;
			}
			return result;
		}

		internal bool RemoveLocalObject(NetworkInstanceId netId)
		{
			return m_LocalObjects.Remove(netId);
		}

		internal bool RemoveLocalObjectAndDestroy(NetworkInstanceId netId)
		{
			bool result;
			if (m_LocalObjects.ContainsKey(netId))
			{
				NetworkIdentity networkIdentity = m_LocalObjects[netId];
				UnityEngine.Object.Destroy(networkIdentity.gameObject);
				result = m_LocalObjects.Remove(netId);
			}
			else
			{
				result = false;
			}
			return result;
		}

		internal void ClearLocalObjects()
		{
			m_LocalObjects.Clear();
		}

		internal static void RegisterPrefab(GameObject prefab, NetworkHash128 newAssetId)
		{
			NetworkIdentity component = prefab.GetComponent<NetworkIdentity>();
			if (component)
			{
				component.SetDynamicAssetId(newAssetId);
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Registering prefab '",
						prefab.name,
						"' as asset:",
						component.assetId
					}));
				}
				NetworkScene.s_GuidToPrefab[component.assetId] = prefab;
			}
			else if (LogFilter.logError)
			{
				Debug.LogError("Could not register '" + prefab.name + "' since it contains no NetworkIdentity component");
			}
		}

		internal static void RegisterPrefab(GameObject prefab)
		{
			NetworkIdentity component = prefab.GetComponent<NetworkIdentity>();
			if (component)
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Registering prefab '",
						prefab.name,
						"' as asset:",
						component.assetId
					}));
				}
				NetworkScene.s_GuidToPrefab[component.assetId] = prefab;
				NetworkIdentity[] componentsInChildren = prefab.GetComponentsInChildren<NetworkIdentity>();
				if (componentsInChildren.Length > 1)
				{
					if (LogFilter.logWarn)
					{
						Debug.LogWarning("The prefab '" + prefab.name + "' has multiple NetworkIdentity components. There can only be one NetworkIdentity on a prefab, and it must be on the root object.");
					}
				}
			}
			else if (LogFilter.logError)
			{
				Debug.LogError("Could not register '" + prefab.name + "' since it contains no NetworkIdentity component");
			}
		}

		internal static bool GetPrefab(NetworkHash128 assetId, out GameObject prefab)
		{
			bool result;
			if (!assetId.IsValid())
			{
				prefab = null;
				result = false;
			}
			else if (NetworkScene.s_GuidToPrefab.ContainsKey(assetId) && NetworkScene.s_GuidToPrefab[assetId] != null)
			{
				prefab = NetworkScene.s_GuidToPrefab[assetId];
				result = true;
			}
			else
			{
				prefab = null;
				result = false;
			}
			return result;
		}

		internal static void ClearSpawners()
		{
			NetworkScene.s_GuidToPrefab.Clear();
			NetworkScene.s_SpawnHandlers.Clear();
			NetworkScene.s_UnspawnHandlers.Clear();
		}

		public static void UnregisterSpawnHandler(NetworkHash128 assetId)
		{
			NetworkScene.s_SpawnHandlers.Remove(assetId);
			NetworkScene.s_UnspawnHandlers.Remove(assetId);
		}

		internal static void RegisterSpawnHandler(NetworkHash128 assetId, SpawnDelegate spawnHandler, UnSpawnDelegate unspawnHandler)
		{
			if (spawnHandler == null || unspawnHandler == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RegisterSpawnHandler custom spawn function null for " + assetId);
				}
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"RegisterSpawnHandler asset '",
						assetId,
						"' ",
						spawnHandler.GetMethodName(),
						"/",
						unspawnHandler.GetMethodName()
					}));
				}
				NetworkScene.s_SpawnHandlers[assetId] = spawnHandler;
				NetworkScene.s_UnspawnHandlers[assetId] = unspawnHandler;
			}
		}

		internal static void UnregisterPrefab(GameObject prefab)
		{
			NetworkIdentity component = prefab.GetComponent<NetworkIdentity>();
			if (component == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Could not unregister '" + prefab.name + "' since it contains no NetworkIdentity component");
				}
			}
			else
			{
				NetworkScene.s_SpawnHandlers.Remove(component.assetId);
				NetworkScene.s_UnspawnHandlers.Remove(component.assetId);
			}
		}

		internal static void RegisterPrefab(GameObject prefab, SpawnDelegate spawnHandler, UnSpawnDelegate unspawnHandler)
		{
			NetworkIdentity component = prefab.GetComponent<NetworkIdentity>();
			if (component == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Could not register '" + prefab.name + "' since it contains no NetworkIdentity component");
				}
			}
			else if (spawnHandler == null || unspawnHandler == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RegisterPrefab custom spawn function null for " + component.assetId);
				}
			}
			else if (!component.assetId.IsValid())
			{
				if (LogFilter.logError)
				{
					Debug.LogError("RegisterPrefab game object " + prefab.name + " has no prefab. Use RegisterSpawnHandler() instead?");
				}
			}
			else
			{
				if (LogFilter.logDebug)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Registering custom prefab '",
						prefab.name,
						"' as asset:",
						component.assetId,
						" ",
						spawnHandler.GetMethodName(),
						"/",
						unspawnHandler.GetMethodName()
					}));
				}
				NetworkScene.s_SpawnHandlers[component.assetId] = spawnHandler;
				NetworkScene.s_UnspawnHandlers[component.assetId] = unspawnHandler;
			}
		}

		internal static bool GetSpawnHandler(NetworkHash128 assetId, out SpawnDelegate handler)
		{
			bool result;
			if (NetworkScene.s_SpawnHandlers.ContainsKey(assetId))
			{
				handler = NetworkScene.s_SpawnHandlers[assetId];
				result = true;
			}
			else
			{
				handler = null;
				result = false;
			}
			return result;
		}

		internal static bool InvokeUnSpawnHandler(NetworkHash128 assetId, GameObject obj)
		{
			bool result;
			if (NetworkScene.s_UnspawnHandlers.ContainsKey(assetId) && NetworkScene.s_UnspawnHandlers[assetId] != null)
			{
				UnSpawnDelegate unSpawnDelegate = NetworkScene.s_UnspawnHandlers[assetId];
				unSpawnDelegate(obj);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		internal void DestroyAllClientObjects()
		{
			foreach (NetworkInstanceId key in m_LocalObjects.Keys)
			{
				NetworkIdentity networkIdentity = m_LocalObjects[key];
				if (networkIdentity != null && networkIdentity.gameObject != null)
				{
					if (!NetworkScene.InvokeUnSpawnHandler(networkIdentity.assetId, networkIdentity.gameObject))
					{
						if (networkIdentity.sceneId.IsEmpty())
						{
							UnityEngine.Object.Destroy(networkIdentity.gameObject);
						}
						else
						{
							networkIdentity.MarkForReset();
							networkIdentity.gameObject.SetActive(false);
						}
					}
				}
			}
			ClearLocalObjects();
		}

		internal void DumpAllClientObjects()
		{
			foreach (NetworkInstanceId networkInstanceId in m_LocalObjects.Keys)
			{
				NetworkIdentity networkIdentity = m_LocalObjects[networkInstanceId];
				if (networkIdentity != null)
				{
					Debug.Log(string.Concat(new object[]
					{
						"ID:",
						networkInstanceId,
						" OBJ:",
						networkIdentity.gameObject,
						" AS:",
						networkIdentity.assetId
					}));
				}
				else
				{
					Debug.Log("ID:" + networkInstanceId + " OBJ: null");
				}
			}
		}

		private Dictionary<NetworkInstanceId, NetworkIdentity> m_LocalObjects = new Dictionary<NetworkInstanceId, NetworkIdentity>();

		private static Dictionary<NetworkHash128, GameObject> s_GuidToPrefab = new Dictionary<NetworkHash128, GameObject>();

		private static Dictionary<NetworkHash128, SpawnDelegate> s_SpawnHandlers = new Dictionary<NetworkHash128, SpawnDelegate>();

		private static Dictionary<NetworkHash128, UnSpawnDelegate> s_UnspawnHandlers = new Dictionary<NetworkHash128, UnSpawnDelegate>();
	}
}
