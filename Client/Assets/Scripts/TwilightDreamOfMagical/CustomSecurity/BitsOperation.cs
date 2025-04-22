

public class BitsOperation
{

	public static uint RotateLeft(uint value, int count)
	{
		if (count < 0 || count >= 32)
			return value;

		return (value << count) | (value >> (32 - count));
	}
	public static uint RotateRight(uint value, int count)
	{
		if (count < 0 || count >= 32)
			return value;

		return (value >> count) | (value << (32 - count));
	}

	public static ulong RotateLeft(ulong value, int count)
	{
		if (count < 0 || count >= 64)
			return value;

		return (value << count) | (value >> (64 - count));
	}
	public static ulong RotateRight(ulong value, int count)
	{
		if (count < 0 || count >= 64)
			return value;

		return (value >> count) | (value << (64 - count));
	}
}
