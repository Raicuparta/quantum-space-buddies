using System;
using System.Net.Sockets;

namespace QSB.UNet.Networking
{
	internal static class DotNetCompatibility
	{
		internal static string GetMethodName(this Delegate func)
		{
			return func.Method.Name;
		}

		internal static Type GetBaseType(this Type type)
		{
			return type.BaseType;
		}

		internal static string GetErrorCode(this SocketException e)
		{
			return e.ErrorCode.ToString();
		}
	}
}
