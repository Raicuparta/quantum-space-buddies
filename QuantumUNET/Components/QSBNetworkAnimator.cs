using QuantumUNET.Messages;
using QuantumUNET.Transport;
using UnityEngine;

namespace QuantumUNET.Components
{
	public class QSBNetworkAnimator : QSBNetworkBehaviour
	{
		private static readonly QSBAnimationMessage AnimationMessage = new QSBAnimationMessage();
		private static readonly QSBAnimationParametersMessage ParametersMessage = new QSBAnimationParametersMessage();
		private static readonly QSBAnimationTriggerMessage TriggersMessage = new QSBAnimationTriggerMessage();

		private Animator m_Animator;
		private uint m_ParameterSendBits;
		private int m_AnimationHash;
		private int m_TransitionHash;
		private QSBNetworkWriter m_ParameterWriter;
		private float m_SendTimer;

		public Animator animator
		{
			get => m_Animator;
			set
			{
				m_Animator = value;
				m_ParameterSendBits = 0U;
			}
		}

		public void SetParameterAutoSend(int index, bool value)
		{
			if (value)
			{
				m_ParameterSendBits |= (uint)(1 << index);
			}
			else
			{
				m_ParameterSendBits &= (uint)~(1 << index);
			}
		}

		public bool GetParameterAutoSend(int index) =>
			((int)m_ParameterSendBits & (1 << index)) != 0;

		public override void OnStartAuthority() =>
			m_ParameterWriter = new QSBNetworkWriter();

		public void FixedUpdate()
		{
			if (m_ParameterWriter == null)
			{
				return;
			}
			CheckSendRate();
			if (!CheckAnimStateChanged(out var stateHash, out var normalizedTime))
			{
				return;
			}
			var animationMessage = new QSBAnimationMessage
			{
				netId = NetId,
				stateHash = stateHash,
				normalizedTime = normalizedTime
			};

			m_ParameterWriter.SeekZero();
			WriteParameters(m_ParameterWriter, false);
			animationMessage.parameters = m_ParameterWriter.ToArray();

			if (HasAuthority || QSBClientScene.readyConnection != null)
			{
				QSBClientScene.readyConnection.Send(40, animationMessage);
			}
			else
			{
				if (!IsServer || LocalPlayerAuthority)
				{
					return;
				}
				QSBNetworkServer.SendToReady(gameObject, 40, animationMessage);
			}
		}

		private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime)
		{
			stateHash = 0;
			normalizedTime = 0.0f;
			if (m_Animator.IsInTransition(0))
			{
				var animatorTransitionInfo = m_Animator.GetAnimatorTransitionInfo(0);
				if (animatorTransitionInfo.fullPathHash == m_TransitionHash)
				{
					return false;
				}
				m_TransitionHash = animatorTransitionInfo.fullPathHash;
				m_AnimationHash = 0;
				return true;
			}
			var animatorStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
			if (animatorStateInfo.fullPathHash == m_AnimationHash)
			{
				return false;
			}
			if (m_AnimationHash != 0)
			{
				stateHash = animatorStateInfo.fullPathHash;
				normalizedTime = animatorStateInfo.normalizedTime;
			}
			m_TransitionHash = 0;
			m_AnimationHash = animatorStateInfo.fullPathHash;
			return true;
		}

		private void CheckSendRate()
		{
			if (GetNetworkSendInterval() == 0.0 || m_SendTimer >= Time.time)
			{
				return;
			}
			m_SendTimer = Time.time + GetNetworkSendInterval();
			var parametersMessage = new QSBAnimationParametersMessage
			{
				netId = NetId
			};
			m_ParameterWriter.SeekZero();
			WriteParameters(m_ParameterWriter, true);
			parametersMessage.parameters = m_ParameterWriter.ToArray();
			if (HasAuthority && QSBClientScene.readyConnection != null)
			{
				QSBClientScene.readyConnection.Send(41, parametersMessage);
			}
			else
			{
				if (!IsServer || LocalPlayerAuthority)
				{
					return;
				}

				QSBNetworkServer.SendToReady(gameObject, 41, parametersMessage);
			}
		}

		internal void HandleAnimMsg(QSBAnimationMessage msg, QSBNetworkReader reader)
		{
			if (HasAuthority)
			{
				return;
			}
			if (msg.stateHash != 0)
			{
				m_Animator.Play(msg.stateHash, 0, msg.normalizedTime);
			}
			ReadParameters(reader, false);
		}

		internal void HandleAnimParamsMsg(QSBAnimationParametersMessage msg, QSBNetworkReader reader)
		{
			if (HasAuthority)
			{
				return;
			}
			ReadParameters(reader, true);
		}

		internal void HandleAnimTriggerMsg(int hash) => m_Animator.SetTrigger(hash);

		private void WriteParameters(QSBNetworkWriter writer, bool autoSend)
		{
			for (var index = 0; index < m_Animator.parameters.Length; ++index)
			{
				if (!autoSend || GetParameterAutoSend(index))
				{
					var parameter = m_Animator.parameters[index];
					switch (parameter.type)
					{
						case AnimatorControllerParameterType.Int:
							writer.WritePackedUInt32((uint)m_Animator.GetInteger(parameter.nameHash));
							break;

						case AnimatorControllerParameterType.Float:
							writer.Write(m_Animator.GetFloat(parameter.nameHash));
							break;

						case AnimatorControllerParameterType.Bool:
							writer.Write(m_Animator.GetBool(parameter.nameHash));
							break;
					}
				}
			}
		}

