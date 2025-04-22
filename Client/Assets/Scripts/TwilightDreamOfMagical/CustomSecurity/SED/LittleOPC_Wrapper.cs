using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Unity.VisualScripting;

namespace TwilightDreamOfMagical.CustomSecurity.SED
{
	public class LittleOPC_Wrapper : IDisposable
	{
		private LittleOPC littleOPC_Cryptor = new LittleOPC();
		private ulong seed = 1;

		public LittleOPC_Wrapper()
		{
			littleOPC_Cryptor.ResetPRNG(seed);
		}

		public ulong SingleBlockEncryption(ulong data, ulong key, ulong numberOnce)
		{
			ulong result = littleOPC_Cryptor.EncryptionCoreFunction(data, key, numberOnce);
			// re-seed the PRNG exists in LittleOPC or externally
			littleOPC_Cryptor.ResetPRNG(this.seed);
			return result;
		}

		public ulong SingleBlockDecryption(ulong data, ulong key, ulong numberOnce)
		{
			ulong result = littleOPC_Cryptor.DecryptionCoreFunction(data, key, numberOnce);
			// re-seed the PRNG exists in LittleOPC or externally
			littleOPC_Cryptor.ResetPRNG(this.seed);
			return result;
		}

		public void MultipleBlocksEncryption(List<ulong> data, List<ulong> keys, ref List<ulong> encryptedData)
		{
			if (data == null || keys == null || keys.Count == 0) return;

			if (encryptedData == null)
				encryptedData = new List<ulong>(data.Count);
			else encryptedData.Clear();

			for (int i = 0; i < data.Count; i++)
			{
				encryptedData.Add(littleOPC_Cryptor.EncryptionCoreFunction(data[i], keys[i % keys.Count], (ulong)i));
			}

			// Reset the PRNG state for the next encryption or decryption
			littleOPC_Cryptor.ResetPRNG(this.seed);
		}

		public void MultipleBlocksDecryption(List<ulong> encryptedData, List<ulong> keys, ref List<ulong> decryptedData)
		{
			if (encryptedData == null || keys == null || keys.Count == 0) return;

			if (decryptedData == null)
				decryptedData = new List<ulong>(encryptedData.Count);
			else decryptedData.Clear();

			for (int i = 0; i < encryptedData.Count; i++)
			{
				decryptedData.Add(littleOPC_Cryptor.DecryptionCoreFunction(encryptedData[i], keys[i % keys.Count], (ulong)i));
			}

			// Reset the PRNG state for the next encryption or decryption
			littleOPC_Cryptor.ResetPRNG(this.seed);
		}

		public void Dispose()
		{
			seed = 0;
			littleOPC_Cryptor.Dispose();
		}
	}

	public class LittleOPC_WrapperE : ICryptoTransform
	{
		private ulong[] keys;
		private ulong nonce;

		// Modified constructors with English comments, size checking and secure cleanup

		public LittleOPC_WrapperE(byte[] key_bytes, byte[] nonce_bytes)
		{
			// 64 Bits * 4 = 256 Bits
			keys = new ulong[4];
			// Process the keys array securely.
			// Clear the keys array to start fresh.
			Array.Clear(keys, 0, keys.Length);
			// Calculate total number of bytes needed for the keys array.
			int keysBytesNeeded = keys.Length * sizeof(ulong);
			// Copy only the minimum required bytes.
			int copyLength = Math.Min(key_bytes.Length, keysBytesNeeded);
			Buffer.BlockCopy(key_bytes, 0, keys, 0, copyLength);

			// Process the nonce securely.
			// Only create a temporary buffer if nonce_bytes is not exactly 8 bytes.
			if (nonce_bytes.Length != 8)
			{
				byte[] tempNonce = new byte[8];
				Buffer.BlockCopy(nonce_bytes, 0, tempNonce, 0, Math.Min(nonce_bytes.Length, 8));
				nonce = BitConverter.ToUInt64(tempNonce, 0);
				// Securely clear the temporary buffer.
				CryptographicOperations.ZeroMemory(tempNonce);
			}
			else
			{
				nonce = BitConverter.ToUInt64(nonce_bytes, 0);
			}
		}

		public LittleOPC_WrapperE(ulong[] keys, ulong nonce)
		{
			this.keys = keys;
			this.nonce = nonce;
		}

		public bool CanReuseTransform => false;
		public bool CanTransformMultipleBlocks => true;
		public int InputBlockSize => 8; // One block of 64 bits
		public int OutputBlockSize => 8;

