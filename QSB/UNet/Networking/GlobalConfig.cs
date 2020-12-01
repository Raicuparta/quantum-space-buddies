using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Defines global paramters for network library.</para>
	/// </summary>
	[Serializable]
	public class GlobalConfig
	{
		/// <summary>
		///   <para>Create new global config object.</para>
		/// </summary>
		public GlobalConfig()
		{
			m_ThreadAwakeTimeout = 1U;
			m_ReactorModel = ReactorModel.SelectReactor;
			m_ReactorMaximumReceivedMessages = 1024;
			m_ReactorMaximumSentMessages = 1024;
			m_MaxPacketSize = 2000;
			m_MaxHosts = 16;
			m_ThreadPoolSize = 1;
			m_MinTimerTimeout = 1U;
			m_MaxTimerTimeout = 12000U;
			m_MinNetSimulatorTimeout = 1U;
			m_MaxNetSimulatorTimeout = 12000U;
		}

		/// <summary>
		///   <para>Defines (1) for select reactor, minimum time period, when system will check if there are any messages for send (2) for fixrate reactor, minimum interval of time, when system will check for sending and receiving messages.</para>
		/// </summary>
		public uint ThreadAwakeTimeout
		{
			get
			{
				return m_ThreadAwakeTimeout;
			}
			set
			{
				if (value == 0U)
				{
					throw new ArgumentOutOfRangeException("Minimal thread awake timeout should be > 0");
				}
				m_ThreadAwakeTimeout = value;
			}
		}

		/// <summary>
		///   <para>Defines reactor model for the network library.</para>
		/// </summary>
		public ReactorModel ReactorModel
		{
			get
			{
				return m_ReactorModel;
			}
			set
			{
				m_ReactorModel = value;
			}
		}

		/// <summary>
		///   <para>This property determines the initial size of the queue that holds messages received by Unity Multiplayer before they are processed.</para>
		/// </summary>
		public ushort ReactorMaximumReceivedMessages
		{
			get
			{
				return m_ReactorMaximumReceivedMessages;
			}
			set
			{
				m_ReactorMaximumReceivedMessages = value;
			}
		}

		/// <summary>
		///   <para>Defines the initial size of the send queue. Messages are placed in this queue ready to be sent in packets to their destination.</para>
		/// </summary>
		public ushort ReactorMaximumSentMessages
		{
			get
			{
				return m_ReactorMaximumSentMessages;
			}
			set
			{
				m_ReactorMaximumSentMessages = value;
			}
		}

		/// <summary>
		///   <para>Defines maximum possible packet size in bytes for all network connections.</para>
		/// </summary>
		public ushort MaxPacketSize
		{
			get
			{
				return m_MaxPacketSize;
			}
			set
			{
				m_MaxPacketSize = value;
			}
		}

		/// <summary>
		///   <para>Defines how many hosts you can use. Default Value = 16. Max value = 128.</para>
		/// </summary>
		public ushort MaxHosts
		{
			get
			{
				return m_MaxHosts;
			}
			set
			{
				if (value == 0)
				{
					throw new ArgumentOutOfRangeException("MaxHosts", "Maximum hosts number should be > 0");
				}
				if (value > 128)
				{
					throw new ArgumentOutOfRangeException("MaxHosts", "Maximum hosts number should be <= " + 128.ToString());
				}
				m_MaxHosts = value;
			}
		}

		/// <summary>
		///   <para>Defines how many worker threads are available to handle incoming and outgoing messages.</para>
		/// </summary>
		public byte ThreadPoolSize
		{
			get
			{
				return m_ThreadPoolSize;
			}
			set
			{
				m_ThreadPoolSize = value;
			}
		}

		/// <summary>
		///   <para>Defines the minimum timeout in milliseconds recognised by the system. The default value is 1 ms.</para>
		/// </summary>
		public uint MinTimerTimeout
		{
			get
			{
				return m_MinTimerTimeout;
			}
			set
			{
				if (value > MaxTimerTimeout)
				{
					throw new ArgumentOutOfRangeException("MinTimerTimeout should be < MaxTimerTimeout");
				}
				if (value == 0U)
				{
					throw new ArgumentOutOfRangeException("MinTimerTimeout should be > 0");
				}
				m_MinTimerTimeout = value;
			}
		}

		/// <summary>
		///   <para>Defines the maximum timeout in milliseconds for any configuration. The default value is 12 seconds (12000ms).</para>
		/// </summary>
		public uint MaxTimerTimeout
		{
			get
			{
				return m_MaxTimerTimeout;
			}
			set
			{
				if (value == 0U)
				{
					throw new ArgumentOutOfRangeException("MaxTimerTimeout should be > 0");
				}
				if (value > 12000U)
				{
					throw new ArgumentOutOfRangeException("MaxTimerTimeout should be <=" + 12000U.ToString());
				}
				m_MaxTimerTimeout = value;
			}
		}

		/// <summary>
		///   <para>Deprecated. Defines the minimal timeout for network simulator. You cannot set up any delay less than this value. See Also: MinTimerTimeout.</para>
		/// </summary>
		public uint MinNetSimulatorTimeout
		{
			get
			{
				return m_MinNetSimulatorTimeout;
			}
			set
			{
				if (value > MaxNetSimulatorTimeout)
				{
					throw new ArgumentOutOfRangeException("MinNetSimulatorTimeout should be < MaxTimerTimeout");
				}
				if (value == 0U)
				{
					throw new ArgumentOutOfRangeException("MinNetSimulatorTimeout should be > 0");
				}
				m_MinNetSimulatorTimeout = value;
			}
		}

		/// <summary>
		///   <para>Deprecated. Defines maximum delay for network simulator. See Also: MaxTimerTimeout.</para>
		/// </summary>
		public uint MaxNetSimulatorTimeout
		{
			get
			{
				return m_MaxNetSimulatorTimeout;
			}
			set
			{
				if (value == 0U)
				{
					throw new ArgumentOutOfRangeException("MaxNetSimulatorTimeout should be > 0");
				}
				if (value > 12000U)
				{
					throw new ArgumentOutOfRangeException("MaxNetSimulatorTimeout should be <=" + 12000U.ToString());
				}
				m_MaxNetSimulatorTimeout = value;
			}
		}

		private const uint g_MaxTimerTimeout = 12000U;

		private const uint g_MaxNetSimulatorTimeout = 12000U;

		private const ushort g_MaxHosts = 128;

		[SerializeField]
		private uint m_ThreadAwakeTimeout;

		[SerializeField]
		private ReactorModel m_ReactorModel;

		[SerializeField]
		private ushort m_ReactorMaximumReceivedMessages;

		[SerializeField]
		private ushort m_ReactorMaximumSentMessages;

		[SerializeField]
		private ushort m_MaxPacketSize;

		[SerializeField]
		private ushort m_MaxHosts;

		[SerializeField]
		private byte m_ThreadPoolSize;

		[SerializeField]
		private uint m_MinTimerTimeout;

		[SerializeField]
		private uint m_MaxTimerTimeout;

		[SerializeField]
		private uint m_MinNetSimulatorTimeout;

		[SerializeField]
		private uint m_MaxNetSimulatorTimeout;
	}
}
