using System;
using System.Text;
using System.Security.Cryptography;
using TwilightDreamOfMagical.CustomSecurity.SED;
using UnityEngine;

/// <summary>
/// File encryption and decryption tool using the LittleOPC algorithm.
/// </summary>
public class FileSecurityTool : IDisposable
{
	private string password = string.Empty;

	private LittleOPCAlgorithm littleOPC_Instance = null;

	public string Password
	{
		get => password;
		set => password = value;
	}

	public static LittleOPCAlgorithm CreateAlgorithmLOPC(string password, byte[] salt)
	{
		if (string.IsNullOrEmpty(password))
		{
			Debug.LogWarning("Password not set.");
			throw new InvalidOperationException("Password not set.");
		}
		var lopc = new LittleOPCAlgorithm();
		using (var KDF = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
		{
			lopc.Key = KDF.GetBytes(32);
			lopc.IV = KDF.GetBytes(8);
		}
		return lopc;
	}

	public void Dispose()
	{
		if (littleOPC_Instance != null)
		{
			littleOPC_Instance.Dispose();
			littleOPC_Instance = null;
			GC.SuppressFinalize(this);
		}
	}

	public static byte[] EncryptDataWithLOPC(byte[] data, ICryptoTransform encryptor)
	{
		if (!(encryptor is LittleOPC_WrapperE))
			Debug.LogError("EncryptData error: Invalid LittleOPC_Wrapper encryptor.");

		LittleOPC_WrapperE instance = encryptor as LittleOPC_WrapperE;
		byte[] padded = instance.Pad(data);
		byte[] encrypted = new byte[padded.Length];
		encryptor.TransformBlock(padded, 0, padded.Length, encrypted, 0);
		return encrypted;
	}

	public static byte[] DecryptDataWithLOPC(byte[] data, ICryptoTransform decryptor)
	{
		if (!(decryptor is LittleOPC_WrapperD))
			Debug.LogError("DecryptData error: Invalid LittleOPC_Wrapper decryptor.");

		byte[] buffer = new byte[data.Length];
		decryptor.TransformBlock(data, 0, data.Length, buffer, 0);

		LittleOPC_WrapperD instance = decryptor as LittleOPC_WrapperD;
		return instance.Unpad(buffer);
	}

	public ICryptoTransform GetEncryptorLOPC()
	{
		if (littleOPC_Instance == null)
			throw new InvalidOperationException("LittleOPC not initialized.");
		return littleOPC_Instance.CreateEncryptor(littleOPC_Instance.Key, littleOPC_Instance.IV);
	}

	public ICryptoTransform GetDecryptorLOPC()
	{
		if (littleOPC_Instance == null)
			throw new InvalidOperationException("LittleOPC not initialized.");
		return littleOPC_Instance.CreateDecryptor(littleOPC_Instance.Key, littleOPC_Instance.IV);
	}

	public void SelfCheckWithLittleOPC()
	{
		const string testText = "Hello, World! Little OaldresPuzzle-Cryptic!";
		Debug.Log($"Original: {testText}");

		var lopc = new LittleOPCAlgorithm();
		lopc.GenerateKey();
		lopc.GenerateIV();
		var enc = lopc.CreateEncryptor(lopc.Key, lopc.IV);
		var dec = lopc.CreateDecryptor(lopc.Key, lopc.IV);

		byte[] raw = Encoding.UTF8.GetBytes(testText);
		byte[] encrypted = FileSecurityTool.EncryptDataWithLOPC(raw, enc);
		// Print encrypted bytes as hex string
		Debug.Log($"Encrypted: {BitConverter.ToString(encrypted).Replace("-", " ")}");  // Remove dashes for cleaner output

		byte[] decrypted = FileSecurityTool.DecryptDataWithLOPC(encrypted, dec);
		string result = Encoding.UTF8.GetString(decrypted);
		Debug.Log($"Decrypted: {result}");
		Debug.Log($"Success: {testText == result}");
	}

	public void EncryptFileWithLittleOPC(string inputFile, string outputFile)
	{
		if (string.IsNullOrEmpty(inputFile) || !System.IO.File.Exists(inputFile))
		{
			Debug.LogWarning($"Encryption error: Input file not found: {inputFile}");
			throw new System.IO.FileNotFoundException("Encryption error: Input file not found.", inputFile);
		}
		if (string.IsNullOrEmpty(outputFile))
		{
			Debug.LogWarning("Encryption error: Output file path not set.");
			throw new ArgumentException("Encryption error: Output file path not set.", nameof(outputFile));
		}
		if (string.IsNullOrEmpty(this.password))
		{
			Debug.LogWarning("Encryption error: Password not set.");
			throw new InvalidOperationException("Encryption error: Password not set.");
		}

		if (this.littleOPC_Instance != null)
		{
			Debug.LogWarning("Encryption warning: Little OPC instanced already. Updating password and creating new instance.");
			this.littleOPC_Instance = null;
		}

		// Generate random salt
		byte[] salt = new byte[8];
		using (var rng = new RNGCryptoServiceProvider())
			rng.GetBytes(salt);

		// Read original file bytes
		byte[] fileBytes = Utilities.FileReaderWriter.ReadExistingFile(inputFile);

		// Compute and save original file hash
		using (var MD5Tool = new MD5CryptoServiceProvider())
		{
			string originalfileHash = BitConverter.ToString(MD5Tool.ComputeHash(fileBytes));
			string originalhashFilePath = inputFile + ".Hash";
			System.IO.File.WriteAllText(originalhashFilePath, originalfileHash);
			Debug.LogWarning("Encryption warning: Original file hash: " + originalfileHash);
			Debug.LogWarning("Encryption warning: Hash file saved: " + originalhashFilePath);
		}

		// Derive algorithm and encrypt data
		this.littleOPC_Instance = CreateAlgorithmLOPC(this.password, salt);
		ICryptoTransform encryptor = GetEncryptorLOPC();
		byte[] encryptedData = EncryptDataWithLOPC(fileBytes, encryptor);

		// Write encrypted data and append salt
		if (System.IO.File.Exists(outputFile))
		{
			Utilities.FileReaderWriter.WriteOrOverwriteFile(outputFile, encryptedData);
		}
		else
		{
			Utilities.FileReaderWriter.WriteNewFile(outputFile, encryptedData);
		}
		Utilities.FileReaderWriter.AppendToFile(outputFile, salt);

		Debug.Log("File encrypted successfully.");
	}

	public void DecryptFileWithLittleOPC(string inputFile, string outputFile)
	{
		if (string.IsNullOrEmpty(inputFile) || !System.IO.File.Exists(inputFile))
		{
			Debug.LogWarning($"Decryption error: Input file not found: {inputFile}");
			throw new System.IO.FileNotFoundException("Decryption error: Input file not found.", inputFile);
		}
		if (string.IsNullOrEmpty(outputFile))
		{
			Debug.LogWarning("Decryption error: Output file path not set.");
			throw new ArgumentException("Decryption error: Output file path not set.", nameof(outputFile));
		}
		if (string.IsNullOrEmpty(this.password))
		{
			Debug.LogWarning("Decryption error: Password not set.");
			throw new InvalidOperationException("Decryption error: Password not set.");
		}

		if (this.littleOPC_Instance != null)
		{
			Debug.LogWarning("Decryption warning: Little OPC instanced already. Updating password and creating new instance.");
			this.littleOPC_Instance = null;
		}

		// Read encrypted file bytes
		byte[] allEncryptedBytes = Utilities.FileReaderWriter.ReadExistingFile(inputFile);
		if (allEncryptedBytes.Length < 8)
			throw new System.IO.InvalidDataException("Encrypted file too small to contain salt.");

		// Extract salt and encrypted data
		int saltLen = 8;
		byte[] salt = new byte[saltLen];
		Array.Copy(allEncryptedBytes, allEncryptedBytes.Length - saltLen, salt, 0, saltLen);
		byte[] encryptedData = new byte[allEncryptedBytes.Length - saltLen];
		Array.Copy(allEncryptedBytes, 0, encryptedData, 0, encryptedData.Length);

		// Derive algorithm and decrypt data
		this.littleOPC_Instance = CreateAlgorithmLOPC(this.password, salt);
		ICryptoTransform decryptor = GetDecryptorLOPC();
		byte[] decryptedData = FileSecurityTool.DecryptDataWithLOPC(encryptedData, decryptor);

		// Verify MD5 hash
		using (var MD5Tool = new MD5CryptoServiceProvider())
		{
			string originalhashFilePath = outputFile + ".Hash";
			string originalfileHash = System.IO.File.ReadAllText(originalhashFilePath);
			string processedfileHash = BitConverter.ToString(MD5Tool.ComputeHash(decryptedData));

			if (originalfileHash != processedfileHash)
			{
				Debug.LogWarning("Decryption error: Oops, two file hash mismatch!!!");
				Debug.LogWarning("Decryption error: Original file hash: " + originalfileHash);
				Debug.LogWarning("Decryption error: Processed file hash: " + processedfileHash);
				throw new CryptographicException("Decryption error: File hash mismatch.");
			}
			System.IO.File.Delete(originalhashFilePath);
		}

		// Write decrypted file
		if (System.IO.File.Exists(outputFile))
		{
			Utilities.FileReaderWriter.WriteOrOverwriteFile(outputFile, decryptedData);
		}
		else
		{
			Utilities.FileReaderWriter.WriteNewFile(outputFile, decryptedData);
		}

		Debug.Log("File decrypted successfully.");
	}
}
