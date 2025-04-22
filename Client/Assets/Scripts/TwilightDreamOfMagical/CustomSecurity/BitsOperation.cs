public static class BitsOperation
{
	public static uint RotateLeft(uint value, int count)
	{
		count &= 31;
		return (value << count) | (value >> ((32 - count) & 31));
	}

	public static uint RotateRight(uint value, int count)
	{
		count &= 31;
		return (value >> count) | (value << ((32 - count) & 31));
	}

	public static ulong RotateLeft(ulong value, int count)
	{
		count &= 63;
		return (value << count) | (value >> ((64 - count) & 63));
	}

	public static ulong RotateRight(ulong value, int count)
	{
		count &= 63;
		return (value >> count) | (value << ((64 - count) & 63));
	}
}
