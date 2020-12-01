using System;

namespace QSB.UNet.Networking
{
	/// <summary>
	///   <para>Access token used to authenticate a client session for the purposes of allowing or disallowing match operations requested by that client.</para>
	/// </summary>
	public class NetworkAccessToken
	{
		public NetworkAccessToken()
		{
			array = new byte[64];
		}

		public NetworkAccessToken(byte[] array)
		{
			array = array;
		}

		public NetworkAccessToken(string strArray)
		{
			try
			{
				array = Convert.FromBase64String(strArray);
			}
			catch (Exception)
			{
				array = new byte[64];
			}
		}

		/// <summary>
		///   <para>Accessor to get an encoded string from the m_array data.</para>
		/// </summary>
		public string GetByteString()
		{
			return Convert.ToBase64String(array);
		}

		/// <summary>
		///   <para>Checks if the token is a valid set of data with respect to default values (returns true if the values are not default, does not validate the token is a current legitimate token with respect to the server's auth framework).</para>
		/// </summary>
		public bool IsValid()
		{
			bool result;
			if (array == null || array.Length != 64)
			{
				result = false;
			}
			else
			{
				bool flag = false;
				foreach (byte b in array)
				{
					if (b != 0)
					{
						flag = true;
						break;
					}
				}
				result = flag;
			}
			return result;
		}

		private const int NETWORK_ACCESS_TOKEN_SIZE = 64;

		/// <summary>
		///   <para>Binary field for the actual token.</para>
		/// </summary>
		public byte[] array;
	}
}
