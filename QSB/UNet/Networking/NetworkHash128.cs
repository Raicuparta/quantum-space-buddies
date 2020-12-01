using System;

namespace QSB.UNet.Networking
{
	[Serializable]
	public struct NetworkHash128
	{
		public void Reset()
		{
			i0 = 0;
			i1 = 0;
			i2 = 0;
			i3 = 0;
			i4 = 0;
			i5 = 0;
			i6 = 0;
			i7 = 0;
			i8 = 0;
			i9 = 0;
			i10 = 0;
			i11 = 0;
			i12 = 0;
			i13 = 0;
			i14 = 0;
			i15 = 0;
		}

		public bool IsValid()
		{
			return (i0 | i1 | i2 | i3 | i4 | i5 | i6 | i7 | i8 | i9 | i10 | i11 | i12 | i13 | i14 | i15) != 0;
		}

		private static int HexToNumber(char c)
		{
			int result;
			if (c >= '0' && c <= '9')
			{
				result = (int)(c - '0');
			}
			else if (c >= 'a' && c <= 'f')
			{
				result = (int)(c - 'a' + '\n');
			}
			else if (c >= 'A' && c <= 'F')
			{
				result = (int)(c - 'A' + '\n');
			}
			else
			{
				result = 0;
			}
			return result;
		}

		public static NetworkHash128 Parse(string text)
		{
			int length = text.Length;
			if (length < 32)
			{
				string str = "";
				for (int i = 0; i < 32 - length; i++)
				{
					str += "0";
				}
				text = str + text;
			}
			NetworkHash128 result;
			result.i0 = (byte)(NetworkHash128.HexToNumber(text[0]) * 16 + NetworkHash128.HexToNumber(text[1]));
			result.i1 = (byte)(NetworkHash128.HexToNumber(text[2]) * 16 + NetworkHash128.HexToNumber(text[3]));
			result.i2 = (byte)(NetworkHash128.HexToNumber(text[4]) * 16 + NetworkHash128.HexToNumber(text[5]));
			result.i3 = (byte)(NetworkHash128.HexToNumber(text[6]) * 16 + NetworkHash128.HexToNumber(text[7]));
			result.i4 = (byte)(NetworkHash128.HexToNumber(text[8]) * 16 + NetworkHash128.HexToNumber(text[9]));
			result.i5 = (byte)(NetworkHash128.HexToNumber(text[10]) * 16 + NetworkHash128.HexToNumber(text[11]));
			result.i6 = (byte)(NetworkHash128.HexToNumber(text[12]) * 16 + NetworkHash128.HexToNumber(text[13]));
			result.i7 = (byte)(NetworkHash128.HexToNumber(text[14]) * 16 + NetworkHash128.HexToNumber(text[15]));
			result.i8 = (byte)(NetworkHash128.HexToNumber(text[16]) * 16 + NetworkHash128.HexToNumber(text[17]));
			result.i9 = (byte)(NetworkHash128.HexToNumber(text[18]) * 16 + NetworkHash128.HexToNumber(text[19]));
			result.i10 = (byte)(NetworkHash128.HexToNumber(text[20]) * 16 + NetworkHash128.HexToNumber(text[21]));
			result.i11 = (byte)(NetworkHash128.HexToNumber(text[22]) * 16 + NetworkHash128.HexToNumber(text[23]));
			result.i12 = (byte)(NetworkHash128.HexToNumber(text[24]) * 16 + NetworkHash128.HexToNumber(text[25]));
			result.i13 = (byte)(NetworkHash128.HexToNumber(text[26]) * 16 + NetworkHash128.HexToNumber(text[27]));
			result.i14 = (byte)(NetworkHash128.HexToNumber(text[28]) * 16 + NetworkHash128.HexToNumber(text[29]));
			result.i15 = (byte)(NetworkHash128.HexToNumber(text[30]) * 16 + NetworkHash128.HexToNumber(text[31]));
			return result;
		}

		public override string ToString()
		{
			return string.Format("{0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}{8:x2}{9:x2}{10:x2}{11:x2}{12:x2}{13:x2}{14:x2}{15:x2}", new object[]
			{
				i0,
				i1,
				i2,
				i3,
				i4,
				i5,
				i6,
				i7,
				i8,
				i9,
				i10,
				i11,
				i12,
				i13,
				i14,
				i15
			});
		}

		public byte i0;

		public byte i1;

		public byte i2;

		public byte i3;

		public byte i4;

		public byte i5;

		public byte i6;

		public byte i7;

		public byte i8;

		public byte i9;

		public byte i10;

		public byte i11;

		public byte i12;

		public byte i13;

		public byte i14;

		public byte i15;
	}
}