		private void ReadParameters(QSBNetworkReader reader, bool autoSend)
		{
			for (var index = 0; index < m_Animator.parameters.Length; ++index)
			{
				if (!autoSend || GetParameterAutoSend(index))
				{
					var parameter = m_Animator.parameters[index];
					if (reader.Length == reader.Position)
					{
						return;
					}
					switch (parameter.type)
					{
						case AnimatorControllerParameterType.Int:
							var num = (int)reader.ReadPackedUInt32();
							m_Animator.SetInteger(parameter.nameHash, num);
							break;

						case AnimatorControllerParameterType.Float:
							var single = reader.ReadSingle();
							m_Animator.SetFloat(parameter.nameHash, single);
							break;

						case AnimatorControllerParameterType.Bool:
							var flag = reader.ReadBoolean();
							m_Animator.SetBool(parameter.nameHash, flag);
							break;
					}
				}
			}
		}

		public override bool OnSerialize(QSBNetworkWriter writer, bool forceAll)
		{
			if (!forceAll)
			{
				return false;
			}
			if (m_Animator.IsInTransition(0))
			{
				var animatorStateInfo = m_Animator.GetNextAnimatorStateInfo(0);
				writer.Write(animatorStateInfo.fullPathHash);
				writer.Write(animatorStateInfo.normalizedTime);
			}
			else
			{
				var animatorStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
				writer.Write(animatorStateInfo.fullPathHash);
				writer.Write(animatorStateInfo.normalizedTime);
			}
			WriteParameters(writer, false);
			return true;
		}

		public override void OnDeserialize(QSBNetworkReader reader, bool initialState)
		{
			if (!initialState)
			{
				return;
			}
			var stateNameHash = reader.ReadInt32();
			var normalizedTime = reader.ReadSingle();
			ReadParameters(reader, false);
			m_Animator.Play(stateNameHash, 0, normalizedTime);
		}

		public void SetTrigger(string triggerName) => SetTrigger(Animator.StringToHash(triggerName));

		public void SetTrigger(int hash)
		{
			var animationTriggerMessage = new QSBAnimationTriggerMessage
			{
				netId = NetId,
				hash = hash
			};
			if (HasAuthority && LocalPlayerAuthority)
			{
				if (QSBNetworkClient.allClients.Count <= 0)
				{
					return;
				}
				var readyConnection = QSBClientScene.readyConnection;
				if (readyConnection == null)
				{
					return;
				}
				readyConnection.Send(42, animationTriggerMessage);
			}
			else
			{
				if (!IsServer || LocalPlayerAuthority)
				{
					return;
				}
				QSBNetworkServer.SendToReady(gameObject, 42, animationTriggerMessage);
			}
		}

		internal static void OnAnimationServerMessage(QSBNetworkMessage netMsg)
		{
			netMsg.ReadMessage(AnimationMessage);
			var localObject = QSBNetworkServer.FindLocalObject(AnimationMessage.netId);
			if (localObject == null)
			{
				return;
			}
			var component = localObject.GetComponent<QSBNetworkAnimator>();
			var reader = new QSBNetworkReader(AnimationMessage.parameters);
			component?.HandleAnimMsg(AnimationMessage, reader);
			QSBNetworkServer.SendToReady(localObject, 40, AnimationMessage);
		}

		internal static void OnAnimationParametersServerMessage(QSBNetworkMessage netMsg)
		{
			netMsg.ReadMessage(ParametersMessage);
			var localObject = QSBNetworkServer.FindLocalObject(ParametersMessage.netId);
			if (localObject == null)
			{
				return;
			}
			var component = localObject.GetComponent<QSBNetworkAnimator>();
			var reader = new QSBNetworkReader(ParametersMessage.parameters);
			component?.HandleAnimParamsMsg(ParametersMessage, reader);
			QSBNetworkServer.SendToReady(localObject, 41, ParametersMessage);
		}

		internal static void OnAnimationTriggerServerMessage(QSBNetworkMessage netMsg)
		{
			netMsg.ReadMessage(TriggersMessage);
			var localObject = QSBNetworkServer.FindLocalObject(TriggersMessage.netId);
			if (localObject == null)
			{
				return;
			}
			var component = localObject.GetComponent<QSBNetworkAnimator>();
			component?.HandleAnimTriggerMsg(TriggersMessage.hash);
			QSBNetworkServer.SendToReady(localObject, 42, TriggersMessage);
		}

		internal static void OnAnimationClientMessage(QSBNetworkMessage netMsg)
		{
			netMsg.ReadMessage(AnimationMessage);
			var localObject = QSBClientScene.FindLocalObject(AnimationMessage.netId);
			if (localObject == null)
			{
				return;
			}

			var component = localObject.GetComponent<QSBNetworkAnimator>();
			if (component == null)
			{
				return;
			}

			var reader = new QSBNetworkReader(AnimationMessage.parameters);
			component.HandleAnimMsg(AnimationMessage, reader);
		}

		internal static void OnAnimationParametersClientMessage(QSBNetworkMessage netMsg)
		{
			netMsg.ReadMessage(ParametersMessage);
			var localObject = QSBClientScene.FindLocalObject(ParametersMessage.netId);
			if (localObject == null)
			{
				return;
			}

			var component = localObject.GetComponent<QSBNetworkAnimator>();
			if (component == null)
			{
				return;
			}

			var reader = new QSBNetworkReader(ParametersMessage.parameters);
			component.HandleAnimParamsMsg(ParametersMessage, reader);
		}

		internal static void OnAnimationTriggerClientMessage(QSBNetworkMessage netMsg)
		{
			netMsg.ReadMessage(TriggersMessage);
			var localObject = QSBClientScene.FindLocalObject(TriggersMessage.netId);
			if (localObject == null)
			{
				return;
			}

			var component = localObject.GetComponent<QSBNetworkAnimator>();
			if (component == null)
			{
				return;
			}

			component.HandleAnimTriggerMsg(TriggersMessage.hash);
		}
	}
}