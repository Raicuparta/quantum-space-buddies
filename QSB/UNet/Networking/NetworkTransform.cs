using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	[AddComponentMenu("Network/NetworkTransform")]
	[DisallowMultipleComponent]
	public class NetworkTransform : NetworkBehaviour
	{
		public NetworkTransform.TransformSyncMode transformSyncMode
		{
			get
			{
				return m_TransformSyncMode;
			}
			set
			{
				m_TransformSyncMode = value;
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

		public bool syncSpin
		{
			get
			{
				return m_SyncSpin;
			}
			set
			{
				m_SyncSpin = value;
			}
		}

		public float movementTheshold
		{
			get
			{
				return m_MovementTheshold;
			}
			set
			{
				m_MovementTheshold = value;
			}
		}

		public float velocityThreshold
		{
			get
			{
				return m_VelocityThreshold;
			}
			set
			{
				m_VelocityThreshold = value;
			}
		}

		public float snapThreshold
		{
			get
			{
				return m_SnapThreshold;
			}
			set
			{
				m_SnapThreshold = value;
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

		public NetworkTransform.ClientMoveCallback2D clientMoveCallback2D
		{
			get
			{
				return m_ClientMoveCallback2D;
			}
			set
			{
				m_ClientMoveCallback2D = value;
			}
		}

		public CharacterController characterContoller
		{
			get
			{
				return m_CharacterController;
			}
		}

		public Rigidbody rigidbody3D
		{
			get
			{
				return m_RigidBody3D;
			}
		}

		public Rigidbody2D rigidbody2D
		{
			get
			{
				return m_RigidBody2D;
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

		public Vector3 targetSyncVelocity
		{
			get
			{
				return m_TargetSyncVelocity;
			}
		}

		public Quaternion targetSyncRotation3D
		{
			get
			{
				return m_TargetSyncRotation3D;
			}
		}

		public float targetSyncRotation2D
		{
			get
			{
				return m_TargetSyncRotation2D;
			}
		}

		public bool grounded
		{
			get
			{
				return m_Grounded;
			}
			set
			{
				m_Grounded = value;
			}
		}

		private void OnValidate()
		{
			if (m_TransformSyncMode < NetworkTransform.TransformSyncMode.SyncNone || m_TransformSyncMode > NetworkTransform.TransformSyncMode.SyncCharacterController)
			{
				m_TransformSyncMode = NetworkTransform.TransformSyncMode.SyncTransform;
			}
			if (m_SendInterval < 0f)
			{
				m_SendInterval = 0f;
			}
			if (m_SyncRotationAxis < NetworkTransform.AxisSyncMode.None || m_SyncRotationAxis > NetworkTransform.AxisSyncMode.AxisXYZ)
			{
				m_SyncRotationAxis = NetworkTransform.AxisSyncMode.None;
			}
			if (m_MovementTheshold < 0f)
			{
				m_MovementTheshold = 0f;
			}
			if (m_VelocityThreshold < 0f)
			{
				m_VelocityThreshold = 0f;
			}
			if (m_SnapThreshold < 0f)
			{
				m_SnapThreshold = 0.01f;
			}
			if (m_InterpolateRotation < 0f)
			{
				m_InterpolateRotation = 0.01f;
			}
			if (m_InterpolateMovement < 0f)
			{
				m_InterpolateMovement = 0.01f;
			}
		}

		private void Awake()
		{
			m_RigidBody3D = base.GetComponent<Rigidbody>();
			m_RigidBody2D = base.GetComponent<Rigidbody2D>();
			m_CharacterController = base.GetComponent<CharacterController>();
			m_PrevPosition = base.transform.position;
			m_PrevRotation = base.transform.rotation;
			m_PrevVelocity = 0f;
			if (base.localPlayerAuthority)
			{
				m_LocalTransformWriter = new NetworkWriter();
			}
		}

		public override void OnStartServer()
		{
			m_LastClientSyncTime = 0f;
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
			switch (transformSyncMode)
			{
				case NetworkTransform.TransformSyncMode.SyncNone:
					return false;
				case NetworkTransform.TransformSyncMode.SyncTransform:
					SerializeModeTransform(writer);
					break;
				case NetworkTransform.TransformSyncMode.SyncRigidbody2D:
					SerializeMode2D(writer);
					break;
				case NetworkTransform.TransformSyncMode.SyncRigidbody3D:
					SerializeMode3D(writer);
					break;
				case NetworkTransform.TransformSyncMode.SyncCharacterController:
					SerializeModeCharacterController(writer);
					break;
			}
			return true;
		}

		private void SerializeModeTransform(NetworkWriter writer)
		{
			writer.Write(base.transform.position);
			if (m_SyncRotationAxis != NetworkTransform.AxisSyncMode.None)
			{
				NetworkTransform.SerializeRotation3D(writer, base.transform.rotation, syncRotationAxis, rotationSyncCompression);
			}
			m_PrevPosition = base.transform.position;
			m_PrevRotation = base.transform.rotation;
			m_PrevVelocity = 0f;
		}

		private void VerifySerializeComponentExists()
		{
			bool flag = false;
			Type type = null;
			NetworkTransform.TransformSyncMode transformSyncMode = this.transformSyncMode;
			if (transformSyncMode != NetworkTransform.TransformSyncMode.SyncCharacterController)
			{
				if (transformSyncMode != NetworkTransform.TransformSyncMode.SyncRigidbody2D)
				{
					if (transformSyncMode == NetworkTransform.TransformSyncMode.SyncRigidbody3D)
					{
						if (!m_RigidBody3D && !(m_RigidBody3D = base.GetComponent<Rigidbody>()))
						{
							flag = true;
							type = typeof(Rigidbody);
						}
					}
				}
				else if (!m_RigidBody2D && !(m_RigidBody2D = base.GetComponent<Rigidbody2D>()))
				{
					flag = true;
					type = typeof(Rigidbody2D);
				}
			}
			else if (!m_CharacterController && !(m_CharacterController = base.GetComponent<CharacterController>()))
			{
				flag = true;
				type = typeof(CharacterController);
			}
			if (flag && type != null)
			{
				throw new InvalidOperationException(string.Format("transformSyncMode set to {0} but no {1} component was found, did you call NetworkServer.Spawn on a prefab?", transformSyncMode, type.Name));
			}
		}

		private void SerializeMode3D(NetworkWriter writer)
		{
			VerifySerializeComponentExists();
			if (base.isServer && m_LastClientSyncTime != 0f)
			{
				writer.Write(m_TargetSyncPosition);
				NetworkTransform.SerializeVelocity3D(writer, m_TargetSyncVelocity, NetworkTransform.CompressionSyncMode.None);
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.SerializeRotation3D(writer, m_TargetSyncRotation3D, syncRotationAxis, rotationSyncCompression);
				}
			}
			else
			{
				writer.Write(m_RigidBody3D.position);
				NetworkTransform.SerializeVelocity3D(writer, m_RigidBody3D.velocity, NetworkTransform.CompressionSyncMode.None);
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.SerializeRotation3D(writer, m_RigidBody3D.rotation, syncRotationAxis, rotationSyncCompression);
				}
			}
			if (m_SyncSpin)
			{
				NetworkTransform.SerializeSpin3D(writer, m_RigidBody3D.angularVelocity, syncRotationAxis, rotationSyncCompression);
			}
			m_PrevPosition = m_RigidBody3D.position;
			m_PrevRotation = base.transform.rotation;
			m_PrevVelocity = m_RigidBody3D.velocity.sqrMagnitude;
		}

		private void SerializeModeCharacterController(NetworkWriter writer)
		{
			VerifySerializeComponentExists();
			if (base.isServer && m_LastClientSyncTime != 0f)
			{
				writer.Write(m_TargetSyncPosition);
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.SerializeRotation3D(writer, m_TargetSyncRotation3D, syncRotationAxis, rotationSyncCompression);
				}
			}
			else
			{
				writer.Write(base.transform.position);
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.SerializeRotation3D(writer, base.transform.rotation, syncRotationAxis, rotationSyncCompression);
				}
			}
			m_PrevPosition = base.transform.position;
			m_PrevRotation = base.transform.rotation;
			m_PrevVelocity = 0f;
		}

		private void SerializeMode2D(NetworkWriter writer)
		{
			VerifySerializeComponentExists();
			if (base.isServer && m_LastClientSyncTime != 0f)
			{
				writer.Write(m_TargetSyncPosition);
				NetworkTransform.SerializeVelocity2D(writer, m_TargetSyncVelocity, NetworkTransform.CompressionSyncMode.None);
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					float num = m_TargetSyncRotation2D % 360f;
					if (num < 0f)
					{
						num += 360f;
					}
					NetworkTransform.SerializeRotation2D(writer, num, rotationSyncCompression);
				}
			}
			else
			{
				writer.Write(m_RigidBody2D.position);
				NetworkTransform.SerializeVelocity2D(writer, m_RigidBody2D.velocity, NetworkTransform.CompressionSyncMode.None);
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					float num2 = m_RigidBody2D.rotation % 360f;
					if (num2 < 0f)
					{
						num2 += 360f;
					}
					NetworkTransform.SerializeRotation2D(writer, num2, rotationSyncCompression);
				}
			}
			if (m_SyncSpin)
			{
				NetworkTransform.SerializeSpin2D(writer, m_RigidBody2D.angularVelocity, rotationSyncCompression);
			}
			m_PrevPosition = m_RigidBody2D.position;
			m_PrevRotation = base.transform.rotation;
			m_PrevVelocity = m_RigidBody2D.velocity.sqrMagnitude;
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
				switch (transformSyncMode)
				{
					case NetworkTransform.TransformSyncMode.SyncNone:
						return;
					case NetworkTransform.TransformSyncMode.SyncTransform:
						UnserializeModeTransform(reader, initialState);
						break;
					case NetworkTransform.TransformSyncMode.SyncRigidbody3D:
						UnserializeMode3D(reader, initialState);
						break;
					case NetworkTransform.TransformSyncMode.SyncCharacterController:
						UnserializeModeCharacterController(reader, initialState);
						break;
				}
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
				Vector3 position = reader.ReadVector3();
				Vector3 zero = Vector3.zero;
				Quaternion rotation = Quaternion.identity;
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					rotation = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
				if (m_ClientMoveCallback3D(ref position, ref zero, ref rotation))
				{
					base.transform.position = position;
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						base.transform.rotation = rotation;
					}
				}
			}
			else
			{
				base.transform.position = reader.ReadVector3();
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					base.transform.rotation = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
			}
		}

		private void UnserializeMode3D(NetworkReader reader, bool initialState)
		{
			if (base.hasAuthority)
			{
				reader.ReadVector3();
				reader.ReadVector3();
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
				if (syncSpin)
				{
					NetworkTransform.UnserializeSpin3D(reader, syncRotationAxis, rotationSyncCompression);
				}
			}
			else
			{
				if (base.isServer && m_ClientMoveCallback3D != null)
				{
					Vector3 targetSyncPosition = reader.ReadVector3();
					Vector3 targetSyncVelocity = reader.ReadVector3();
					Quaternion targetSyncRotation3D = Quaternion.identity;
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						targetSyncRotation3D = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
					}
					if (!m_ClientMoveCallback3D(ref targetSyncPosition, ref targetSyncVelocity, ref targetSyncRotation3D))
					{
						return;
					}
					m_TargetSyncPosition = targetSyncPosition;
					m_TargetSyncVelocity = targetSyncVelocity;
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						m_TargetSyncRotation3D = targetSyncRotation3D;
					}
				}
				else
				{
					m_TargetSyncPosition = reader.ReadVector3();
					m_TargetSyncVelocity = reader.ReadVector3();
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						m_TargetSyncRotation3D = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
					}
				}
				if (syncSpin)
				{
					m_TargetSyncAngularVelocity3D = NetworkTransform.UnserializeSpin3D(reader, syncRotationAxis, rotationSyncCompression);
				}
				if (!(m_RigidBody3D == null))
				{
					if (base.isServer && !base.isClient)
					{
						m_RigidBody3D.MovePosition(m_TargetSyncPosition);
						m_RigidBody3D.MoveRotation(m_TargetSyncRotation3D);
						m_RigidBody3D.velocity = m_TargetSyncVelocity;
					}
					else if (GetNetworkSendInterval() == 0f)
					{
						m_RigidBody3D.MovePosition(m_TargetSyncPosition);
						m_RigidBody3D.velocity = m_TargetSyncVelocity;
						if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
						{
							m_RigidBody3D.MoveRotation(m_TargetSyncRotation3D);
						}
						if (syncSpin)
						{
							m_RigidBody3D.angularVelocity = m_TargetSyncAngularVelocity3D;
						}
					}
					else
					{
						float magnitude = (m_RigidBody3D.position - m_TargetSyncPosition).magnitude;
						if (magnitude > snapThreshold)
						{
							m_RigidBody3D.position = m_TargetSyncPosition;
							m_RigidBody3D.velocity = m_TargetSyncVelocity;
						}
						if (interpolateRotation == 0f && syncRotationAxis != NetworkTransform.AxisSyncMode.None)
						{
							m_RigidBody3D.rotation = m_TargetSyncRotation3D;
							if (syncSpin)
							{
								m_RigidBody3D.angularVelocity = m_TargetSyncAngularVelocity3D;
							}
						}
						if (m_InterpolateMovement == 0f)
						{
							m_RigidBody3D.position = m_TargetSyncPosition;
						}
						if (initialState && syncRotationAxis != NetworkTransform.AxisSyncMode.None)
						{
							m_RigidBody3D.rotation = m_TargetSyncRotation3D;
						}
					}
				}
			}
		}

		private void UnserializeModeCharacterController(NetworkReader reader, bool initialState)
		{
			if (base.hasAuthority)
			{
				reader.ReadVector3();
				if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
				{
					NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
				}
			}
			else
			{
				if (base.isServer && m_ClientMoveCallback3D != null)
				{
					Vector3 targetSyncPosition = reader.ReadVector3();
					Quaternion targetSyncRotation3D = Quaternion.identity;
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						targetSyncRotation3D = NetworkTransform.UnserializeRotation3D(reader, syncRotationAxis, rotationSyncCompression);
					}
					if (m_CharacterController == null)
					{
						return;
					}
					Vector3 velocity = m_CharacterController.velocity;
					if (!m_ClientMoveCallback3D(ref targetSyncPosition, ref velocity, ref targetSyncRotation3D))
					{
						return;
					}
					m_TargetSyncPosition = targetSyncPosition;
					m_TargetSyncVelocity = velocity;
					if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
					{
						m_TargetSyncRotation3D = targetSyncRotation3D;
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
				if (!(m_CharacterController == null))
				{
					Vector3 a = m_TargetSyncPosition - base.transform.position;
					Vector3 a2 = a / GetNetworkSendInterval();
					m_FixedPosDiff = a2 * Time.fixedDeltaTime;
					if (base.isServer && !base.isClient)
					{
						base.transform.position = m_TargetSyncPosition;
						base.transform.rotation = m_TargetSyncRotation3D;
					}
					else if (GetNetworkSendInterval() == 0f)
					{
						base.transform.position = m_TargetSyncPosition;
						if (syncRotationAxis != NetworkTransform.AxisSyncMode.None)
						{
							base.transform.rotation = m_TargetSyncRotation3D;
						}
					}
					else
					{
						float magnitude = (base.transform.position - m_TargetSyncPosition).magnitude;
						if (magnitude > snapThreshold)
						{
							base.transform.position = m_TargetSyncPosition;
						}
						if (interpolateRotation == 0f && syncRotationAxis != NetworkTransform.AxisSyncMode.None)
						{
							base.transform.rotation = m_TargetSyncRotation3D;
						}
						if (m_InterpolateMovement == 0f)
						{
							base.transform.position = m_TargetSyncPosition;
						}
						if (initialState && syncRotationAxis != NetworkTransform.AxisSyncMode.None)
						{
							base.transform.rotation = m_TargetSyncRotation3D;
						}
					}
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
							float num = (base.transform.position - m_PrevPosition).magnitude;
							if (num < movementTheshold)
							{
								num = Quaternion.Angle(m_PrevRotation, base.transform.rotation);
								if (num < movementTheshold)
								{
									if (!CheckVelocityChanged())
									{
										return;
									}
								}
							}
							base.SetDirtyBit(1U);
						}
					}
				}
			}
		}

		private bool CheckVelocityChanged()
		{
			NetworkTransform.TransformSyncMode transformSyncMode = this.transformSyncMode;
			bool result;
			if (transformSyncMode != NetworkTransform.TransformSyncMode.SyncRigidbody2D)
			{
				result = (transformSyncMode == NetworkTransform.TransformSyncMode.SyncRigidbody3D && (m_RigidBody3D && m_VelocityThreshold > 0f) && Mathf.Abs(m_RigidBody3D.velocity.sqrMagnitude - m_PrevVelocity) >= m_VelocityThreshold);
			}
			else
			{
				result = (m_RigidBody2D && m_VelocityThreshold > 0f && Mathf.Abs(m_RigidBody2D.velocity.sqrMagnitude - m_PrevVelocity) >= m_VelocityThreshold);
			}
			return result;
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
								switch (transformSyncMode)
								{
									case NetworkTransform.TransformSyncMode.SyncRigidbody3D:
										InterpolateTransformMode3D();
										break;
									case NetworkTransform.TransformSyncMode.SyncCharacterController:
										InterpolateTransformModeCharacterController();
										break;
								}
							}
						}
					}
				}
			}
		}

		private void InterpolateTransformMode3D()
		{
			if (m_InterpolateMovement != 0f)
			{
				Vector3 velocity = (m_TargetSyncPosition - m_RigidBody3D.position) * m_InterpolateMovement / GetNetworkSendInterval();
				m_RigidBody3D.velocity = velocity;
			}
			if (interpolateRotation != 0f)
			{
				m_RigidBody3D.MoveRotation(Quaternion.Slerp(m_RigidBody3D.rotation, m_TargetSyncRotation3D, Time.fixedDeltaTime * interpolateRotation));
			}
			m_TargetSyncPosition += m_TargetSyncVelocity * Time.fixedDeltaTime * 0.1f;
		}

		private void InterpolateTransformModeCharacterController()
		{
			if (!(m_FixedPosDiff == Vector3.zero) || !(m_TargetSyncRotation3D == base.transform.rotation))
			{
				if (m_InterpolateMovement != 0f)
				{
					m_CharacterController.Move(m_FixedPosDiff * m_InterpolateMovement);
				}
				if (interpolateRotation != 0f)
				{
					base.transform.rotation = Quaternion.Slerp(base.transform.rotation, m_TargetSyncRotation3D, Time.fixedDeltaTime * interpolateRotation * 10f);
				}
				if (Time.time - m_LastClientSyncTime > GetNetworkSendInterval())
				{
					m_FixedPosDiff = Vector3.zero;
					Vector3 motion = m_TargetSyncPosition - base.transform.position;
					m_CharacterController.Move(motion);
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
			float num;
			if (m_RigidBody3D != null)
			{
				num = (m_RigidBody3D.position - m_PrevPosition).magnitude;
			}
			else
			{
				num = (base.transform.position - m_PrevPosition).magnitude;
			}
			bool result;
			if (num > 1E-05f)
			{
				result = true;
			}
			else
			{
				if (m_RigidBody3D != null)
				{
					num = Quaternion.Angle(m_RigidBody3D.rotation, m_PrevRotation);
				}
				else if (m_RigidBody2D != null)
				{
					num = Math.Abs(m_RigidBody2D.rotation - m_PrevRotation2D);
				}
				else
				{
					num = Quaternion.Angle(base.transform.rotation, m_PrevRotation);
				}
				if (num > 1E-05f)
				{
					result = true;
				}
				else
				{
					if (m_RigidBody3D != null)
					{
						num = Mathf.Abs(m_RigidBody3D.velocity.sqrMagnitude - m_PrevVelocity);
					}
					else if (m_RigidBody2D != null)
					{
						num = Mathf.Abs(m_RigidBody2D.velocity.sqrMagnitude - m_PrevVelocity);
					}
					result = (num > 1E-05f);
				}
			}
			return result;
		}

		[Client]
		private void SendTransform()
		{
			if (HasMoved() && ClientScene.readyConnection != null)
			{
				m_LocalTransformWriter.StartMessage(6);
				m_LocalTransformWriter.Write(base.netId);
				switch (transformSyncMode)
				{
					case NetworkTransform.TransformSyncMode.SyncNone:
						return;
					case NetworkTransform.TransformSyncMode.SyncTransform:
						SerializeModeTransform(m_LocalTransformWriter);
						break;
					case NetworkTransform.TransformSyncMode.SyncRigidbody2D:
						SerializeMode2D(m_LocalTransformWriter);
						break;
					case NetworkTransform.TransformSyncMode.SyncRigidbody3D:
						SerializeMode3D(m_LocalTransformWriter);
						break;
					case NetworkTransform.TransformSyncMode.SyncCharacterController:
						SerializeModeCharacterController(m_LocalTransformWriter);
						break;
				}
				if (m_RigidBody3D != null)
				{
					m_PrevPosition = m_RigidBody3D.position;
					m_PrevRotation = m_RigidBody3D.rotation;
					m_PrevVelocity = m_RigidBody3D.velocity.sqrMagnitude;
				}
				else if (m_RigidBody2D != null)
				{
					m_PrevPosition = m_RigidBody2D.position;
					m_PrevRotation2D = m_RigidBody2D.rotation;
					m_PrevVelocity = m_RigidBody2D.velocity.sqrMagnitude;
				}
				else
				{
					m_PrevPosition = base.transform.position;
					m_PrevRotation = base.transform.rotation;
				}
				m_LocalTransformWriter.FinishMessage();
				ClientScene.readyConnection.SendWriter(m_LocalTransformWriter, GetNetworkChannel());
			}
		}

		public static void HandleTransform(NetworkMessage netMsg)
		{
			NetworkInstanceId networkInstanceId = netMsg.reader.ReadNetworkId();
			GameObject gameObject = NetworkServer.FindLocalObject(networkInstanceId);
			if (gameObject == null)
			{
				if (LogFilter.logError)
				{
					Debug.LogError("Received NetworkTransform data for GameObject that doesn't exist");
				}
			}
			else
			{
				NetworkTransform component = gameObject.GetComponent<NetworkTransform>();
				if (component == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("HandleTransform null target");
					}
				}
				else if (!component.localPlayerAuthority)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("HandleTransform no localPlayerAuthority");
					}
				}
				else if (netMsg.conn.clientOwnedObjects == null)
				{
					if (LogFilter.logError)
					{
						Debug.LogError("HandleTransform object not owned by connection");
					}
				}
				else if (netMsg.conn.clientOwnedObjects.Contains(networkInstanceId))
				{
					switch (component.transformSyncMode)
					{
						case NetworkTransform.TransformSyncMode.SyncNone:
							return;
						case NetworkTransform.TransformSyncMode.SyncTransform:
							component.UnserializeModeTransform(netMsg.reader, false);
							break;
						case NetworkTransform.TransformSyncMode.SyncRigidbody3D:
							component.UnserializeMode3D(netMsg.reader, false);
							break;
						case NetworkTransform.TransformSyncMode.SyncCharacterController:
							component.UnserializeModeCharacterController(netMsg.reader, false);
							break;
					}
					component.m_LastClientSyncTime = Time.time;
				}
				else if (LogFilter.logWarn)
				{
					Debug.LogWarning("HandleTransform netId:" + networkInstanceId + " is not for a valid player");
				}
			}
		}

		private static void WriteAngle(NetworkWriter writer, float angle, NetworkTransform.CompressionSyncMode compression)
		{
			if (compression != NetworkTransform.CompressionSyncMode.None)
			{
				if (compression != NetworkTransform.CompressionSyncMode.Low)
				{
					if (compression == NetworkTransform.CompressionSyncMode.High)
					{
						writer.Write((short)angle);
					}
				}
				else
				{
					writer.Write((short)angle);
				}
			}
			else
			{
				writer.Write(angle);
			}
		}

		private static float ReadAngle(NetworkReader reader, NetworkTransform.CompressionSyncMode compression)
		{
			float result;
			if (compression != NetworkTransform.CompressionSyncMode.None)
			{
				if (compression != NetworkTransform.CompressionSyncMode.Low)
				{
					if (compression != NetworkTransform.CompressionSyncMode.High)
					{
						result = 0f;
					}
					else
					{
						result = (float)reader.ReadInt16();
					}
				}
				else
				{
					result = (float)reader.ReadInt16();
				}
			}
			else
			{
				result = reader.ReadSingle();
			}
			return result;
		}

		public static void SerializeVelocity3D(NetworkWriter writer, Vector3 velocity, NetworkTransform.CompressionSyncMode compression)
		{
			writer.Write(velocity);
		}

		public static void SerializeVelocity2D(NetworkWriter writer, Vector2 velocity, NetworkTransform.CompressionSyncMode compression)
		{
			writer.Write(velocity);
		}

		public static void SerializeRotation3D(NetworkWriter writer, Quaternion rot, NetworkTransform.AxisSyncMode mode, NetworkTransform.CompressionSyncMode compression)
		{
			switch (mode)
			{
				case NetworkTransform.AxisSyncMode.AxisX:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.x, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisY:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.y, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisZ:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.z, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisXY:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.x, compression);
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.y, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisXZ:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.x, compression);
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.z, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisYZ:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.y, compression);
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.z, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisXYZ:
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.x, compression);
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.y, compression);
					NetworkTransform.WriteAngle(writer, rot.eulerAngles.z, compression);
					break;
			}
		}

		public static void SerializeRotation2D(NetworkWriter writer, float rot, NetworkTransform.CompressionSyncMode compression)
		{
			NetworkTransform.WriteAngle(writer, rot, compression);
		}

		public static void SerializeSpin3D(NetworkWriter writer, Vector3 angularVelocity, NetworkTransform.AxisSyncMode mode, NetworkTransform.CompressionSyncMode compression)
		{
			switch (mode)
			{
				case NetworkTransform.AxisSyncMode.AxisX:
					NetworkTransform.WriteAngle(writer, angularVelocity.x, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisY:
					NetworkTransform.WriteAngle(writer, angularVelocity.y, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisZ:
					NetworkTransform.WriteAngle(writer, angularVelocity.z, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisXY:
					NetworkTransform.WriteAngle(writer, angularVelocity.x, compression);
					NetworkTransform.WriteAngle(writer, angularVelocity.y, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisXZ:
					NetworkTransform.WriteAngle(writer, angularVelocity.x, compression);
					NetworkTransform.WriteAngle(writer, angularVelocity.z, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisYZ:
					NetworkTransform.WriteAngle(writer, angularVelocity.y, compression);
					NetworkTransform.WriteAngle(writer, angularVelocity.z, compression);
					break;
				case NetworkTransform.AxisSyncMode.AxisXYZ:
					NetworkTransform.WriteAngle(writer, angularVelocity.x, compression);
					NetworkTransform.WriteAngle(writer, angularVelocity.y, compression);
					NetworkTransform.WriteAngle(writer, angularVelocity.z, compression);
					break;
			}
		}

		public static void SerializeSpin2D(NetworkWriter writer, float angularVelocity, NetworkTransform.CompressionSyncMode compression)
		{
			NetworkTransform.WriteAngle(writer, angularVelocity, compression);
		}

		public static Vector3 UnserializeVelocity3D(NetworkReader reader, NetworkTransform.CompressionSyncMode compression)
		{
			return reader.ReadVector3();
		}

		public static Vector3 UnserializeVelocity2D(NetworkReader reader, NetworkTransform.CompressionSyncMode compression)
		{
			return reader.ReadVector2();
		}

		public static Quaternion UnserializeRotation3D(NetworkReader reader, NetworkTransform.AxisSyncMode mode, NetworkTransform.CompressionSyncMode compression)
		{
			Quaternion identity = Quaternion.identity;
			Vector3 zero = Vector3.zero;
			switch (mode)
			{
				case NetworkTransform.AxisSyncMode.AxisX:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), 0f, 0f);
					identity.eulerAngles = zero;
					break;
				case NetworkTransform.AxisSyncMode.AxisY:
					zero.Set(0f, NetworkTransform.ReadAngle(reader, compression), 0f);
					identity.eulerAngles = zero;
					break;
				case NetworkTransform.AxisSyncMode.AxisZ:
					zero.Set(0f, 0f, NetworkTransform.ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;
				case NetworkTransform.AxisSyncMode.AxisXY:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression), 0f);
					identity.eulerAngles = zero;
					break;
				case NetworkTransform.AxisSyncMode.AxisXZ:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), 0f, NetworkTransform.ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;
				case NetworkTransform.AxisSyncMode.AxisYZ:
					zero.Set(0f, NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;
				case NetworkTransform.AxisSyncMode.AxisXYZ:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression));
					identity.eulerAngles = zero;
					break;
			}
			return identity;
		}

		public static float UnserializeRotation2D(NetworkReader reader, NetworkTransform.CompressionSyncMode compression)
		{
			return NetworkTransform.ReadAngle(reader, compression);
		}

		public static Vector3 UnserializeSpin3D(NetworkReader reader, NetworkTransform.AxisSyncMode mode, NetworkTransform.CompressionSyncMode compression)
		{
			Vector3 zero = Vector3.zero;
			switch (mode)
			{
				case NetworkTransform.AxisSyncMode.AxisX:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), 0f, 0f);
					break;
				case NetworkTransform.AxisSyncMode.AxisY:
					zero.Set(0f, NetworkTransform.ReadAngle(reader, compression), 0f);
					break;
				case NetworkTransform.AxisSyncMode.AxisZ:
					zero.Set(0f, 0f, NetworkTransform.ReadAngle(reader, compression));
					break;
				case NetworkTransform.AxisSyncMode.AxisXY:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression), 0f);
					break;
				case NetworkTransform.AxisSyncMode.AxisXZ:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), 0f, NetworkTransform.ReadAngle(reader, compression));
					break;
				case NetworkTransform.AxisSyncMode.AxisYZ:
					zero.Set(0f, NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression));
					break;
				case NetworkTransform.AxisSyncMode.AxisXYZ:
					zero.Set(NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression), NetworkTransform.ReadAngle(reader, compression));
					break;
			}
			return zero;
		}

		public static float UnserializeSpin2D(NetworkReader reader, NetworkTransform.CompressionSyncMode compression)
		{
			return NetworkTransform.ReadAngle(reader, compression);
		}

		public override int GetNetworkChannel()
		{
			return 1;
		}

		public override float GetNetworkSendInterval()
		{
			return m_SendInterval;
		}

		public override void OnStartAuthority()
		{
			m_LastClientSyncTime = 0f;
		}

		[SerializeField]
		private NetworkTransform.TransformSyncMode m_TransformSyncMode = NetworkTransform.TransformSyncMode.SyncNone;

		[SerializeField]
		private float m_SendInterval = 0.1f;

		[SerializeField]
		private NetworkTransform.AxisSyncMode m_SyncRotationAxis = NetworkTransform.AxisSyncMode.AxisXYZ;

		[SerializeField]
		private NetworkTransform.CompressionSyncMode m_RotationSyncCompression = NetworkTransform.CompressionSyncMode.None;

		[SerializeField]
		private bool m_SyncSpin;

		[SerializeField]
		private float m_MovementTheshold = 0.001f;

		[SerializeField]
		private float m_VelocityThreshold = 0.0001f;

		[SerializeField]
		private float m_SnapThreshold = 5f;

		[SerializeField]
		private float m_InterpolateRotation = 1f;

		[SerializeField]
		private float m_InterpolateMovement = 1f;

		[SerializeField]
		private NetworkTransform.ClientMoveCallback3D m_ClientMoveCallback3D;

		[SerializeField]
		private NetworkTransform.ClientMoveCallback2D m_ClientMoveCallback2D;

		private Rigidbody m_RigidBody3D;

		private Rigidbody2D m_RigidBody2D;

		private CharacterController m_CharacterController;

		private bool m_Grounded = true;

		private Vector3 m_TargetSyncPosition;

		private Vector3 m_TargetSyncVelocity;

		private Vector3 m_FixedPosDiff;

		private Quaternion m_TargetSyncRotation3D;

		private Vector3 m_TargetSyncAngularVelocity3D;

		private float m_TargetSyncRotation2D;

		private float m_TargetSyncAngularVelocity2D;

		private float m_LastClientSyncTime;

		private float m_LastClientSendTime;

		private Vector3 m_PrevPosition;

		private Quaternion m_PrevRotation;

		private float m_PrevRotation2D;

		private float m_PrevVelocity;

		private const float k_LocalMovementThreshold = 1E-05f;

		private const float k_LocalRotationThreshold = 1E-05f;

		private const float k_LocalVelocityThreshold = 1E-05f;

		private const float k_MoveAheadRatio = 0.1f;

		private NetworkWriter m_LocalTransformWriter;

		public enum TransformSyncMode
		{
			SyncNone,
			SyncTransform,
			SyncRigidbody2D,
			SyncRigidbody3D,
			SyncCharacterController
		}

		public enum AxisSyncMode
		{
			None,
			AxisX,
			AxisY,
			AxisZ,
			AxisXY,
			AxisXZ,
			AxisYZ,
			AxisXYZ
		}

		public enum CompressionSyncMode
		{
			None,
			Low,
			High
		}

		public delegate bool ClientMoveCallback3D(ref Vector3 position, ref Vector3 velocity, ref Quaternion rotation);

		public delegate bool ClientMoveCallback2D(ref Vector2 position, ref Vector2 velocity, ref float rotation);
	}
}
