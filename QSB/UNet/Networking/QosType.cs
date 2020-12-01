using System;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Enumeration of all supported quality of service channel modes.</para>
	/// </summary>
	public enum QosType
	{
		/// <summary>
		///   <para>There is no guarantee of delivery or ordering.</para>
		/// </summary>
		Unreliable,
		/// <summary>
		///   <para>There is no guarantee of delivery or ordering, but allowing fragmented messages with up to 32 fragments per message.</para>
		/// </summary>
		UnreliableFragmented,
		/// <summary>
		///   <para>There is no guarantee of delivery and all unordered messages will be dropped. Example: VoIP.</para>
		/// </summary>
		UnreliableSequenced,
		/// <summary>
		///   <para>Each message is guaranteed to be delivered but not guaranteed to be in order.</para>
		/// </summary>
		Reliable,
		/// <summary>
		///   <para>Each message is guaranteed to be delivered, also allowing fragmented messages with up to 32 fragments per message.</para>
		/// </summary>
		ReliableFragmented,
		/// <summary>
		///   <para>Each message is guaranteed to be delivered and in order.</para>
		/// </summary>
		ReliableSequenced,
		/// <summary>
		///   <para>An unreliable message. Only the last message in the send buffer is sent. Only the most recent message in the receive buffer will be delivered.</para>
		/// </summary>
		StateUpdate,
		/// <summary>
		///   <para>A reliable message. Note: Only the last message in the send buffer is sent. Only the most recent message in the receive buffer will be delivered.</para>
		/// </summary>
		ReliableStateUpdate,
		/// <summary>
		///   <para>A reliable message that will be re-sent with a high frequency until it is acknowledged.</para>
		/// </summary>
		AllCostDelivery,
		/// <summary>
		///   <para>There is garantee of ordering, no guarantee of delivery, but allowing fragmented messages with up to 32 fragments per message.</para>
		/// </summary>
		UnreliableFragmentedSequenced,
		/// <summary>
		///   <para>Each message is guaranteed to be delivered in order, also allowing fragmented messages with up to 32 fragments per message.</para>
		/// </summary>
		ReliableFragmentedSequenced
	}
}
