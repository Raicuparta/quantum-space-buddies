using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace QSB.UNet.Networking
{
	internal sealed class HostTopologyInternal : IDisposable
	{
		object instance;
		public HostTopology storedTopology;

		public HostTopologyInternal(HostTopology topology)
		{
			var assembly = Assembly.LoadFile($@"{QSB.Helper.OwmlConfig.GamePath}OuterWilds_Data\Managed\UnityEngine.UNETModule.dll");
			var type = assembly.GetType("UnityEngine.Networking.HostTopologyInternal");
			instance = Activator.CreateInstance(type, topology.ToUnity());

			storedTopology = topology;

			var config = new ConnectionConfigInternal(topology.DefaultConfig);
			InitWrapper(config, topology);
			for (int i = 1; i <= topology.SpecialConnectionConfigsCount; i++)
			{
				var specialConnectionConfig = topology.GetSpecialConnectionConfig(i);
				var internalConfig = new ConnectionConfigInternal(specialConnectionConfig);
				AddSpecialConnectionConfig(specialConnectionConfig);
			}
			InitOtherParameters(topology);
		}

		public void InitWrapper(ConnectionConfigInternal config, HostTopology topology)
		{
			var assembly = Assembly.LoadFile($@"{QSB.Helper.OwmlConfig.GamePath}OuterWilds_Data\Managed\UnityEngine.UNETModule.dll");
			var type = assembly.GetType("UnityEngine.Networking.ConnectionConfigInternal");
			var internalInstance = Activator.CreateInstance(type, topology.DefaultConfig.ToUnity());
			instance.GetType().GetMethod("InitWrapper", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { internalInstance, topology.MaxDefaultConnections});
		}

		private int AddSpecialConnectionConfig(ConnectionConfig config)
		{
			return AddSpecialConnectionConfigWrapper(config);
		}

		public int AddSpecialConnectionConfigWrapper(ConnectionConfig config)
		{
			var assembly = Assembly.LoadFile($@"{QSB.Helper.OwmlConfig.GamePath}OuterWilds_Data\Managed\UnityEngine.UNETModule.dll");
			var type = assembly.GetType("UnityEngine.Networking.ConnectionConfigInternal");
			var internalInstance = Activator.CreateInstance(type, config.ToUnity());
			return (int)instance.GetType().GetMethod("AddSpecialConnectionConfigWrapper", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { internalInstance });
		}

		private void InitOtherParameters(HostTopology topology)
		{
			InitReceivedPoolSize(topology.ReceivedMessagePoolSize);
			InitSentMessagePoolSize(topology.SentMessagePoolSize);
			InitMessagePoolSizeGrowthFactor(topology.MessagePoolSizeGrowthFactor);
		}

		public void InitReceivedPoolSize(ushort pool)
		{
			instance.GetType().GetMethod("InitReceivedPoolSize", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { pool });
		}

		public void InitSentMessagePoolSize(ushort pool)
		{
			instance.GetType().GetMethod("InitSentMessagePoolSize", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { pool });
		}

		public void InitMessagePoolSizeGrowthFactor(float factor)
		{
			instance.GetType().GetMethod("InitMessagePoolSizeGrowthFactor", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { factor });
		}

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Dispose();

		~HostTopologyInternal()
		{
			Dispose();
		}

		internal IntPtr m_Ptr;
	}
}
