using System;
using System.ComponentModel;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>The NodeID is the ID used in Relay matches to track nodes in a network.</para>
	/// </summary>
	[DefaultValue(NodeID.Invalid)]
	public enum NodeID : ushort
	{
		/// <summary>
		///   <para>The invalid case of a NodeID.</para>
		/// </summary>
		Invalid
	}
}
