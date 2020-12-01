using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	[AddComponentMenu("Network/NetworkTransformChild")]
	public class NetworkTransformChild : NetworkBehaviour
	{
		public Transform target
		{
			get
			{
				return m_Target;
			}
			set
			{
				m_Target = value;
				OnValidate();
			}
		}

		public uint childIndex
		{
			get
			{
				return m_ChildIndex;
			}
		}

		public float sendInterval
		{
			get
			{
				return m_SendInterval;
			}
			set
			{
				m_SendInterval = value;
			}
		}

		public NetworkTransform.AxisSyncMode syncRotationAxis
		{
			get
			{
				return m_SyncRotationAxis;
			}
			set
			{
				m_SyncRotationAxis = value;
			}
		}

		public NetworkTransform.CompressionSyncMode rotationSyncCompression
		{
			get
			{
				return m_RotationSyncCompression;
			}
			set
			{
				m_RotationSyncCompression = value;
			}
		}

		public float movementThreshold
		{
			get
			{
				return m_MovementThreshold;
			}
			set
			{
				m_MovementThreshold = value;
			}
		}

		public float interpolateRotation
		{
			get
			{
				return m_InterpolateRotation;
			}
			set
			{
				m_InterpolateRotation = value;
			}
		}

		public float interpolateMovement
		{
			get
			{
				return m_InterpolateMovement;
			}
			set
			{
				m_InterpolateMovement = value;
			}
		}

		public NetworkTransform.ClientMoveCallback3D clientMoveCallback3D
		{
			get
			{
				return m_ClientMoveCallback3D;
			}
			set
			{
				m_ClientMoveCallback3D = value;
			}
		}

		public float lastSyncTime
		{
			get
			{
				return m_LastClientSyncTime;
			}
		}

		public Vector3 targetSyncPosition
		{
			get
			{
				return m_TargetSyncPosition;
			}
		}

		public Quaternion targetSyncRotation3D
		{
			get
			{
				return m_TargetSyncRotation3D;
			}
		}

		private void OnValidate()
		{
			if (m_Target != null)
			{
				Transform parent = m_Target.parent;
				if (parent == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkTransformChild target cannot be the root transform.");
					}
					m_Target = null;
					return;
				}
				while (parent.parent != null)
				{
					parent = parent.parent;
				}
				m_Root = parent.gameObject.GetComponent<NetworkTransform>();
				if (m_Root == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkTransformChild root must have NetworkTransform");
					}
					m_Target = null;
					return;
				}
			}
			if (m_Root != null)
			{
				m_ChildIndex = uint.MaxValue;
				NetworkTransformChild[] components = m_Root.GetComponents<NetworkTransformChild>();
				uint num = 0U;
				while ((ulong)num < (ulong)((long)components.Length))
				{
					if (components[(int)((UIntPtr)num)] == this)
					{
						m_ChildIndex = num;
						break;
					}
					num += 1U;
				}
				if (m_ChildIndex == 4294967295U)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("NetworkTransformChild component must be a child in the same hierarchy");
					}
					m_Target = null;
				}
			}
			if (m_SendInterval < 0f)
			{
				m_SendInterval = 0f;
			}
			if (m_SyncRotationAxis < NetworkTransform.AxisSyncMode.None || m_SyncRotationAxis > NetworkTransform.AxisSyncMode.AxisXYZ)
			{
				m_SyncRotationAxis = NetworkTransform.AxisSyncMode.None;
			}
			if (movementThreshold < 0f)
			{
				movementThreshold = 0f;
			}
			if (interpolateRotation < 0f)
			{
				interpolateRotation = 0.01f;
			}
			if (interpolateRotation > 1f)
			{
				interpolateRotation = 1f;
			}
			if (interpolateMovement < 0f)
			{
				interpolateMovement = 0.01f;
			}
			if (interpolateMovement > 1f)
			{
				interpolateMovement = 1f;
			}
		}

		private void Awake()
		{
			m_PrevPosition = m_Target.localPosition;
			m_PrevRotation = m_Target.localRotation;
			if (base.localPlayerAuthority)
			{
				m_LocalTransformWriter = new NetworkWriter();
			}
		}

		public override bool OnSerialize(NetworkWriter writer, bool initialState)
		{
			if (!initialState)
			{
				if (base.syncVarDirtyBits == 0U)
				{
					writer.WritePackedUInt32(0U);
					return false;
				}
				writer.WritePackedUInt32(1U);
			}
			SerializeModeTransform(writer);
			return true;
		}

		private void SerializeModeTransform(NetworkWriter writer)
		{
			writer.Write(m_Target.localPosition);
			if (m_SyncRotationAxis != NetworkTransform.AxisSyncMode.None)
			{
				NetworkTransform.SerializeRotation3D(writer, m_Target.localRotation, syncRotationAxis, rotationSyncCompression);
			}
			m_PrevPosition = m_Target.localPosition;
			m_PrevRotation = m_Target.localRotation;
		}

		public override void OnDeserialize(NetworkReader reader, bool initialState)
		{
			if (!base.isServer || !NetworkServer.localClientActive)
			{
				if (!initialState)
				{
					if (reader.ReadPackedUInt32() == 0U)
					{
						return;
					}
				}
				UnserializeModeTransform(reader, initialState);
				m_LastClientSyncTime = Time.time;
			}
		}

		private void UnserializeModeTransform(NetworkReader reader, bool initialState)
		{
			if (base.hasAuthority)
			{
				reader.ReadVector3();
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
			}
			else if (base.isServer && m_ClientMoveCallback3D != null)
			{
				Vector3 targetSyncPosition = reader.ReadVector3();
				Vector3 zero = Vector3.zero;
				Quaternion targetSyncRotation3D = Quaternion.identity;
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					targetSyncRotation3D = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
				if (m_ClientMoveCallback3D(ref targetSyncPosition, ref zero, ref targetSyncRotation3D))
				{
					m_TargetSyncPosition = targetSyncPosition;
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						m_TargetSyncRotation3D = targetSyncRotation3D;
					}
				}
			}
			else
			{
				m_TargetSyncPosition = reader.ReadVector3();
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					m_TargetSyncRotation3D = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
			}
		}

		private void FixedUpdate()
		{
			if (base.isServer)
			{
				FixedUpdateServer();
			}
			if (base.isClient)
			{
				FixedUpdateClient();
			}
		}

		private void FixedUpdateServer()
		{
			if (base.syncVarDirtyBits == 0U)
			{
				if (NetworkServer.active)
				{
					if (base.isServer)
					{
						if (GetNetworkSendInterval() != 0f)
						{
							float num = (m_Target.localPosition - m_PrevPosition).sqrMagnitude;
							if (num < movementThreshold)
							{
								num = Quaternion.Angle(m_PrevRotation, m_Target.localRotation);
								if (num < movementThreshold)
								{
									return;
								}
							}
							base.SetDirtyBit(1U);
						}
					}
				}
			}
		}

		private void FixedUpdateClient()
		{
			if (m_LastClientSyncTime != 0f)
			{
				if (NetworkServer.active || NetworkClient.active)
				{
					if (base.isServer || base.isClient)
					{
						if (GetNetworkSendInterval() != 0f)
						{
							if (!base.hasAuthority)
							{
								if (m_LastClientSyncTime != 0f)
								{
									if (m_InterpolateMovement > 0f)
									{
										m_Target.localPosition = Vector3.Lerp(m_Target.localPosition, m_TargetSyncPosition, m_InterpolateMovement);
									}
									else
									{
										m_Target.localPosition = m_TargetSyncPosition;
									}
									if (m_InterpolateRotation > 0f)
									{
										m_Target.localRotation = Quaternion.Slerp(m_Target.localRotation, m_TargetSyncRotation3D, m_InterpolateRotation);
									}
									else
									{
										m_Target.localRotation = m_TargetSyncRotation3D;
									}
								}
							}
						}
					}
				}
			}
		}

		private void Update()
		{
			if (base.hasAuthority)
			{
				if (base.localPlayerAuthority)
				{
					if (!NetworkServer.active)
					{
						if (Time.time - m_LastClientSendTime > GetNetworkSendInterval())
						{
							SendTransform();
							m_LastClientSendTime = Time.time;
						}
					}
				}
			}
		}

		private bool HasMoved()
		{
			float num = (m_Target.localPosition - m_PrevPosition).sqrMagnitude;
			bool result;
			if (num > 1E-05f)
			{
				result = true;
			}
			else
			{
				num = Quaternion.Angle(m_Target.localRotation, m_PrevRotation);
				result = (num > 1E-05f);
			}
			return result;
		}

		[Client]
		private void SendTransform()
		{
			if (HasMoved() && ClientScene.readyConnection != null)
			{
				m_LocalTransformWriter.StartMessage(16);
				m_LocalTransformWriter.Write(base.netId);
				m_LocalTransformWriter.WritePackedUInt32(m_ChildIndex);
				SerializeModeTransform(m_LocalTransformWriter);
				m_PrevPosition = m_Target.localPosition;
				m_PrevRotation = m_Target.localRotation;
				m_LocalTransformWriter.FinishMessage();
				ClientScene.readyConnection.SendWriter(m_LocalTransformWriter, GetNetworkChannel());
			}
		}

		internal static void HandleChildTransform(NetworkMessage netMsg)
		{
			NetworkInstanceId networkInstanceId = netMsg.reader.ReadNetworkId();
			uint num = netMsg.reader.ReadPackedUInt32();
			GameObject gameObject = NetworkServer.FindLocalObject(networkInstanceId);
			if (gameObject == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Received NetworkTransformChild data for GameObject that doesn't exist");
				}
			}
			else
			{
				NetworkTransformChild[] components = gameObject.GetComponents<NetworkTransformChild>();
				if (components == null || components.Length == 0)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("HandleChildTransform no children");
					}
				}
				else if ((ulong)num >= (ulong)((long)components.Length))
				{
					if (LogFilter.logError)
					{
						Debug.LogError("HandleChildTransform childIndex invalid");
					}
				}
				else
				{
					NetworkTransformChild networkTransformChild = components[(int)((UIntPtr)num)];
					if (networkTransformChild == null)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("HandleChildTransform null target");
						}
					}
					else if (!networkTransformChild.localPlayerAuthority)
					{
						if (LogFilter.logError)
						{
							Debug.LogError("HandleChildTransform no localPlayerAuthority");
						}
					}
					else if (!netMsg.conn.clientOwnedObjects.Contains(networkInstanceId))
					{
						if (LogFilter.logWarn)
						{
							Debug.LogWarning("NetworkTransformChild netId:" + networkInstanceId + " is not for a valid player");
						}
					}
					else
					{
						networkTransformChild.UnserializeModeTransform(netMsg.reader, false);
						networkTransformChild.m_LastClientSyncTime = Time.time;
						if (!networkTransformChild.isClient)
						{
							networkTransformChild.m_Target.localPosition = networkTransformChild.m_TargetSyncPosition;
							networkTransformChild.m_Target.localRotation = networkTransformChild.m_TargetSyncRotation3D;
						}
					}
				}
			}
		}

		public override int GetNetworkChannel()
		{
			return 1;
		}

		public override float GetNetworkSendInterval()
		{
			return m_SendInterval;
		}

		[SerializeField]
		private Transform m_Target;

		[SerializeField]
		private uint m_ChildIndex;

		private NetworkTransform m_Root;

		[SerializeField]
		private float m_SendInterval = 0.1f;

		[SerializeField]
		private NetworkTransform.AxisSyncMode m_SyncRotationAxis = NetworkTransform.AxisSyncMode.AxisXYZ;

		[SerializeField]
		private NetworkTransform.CompressionSyncMode m_RotationSyncCompression = NetworkTransform.CompressionSyncMode.None;

		[SerializeField]
		private float m_MovementThreshold = 0.001f;

		[SerializeField]
		private float m_InterpolateRotation = 0.5f;

		[SerializeField]
		private float m_InterpolateMovement = 0.5f;

		[SerializeField]
		private NetworkTransform.ClientMoveCallback3D m_ClientMoveCallback3D;

		private Vector3 m_TargetSyncPosition;

		private Quaternion m_TargetSyncRotation3D;

		private float m_LastClientSyncTime;

		private float m_LastClientSendTime;

		private Vector3 m_PrevPosition;

		private Quaternion m_PrevRotation;

		private const float k_LocalMovementThreshold = 1E-05f;

		private const float k_LocalRotationThreshold = 1E-05f;

		private NetworkWriter m_LocalTransformWriter;
	}
}
