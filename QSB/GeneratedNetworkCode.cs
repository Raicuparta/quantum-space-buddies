using System;
using System.Runtime.InteropServices;
using QSB.TransformSync;
using UnityEngine.Networking;

namespace Unity
{
	[StructLayout(LayoutKind.Auto, CharSet = CharSet.Auto)]
	public class GeneratedNetworkCode
	{
		public static void Write<T>(NetworkWriter writer, T value)
		{
			switch (Type.GetTypeCode(typeof(T)))
			{
				case TypeCode.String:
					writer.Write((string)Convert.ChangeType(value, typeof(string)));
					break;
				case TypeCode.Boolean:
					writer.Write((bool)Convert.ChangeType(value, typeof(bool)));
					break;
			}
		}

		public static T Read<T>(NetworkReader reader)
		{
			switch (Type.GetTypeCode(typeof(T)))
			{
				case TypeCode.String:
					return (T)Convert.ChangeType(reader.ReadString(), typeof(T));
				case TypeCode.Boolean:
					return (T)Convert.ChangeType(reader.ReadBoolean(), typeof(T));
			}
			return default;
		}
	}
}