		public byte[] Pad(byte[] data)
		{
			int paddingSize = InputBlockSize - (data.Length % InputBlockSize);
			byte[] paddedData = new byte[data.Length + paddingSize];
			Array.Copy(data, paddedData, data.Length);

			// Add PKCS#7 padding
			for (int i = data.Length; i < paddedData.Length; i++)
			{
				paddedData[i] = (byte)paddingSize;
			}

			return paddedData;

		}

		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			// Direct processing of input data, assuming that it has been externally cryptographically padded prior to encryption
			byte[] dataToProcess = inputBuffer.Skip(inputOffset).Take(inputCount).ToArray();

			if (dataToProcess.Length <= 8)
			{
				// Directly handle small data without parallelization
				LittleOPC_Wrapper opcWrapper = new LittleOPC_Wrapper();
				ulong block = BitConverter.ToUInt64(dataToProcess, 0);
				ulong encryptedBlock = opcWrapper.SingleBlockEncryption(block, keys[0], nonce);
				byte[] encryptedBytes = BitConverter.GetBytes(encryptedBlock);
				Array.Copy(encryptedBytes, 0, outputBuffer, outputOffset, encryptedBytes.Length);
			}
			else
			{
				// Use parallel processing for larger data
				Parallel.For(0, dataToProcess.Length / 8, (index) =>
				{
					LittleOPC_Wrapper opcWrapper = new LittleOPC_Wrapper();
					int offset = index * 8;
					ulong block = BitConverter.ToUInt64(dataToProcess, offset);
					ulong encryptedBlock = block;
					//Applying multiple keys to a single data block
					for (int i = 0; i < keys.Length; i++)
					{
						encryptedBlock = opcWrapper.SingleBlockEncryption(encryptedBlock, keys[i], nonce + (ulong)(i + 1));
					}
					byte[] encryptedBytes = BitConverter.GetBytes(encryptedBlock);
					Array.Copy(encryptedBytes, 0, outputBuffer, outputOffset + offset, 8);
				});
			}

			return inputCount; //Return raw data length
		}

