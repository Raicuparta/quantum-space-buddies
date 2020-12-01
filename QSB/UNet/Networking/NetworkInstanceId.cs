﻿using System;
using UnityEngine;

namespace QSB.UNet.Networking
{
	[Serializable]
	public struct NetworkInstanceId
	{
		public NetworkInstanceId(uint value)
		{
			m_Value = value;
		}

		public bool IsEmpty()
		{
			return m_Value == 0U;
		}

		public override int GetHashCode()
		{
			return (int)m_Value;
		}

		public override bool Equals(object obj)
		{
			return obj is NetworkInstanceId && this == (NetworkInstanceId)obj;
		}

		public static bool operator ==(NetworkInstanceId c1, NetworkInstanceId c2)
		{
			return c1.m_Value == c2.m_Value;
		}

		public static bool operator !=(NetworkInstanceId c1, NetworkInstanceId c2)
		{
			return c1.m_Value != c2.m_Value;
		}

		public override string ToString()
		{
			return m_Value.ToString();
		}

		public uint Value
		{
			get
			{
				return m_Value;
			}
		}

		[SerializeField]
		private readonly uint m_Value;

		public static NetworkInstanceId Invalid = new NetworkInstanceId(uint.MaxValue);

		internal static NetworkInstanceId Zero = new NetworkInstanceId(0U);
	}
}
