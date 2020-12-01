using System;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Possible Networking.NetworkTransport errors.</para>
	/// </summary>
	public enum NetworkError
	{
		/// <summary>
		///   <para>The operation completed successfully.</para>
		/// </summary>
		Ok,
		/// <summary>
		///   <para>The specified host not available.</para>
		/// </summary>
		WrongHost,
		/// <summary>
		///   <para>The specified connectionId doesn't exist.</para>
		/// </summary>
		WrongConnection,
		/// <summary>
		///   <para>The specified channel doesn't exist.</para>
		/// </summary>
		WrongChannel,
		/// <summary>
		///   <para>Not enough resources are available to process this request.</para>
		/// </summary>
		NoResources,
		/// <summary>
		///   <para>Not a data message.</para>
		/// </summary>
		BadMessage,
		/// <summary>
		///   <para>Connection timed out.</para>
		/// </summary>
		Timeout,
		/// <summary>
		///   <para>The message is too long to fit the buffer.</para>
		/// </summary>
		MessageToLong,
		/// <summary>
		///   <para>Operation is not supported.</para>
		/// </summary>
		WrongOperation,
		/// <summary>
		///   <para>The protocol versions are not compatible. Check your library versions.</para>
		/// </summary>
		VersionMismatch,
		/// <summary>
		///   <para>The Networking.ConnectionConfig  does not match the other endpoint.</para>
		/// </summary>
		CRCMismatch,
		/// <summary>
		///   <para>The address supplied to connect to was invalid or could not be resolved.</para>
		/// </summary>
		DNSFailure,
		/// <summary>
		///   <para>This error will occur if any function is called with inappropriate parameter values.</para>
		/// </summary>
		UsageError
	}
}