		public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			byte[] finalBlock = new byte[inputCount];
			TransformBlock(inputBuffer, inputOffset, inputCount, finalBlock, 0);
			return finalBlock;
		}

		public void Dispose()
		{
			if (keys != null)
			{
				UnsafeTool.MemoryCleanup.ZeroMemory(keys);

				keys = null;
			}
			nonce = 0;
			GC.SuppressFinalize(this);
		}
	}

	public class LittleOPC_WrapperD : ICryptoTransform
	{
		private ulong[] keys;
		private ulong nonce;

		public LittleOPC_WrapperD(byte[] key_bytes, byte[] nonce_bytes)
		{
			// 64 Bits * 4 = 256 Bits
			keys = new ulong[4];
			// Process the keys array securely.
			// Clear the keys array to start fresh.
			Array.Clear(keys, 0, keys.Length);
			// Calculate total number of bytes needed for the keys array.
			int keysBytesNeeded = keys.Length * sizeof(ulong);
			// Copy only the minimum required bytes.
			int copyLength = Math.Min(key_bytes.Length, keysBytesNeeded);
			Buffer.BlockCopy(key_bytes, 0, keys, 0, copyLength);

			// Process the nonce securely.
			// Only create a temporary buffer if nonce_bytes is not exactly 8 bytes.
			if (nonce_bytes.Length != 8)
			{
				byte[] tempNonce = new byte[8];
				Buffer.BlockCopy(nonce_bytes, 0, tempNonce, 0, Math.Min(nonce_bytes.Length, 8));
				nonce = BitConverter.ToUInt64(tempNonce, 0);
				// Securely clear the temporary buffer.
				CryptographicOperations.ZeroMemory(tempNonce);
			}
			else
			{
				nonce = BitConverter.ToUInt64(nonce_bytes, 0);
			}
		}

		public LittleOPC_WrapperD(ulong[] keys, ulong nonce)
		{
			this.keys = keys;
			this.nonce = nonce;
		}

		public bool CanReuseTransform => false;
		public bool CanTransformMultipleBlocks => true;
		public int InputBlockSize => 8; // One block of 64 bits
		public int OutputBlockSize => 8;

		public byte[] Unpad(byte[] data)
		{
			if (data == null || data.Length == 0)
				throw new ArgumentException("Data cannot be null or empty");

			// Get the value of the last byte, which indicates the padding size
			int paddingSize = data[data.Length - 1];
			if (paddingSize <= 0 || paddingSize > InputBlockSize)
				throw new InvalidOperationException("Invalid padding size");

			// Validate padding
			for (int i = data.Length - paddingSize; i < data.Length; i++)
			{
				if (data[i] != paddingSize)
					throw new InvalidOperationException("Invalid padding");
			}

			// Remove PKCS#7 padding
			byte[] unpaddedData = new byte[data.Length - paddingSize];
			Array.Copy(data, unpaddedData, unpaddedData.Length);
			return unpaddedData;
		}

		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			// Direct processing of input data, assuming it will be externally de-cryptographically padded after decryption

			byte[] dataToProcess = new byte[inputCount];
			Array.Copy(inputBuffer, inputOffset, dataToProcess, 0, inputCount);

			if (dataToProcess.Length <= 8)
			{
				// Directly handle small data without parallelization
				LittleOPC_Wrapper opcWrapper = new LittleOPC_Wrapper();
				ulong block = BitConverter.ToUInt64(dataToProcess, 0);
				ulong decryptedBlock = opcWrapper.SingleBlockDecryption(block, keys[0], nonce);
				byte[] decryptedBytes = BitConverter.GetBytes(decryptedBlock);
				Array.Copy(decryptedBytes, 0, outputBuffer, outputOffset, decryptedBytes.Length);
			}
			else
			{
				// Use parallel processing for larger data
				Parallel.For(0, dataToProcess.Length / 8, (index) =>
				{
					LittleOPC_Wrapper opcWrapper = new LittleOPC_Wrapper();
					int offset = index * 8;
					ulong block = BitConverter.ToUInt64(dataToProcess, offset);
					ulong decryptedBlock = block;
					//Applying multiple keys to a single data block
					for (int i = keys.Length; i > 0; i--)
					{
						decryptedBlock = opcWrapper.SingleBlockDecryption(decryptedBlock, keys[i - 1], nonce + (ulong)(i));
					}
					byte[] decryptedBytes = BitConverter.GetBytes(decryptedBlock);
					Array.Copy(decryptedBytes, 0, outputBuffer, outputOffset + offset, 8);
				});
			}

			return inputCount; //Return raw data length
		}

		public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			byte[] finalBlock = new byte[inputCount];
			TransformBlock(inputBuffer, inputOffset, inputCount, finalBlock, 0);
			return finalBlock;
		}


		public void Dispose()
		{
			if (keys != null)
			{
				UnsafeTool.MemoryCleanup.ZeroMemory(keys);

				keys = null;
			}
			nonce = 0;
			GC.SuppressFinalize(this);
		}
	}

	public class LittleOPCAlgorithm : SymmetricAlgorithm
	{
		public override byte[] Key 
		{
			get { return this.KeyValue; }
			set { this.KeyValue = value; }
		}
		public override byte[] IV
		{
			get { return this.IVValue; }
			set { this.IVValue = value; }
		}

		public LittleOPCAlgorithm()
		{
			// 初始化对称算法的各种属性
			this.KeyValue = new byte[32];
			this.IVValue = new byte[8];
			this.BlockSizeValue = 64; // 设置块大小为64比特
			this.KeySizeValue = this.KeyValue.Length * 8; // 设置密钥大小为64比特
			this.FeedbackSizeValue = 64; // 设置反馈大小为64比特
			this.LegalBlockSizesValue = new KeySizes[] { new KeySizes(this.BlockSizeValue, this.BlockSizeValue, 0) };
			this.LegalKeySizesValue = new KeySizes[] { new KeySizes(this.KeySizeValue, this.KeySizeValue, 0) };
		}

		public override ICryptoTransform CreateEncryptor()
		{
			return new LittleOPC_WrapperE(new ulong[] { 0, 0, 0, 0 }, 1);
		}

		public override ICryptoTransform CreateDecryptor()
		{
			return new LittleOPC_WrapperD(new ulong[] { 0, 0, 0, 0 }, 1);
		}

		public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
		{
			return new LittleOPC_WrapperE(rgbKey, rgbIV);
		}

		public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
		{
			return new LittleOPC_WrapperD(rgbKey, rgbIV);
		}

		public override void GenerateIV()
		{
			// 生成适合算法的初始化向量（64比特，即8字节）
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(this.IVValue); // 填充数组
			}
		}

		public override void GenerateKey()
		{
			// 生成适合算法的密钥（256比特，即32字节）
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(this.KeyValue); // 填充数组
			}
		}

		public new void Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Wipe Keys
				if (this.KeyValue != null)
				{
					CryptographicOperations.ZeroMemory(this.KeyValue);
					this.KeyValue = null;
				}

				// Wipe Initial Vector
				if (this.IVValue != null)
				{
					CryptographicOperations.ZeroMemory(this.IVValue);
					this.IVValue = null;
				}
			}

			base.Dispose(disposing);
		}
	}
}
