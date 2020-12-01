using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Defines parameters of channels.</para>
	/// </summary>
	[Serializable]
	public class ChannelQOS
	{
		/// <summary>
		///   <para>UnderlyingModel.MemDoc.MemDocModel.</para>
		/// </summary>
		/// <param name="value">Requested type of quality of service (default Unreliable).</param>
		/// <param name="channel">Copy constructor.</param>
		public ChannelQOS(QosType value)
		{
			m_Type = value;
		}

		/// <summary>
		///   <para>UnderlyingModel.MemDoc.MemDocModel.</para>
		/// </summary>
		/// <param name="value">Requested type of quality of service (default Unreliable).</param>
		/// <param name="channel">Copy constructor.</param>
		public ChannelQOS()
		{
			m_Type = QosType.Unreliable;
		}

		/// <summary>
		///   <para>UnderlyingModel.MemDoc.MemDocModel.</para>
		/// </summary>
		/// <param name="value">Requested type of quality of service (default Unreliable).</param>
		/// <param name="channel">Copy constructor.</param>
		public ChannelQOS(ChannelQOS channel)
		{
			if (channel == null)
			{
				throw new NullReferenceException("channel is not defined");
			}
			m_Type = channel.m_Type;
		}

		/// <summary>
		///   <para>Channel quality of service.</para>
		/// </summary>
		public QosType QOS
		{
			get
			{
				return m_Type;
			}
		}

		[SerializeField]
		internal QosType m_Type;
	}
}
