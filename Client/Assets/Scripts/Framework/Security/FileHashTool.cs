using System;
using System.Security.Cryptography;

public static class FileHashTool 
{
	public static string ComputeMD5(byte[] data)
	{
		using (var md5 = MD5.Create())
		{
			byte[] hash = md5.ComputeHash(data);
			return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
		}
	}

	public static string ComputeSHA256(byte[] data)
	{
		using (var sha256 = SHA256.Create())
		{
			byte[] hash = sha256.ComputeHash(data);
			return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
		}
	}
}
