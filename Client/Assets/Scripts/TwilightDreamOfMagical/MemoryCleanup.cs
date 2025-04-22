using System;
using System.Security.Cryptography;

namespace TwilightDreamOfMagical.CustomSecurity.UnsafeTool
{
	public static unsafe class MemoryCleanup
	{
		public static void ZeroMemory(ulong[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Length > int.MaxValue / sizeof(ulong))
				throw new ArgumentOutOfRangeException(nameof(data), "Array is too large.");

			fixed (ulong* ptr = data)
			{
				Span<byte> byteSpan = new Span<byte>(ptr, data.Length * sizeof(ulong));
				CryptographicOperations.ZeroMemory(byteSpan);
			}
		}
	}
}
