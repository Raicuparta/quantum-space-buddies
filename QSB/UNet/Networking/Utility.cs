using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Types;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Networking Utility.</para>
	/// </summary>
	public class Utility
	{
		private Utility()
		{
		}

		/// <summary>
		///   <para>This property is deprecated and does not need to be set or referenced.</para>
		/// </summary>
		[Obsolete("This property is unused and should not be referenced in code.", true)]
		public static bool useRandomSourceID
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		/// <summary>
		///   <para>Utility function to get the client's SourceID for unique identification.</para>
		/// </summary>
		public static SourceID GetSourceID()
		{
			return (SourceID)((long)SystemInfo.deviceUniqueIdentifier.GetHashCode());
		}

		/// <summary>
		///   <para>Deprecated; Setting the AppID is no longer necessary. Please log in through the editor and set up the project there.</para>
		/// </summary>
		/// <param name="newAppID"></param>
		[Obsolete("This function is unused and should not be referenced in code. Please sign in and setup your project in the editor instead.", true)]
		public static void SetAppID(AppID newAppID)
		{
		}

		/// <summary>
		///   <para>Utility function to fetch the program's ID for UNET Cloud interfacing.</para>
		/// </summary>
		[Obsolete("This function is unused and should not be referenced in code. Please sign in and setup your project in the editor instead.", true)]
		public static AppID GetAppID()
		{
			return AppID.Invalid;
		}

		/// <summary>
		///   <para>Utility function that accepts the access token for a network after it's received from the server.</para>
		/// </summary>
		/// <param name="netId"></param>
		/// <param name="accessToken"></param>
		public static void SetAccessTokenForNetwork(NetworkID netId, NetworkAccessToken accessToken)
		{
			if (Utility.s_dictTokens.ContainsKey(netId))
			{
				Utility.s_dictTokens.Remove(netId);
			}
			Utility.s_dictTokens.Add(netId, accessToken);
		}

		/// <summary>
		///   <para>Utility function to get this client's access token for a particular network, if it has been set.</para>
		/// </summary>
		/// <param name="netId"></param>
		public static NetworkAccessToken GetAccessTokenForNetwork(NetworkID netId)
		{
			NetworkAccessToken result;
			if (!Utility.s_dictTokens.TryGetValue(netId, out result))
			{
				result = new NetworkAccessToken();
			}
			return result;
		}

		private static Dictionary<NetworkID, NetworkAccessToken> s_dictTokens = new Dictionary<NetworkID, NetworkAccessToken>();
	}
}
