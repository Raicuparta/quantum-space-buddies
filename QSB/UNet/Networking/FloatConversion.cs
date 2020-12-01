using System;

namespace QSB.UNet.Networking
{
	internal class FloatConversion
	{
		public static float ToSingle(uint value)
		{
			UIntFloat uintFloat = default(UIntFloat);
			uintFloat.intValue = value;
			return uintFloat.floatValue;
		}

		public static double ToDouble(ulong value)
		{
			UIntFloat uintFloat = default(UIntFloat);
			uintFloat.longValue = value;
			return uintFloat.doubleValue;
		}

		public static decimal ToDecimal(ulong value1, ulong value2)
		{
			UIntDecimal uintDecimal = default(UIntDecimal);
			uintDecimal.longValue1 = value1;
			uintDecimal.longValue2 = value2;
			return uintDecimal.decimalValue;
		}
	}
}
