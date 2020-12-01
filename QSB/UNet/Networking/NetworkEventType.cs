using System;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Event that is returned when calling the Networking.NetworkTransport.Receive and Networking.NetworkTransport.ReceiveFromHost functions.</para>
	/// </summary>
	public enum NetworkEventType
	{
		/// <summary>
		///   <para>Data event received. Indicating that data was received.</para>
		/// </summary>
		DataEvent,
		/// <summary>
		///   <para>Connection event received. Indicating that a new connection was established.</para>
		/// </summary>
		ConnectEvent,
		/// <summary>
		///   <para>Disconnection event received.</para>
		/// </summary>
		DisconnectEvent,
		/// <summary>
		///   <para>No new event was received.</para>
		/// </summary>
		Nothing,
		/// <summary>
		///   <para>Broadcast discovery event received.
		/// To obtain sender connection info and possible complimentary message from them, call Networking.NetworkTransport.GetBroadcastConnectionInfo() and Networking.NetworkTransport.GetBroadcastConnectionMessage() functions.</para>
		/// </summary>
		BroadcastEvent
	}
}
