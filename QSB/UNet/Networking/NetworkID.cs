using System;
using System.ComponentModel;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Network ID, used for match making.</para>
	/// </summary>
	[DefaultValue(18446744073709551615UL)]
	public enum NetworkID : ulong
	{
		/// <summary>
		///   <para>Invalid NetworkID.</para>
		/// </summary>
		Invalid = 18446744073709551615UL
	}
}
