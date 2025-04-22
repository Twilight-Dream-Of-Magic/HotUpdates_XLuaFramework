using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TwilightDreamOfMagical.CustomSecurity;

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

		public Block128 SingleBlockEncryption(Block128 data, Key128 key, ulong numberOnce)
		{
			Block128 result = littleOPC_Cryptor.EncryptionCoreFunction(data, key, numberOnce);
			// Reset after a top-level single-block call so each block is deterministic by nonce/counter.
			littleOPC_Cryptor.ResetPRNG(this.seed);
			return result;
		}

		public Block128 SingleBlockDecryption(Block128 data, Key128 key, ulong numberOnce)
		{
			Block128 result = littleOPC_Cryptor.DecryptionCoreFunction(data, key, numberOnce);
			// Reset after a top-level single-block call so each block is deterministic by nonce/counter.
			littleOPC_Cryptor.ResetPRNG(this.seed);
			return result;
		}

		public void MultipleBlocksEncryption(List<Block128> data, List<Key128> keys, ref List<Block128> encryptedData)
		{
			if (data == null || keys == null || keys.Count == 0) return;

			if (encryptedData == null)
				encryptedData = new List<Block128>(data.Count);
			else encryptedData.Clear();

			for (int i = 0; i < data.Count; i++)
			{
				encryptedData.Add(littleOPC_Cryptor.EncryptionCoreFunction(data[i], keys[i % keys.Count], (ulong)i));
			}

			littleOPC_Cryptor.ResetPRNG(this.seed);
		}

		public void MultipleBlocksDecryption(List<Block128> encryptedData, List<Key128> keys, ref List<Block128> decryptedData)
		{
			if (encryptedData == null || keys == null || keys.Count == 0) return;

			if (decryptedData == null)
				decryptedData = new List<Block128>(encryptedData.Count);
			else decryptedData.Clear();

			for (int i = 0; i < encryptedData.Count; i++)
			{
				decryptedData.Add(littleOPC_Cryptor.DecryptionCoreFunction(encryptedData[i], keys[i % keys.Count], (ulong)i));
			}

			littleOPC_Cryptor.ResetPRNG(this.seed);
		}

		public void Dispose()
		{
			seed = 0;
			if (littleOPC_Cryptor != null)
			{
				littleOPC_Cryptor.Dispose();
				littleOPC_Cryptor = null;
			}
			GC.SuppressFinalize(this);
		}
	}

	internal static class LittleOPC128Codec
	{
		public const int BlockSizeBytes = 16;
		public const int KeySizeBytes = 16;
		public const int IVSizeBytes = 16;

		public static Key128 ReadKey128(byte[] keyBytes)
		{
			if (keyBytes == null)
				throw new ArgumentNullException(nameof(keyBytes));
			if (keyBytes.Length != KeySizeBytes)
				throw new CryptographicException("LittleOPC-128 requires a 128-bit key (16 bytes).");

			return new Key128(BitConverter.ToUInt64(keyBytes, 0), BitConverter.ToUInt64(keyBytes, 8));
		}

		public static void ReadIV128(byte[] ivBytes, out ulong nonceLow, out ulong nonceHigh)
		{
			if (ivBytes == null)
				throw new ArgumentNullException(nameof(ivBytes));
			if (ivBytes.Length != IVSizeBytes)
				throw new CryptographicException("LittleOPC-128 requires a 128-bit IV/nonce (16 bytes).");

			nonceLow = BitConverter.ToUInt64(ivBytes, 0);
			nonceHigh = BitConverter.ToUInt64(ivBytes, 8);
		}

		public static Block128 ReadBlock128(byte[] buffer, int offset)
		{
			return new Block128(BitConverter.ToUInt64(buffer, offset), BitConverter.ToUInt64(buffer, offset + 8));
		}

		public static void WriteBlock128(Block128 block, byte[] buffer, int offset)
		{
			byte[] first = BitConverter.GetBytes(block.First);
			byte[] second = BitConverter.GetBytes(block.Second);
			Buffer.BlockCopy(first, 0, buffer, offset, 8);
			Buffer.BlockCopy(second, 0, buffer, offset + 8, 8);
		}

		public static ulong MakeBlockNumberOnce(ulong nonceLow, ulong nonceHigh, ulong blockIndex)
		{
			unchecked
			{
				// Keep the public C++ core's 64-bit number_once while making all 128 IV bits participate.
				return (nonceLow + blockIndex) ^ BitsOperation.RotateLeft(nonceHigh ^ blockIndex, (int)(blockIndex & 63UL));
			}
		}
	}

	public class LittleOPC_WrapperE : ICryptoTransform
	{
		private Key128 key;
		private ulong nonceLow;
		private ulong nonceHigh;
		private bool disposed;

		public LittleOPC_WrapperE(byte[] key_bytes, byte[] nonce_bytes)
		{
			key = LittleOPC128Codec.ReadKey128(key_bytes);
			LittleOPC128Codec.ReadIV128(nonce_bytes, out nonceLow, out nonceHigh);
		}

		public LittleOPC_WrapperE(Key128 key, Block128 nonce)
		{
			this.key = key;
			nonceLow = nonce.First;
			nonceHigh = nonce.Second;
		}

		public bool CanReuseTransform => false;
		public bool CanTransformMultipleBlocks => true;
		public int InputBlockSize => LittleOPC128Codec.BlockSizeBytes;
		public int OutputBlockSize => LittleOPC128Codec.BlockSizeBytes;

		public byte[] Pad(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			int paddingSize = InputBlockSize - (data.Length % InputBlockSize);
			byte[] paddedData = new byte[data.Length + paddingSize];
			Array.Copy(data, paddedData, data.Length);

			for (int i = data.Length; i < paddedData.Length; i++)
			{
				paddedData[i] = (byte)paddingSize;
			}

			return paddedData;
		}

		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(LittleOPC_WrapperE));
			if (inputBuffer == null)
				throw new ArgumentNullException(nameof(inputBuffer));
			if (outputBuffer == null)
				throw new ArgumentNullException(nameof(outputBuffer));
			if (inputOffset < 0 || inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
				throw new ArgumentOutOfRangeException(nameof(inputCount));
			if (outputOffset < 0 || outputOffset + inputCount > outputBuffer.Length)
				throw new ArgumentOutOfRangeException(nameof(outputOffset));
			if ((inputCount % InputBlockSize) != 0)
				throw new CryptographicException("LittleOPC-128 TransformBlock input length must be a multiple of 16 bytes. Use Pad() or TransformFinalBlock-style padding before encryption.");

			int blockCount = inputCount / InputBlockSize;

			Parallel.For(0, blockCount, (index) =>
			{
				int offset = inputOffset + index * InputBlockSize;
				Block128 block = LittleOPC128Codec.ReadBlock128(inputBuffer, offset);
				ulong numberOnce = LittleOPC128Codec.MakeBlockNumberOnce(nonceLow, nonceHigh, (ulong)index);

				using (LittleOPC_Wrapper opcWrapper = new LittleOPC_Wrapper())
				{
					Block128 encryptedBlock = opcWrapper.SingleBlockEncryption(block, key, numberOnce);
					LittleOPC128Codec.WriteBlock128(encryptedBlock, outputBuffer, outputOffset + index * InputBlockSize);
				}
			});

			return inputCount;
		}

		public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			byte[] finalBlock = new byte[inputCount];
			TransformBlock(inputBuffer, inputOffset, inputCount, finalBlock, 0);
			return finalBlock;
		}

		public void Dispose()
		{
			key = default;
			nonceLow = 0;
			nonceHigh = 0;
			disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	public class LittleOPC_WrapperD : ICryptoTransform
	{
		private Key128 key;
		private ulong nonceLow;
		private ulong nonceHigh;
		private bool disposed;

		public LittleOPC_WrapperD(byte[] key_bytes, byte[] nonce_bytes)
		{
			key = LittleOPC128Codec.ReadKey128(key_bytes);
			LittleOPC128Codec.ReadIV128(nonce_bytes, out nonceLow, out nonceHigh);
		}

		public LittleOPC_WrapperD(Key128 key, Block128 nonce)
		{
			this.key = key;
			nonceLow = nonce.First;
			nonceHigh = nonce.Second;
		}

		public bool CanReuseTransform => false;
		public bool CanTransformMultipleBlocks => true;
		public int InputBlockSize => LittleOPC128Codec.BlockSizeBytes;
		public int OutputBlockSize => LittleOPC128Codec.BlockSizeBytes;

		public byte[] Unpad(byte[] data)
		{
			if (data == null || data.Length == 0)
				throw new ArgumentException("Data cannot be null or empty", nameof(data));
			if ((data.Length % InputBlockSize) != 0)
				throw new InvalidOperationException("Padded data length must be a multiple of 16 bytes.");

			int paddingSize = data[data.Length - 1];
			if (paddingSize <= 0 || paddingSize > InputBlockSize)
				throw new InvalidOperationException("Invalid padding size");

			for (int i = data.Length - paddingSize; i < data.Length; i++)
			{
				if (data[i] != paddingSize)
					throw new InvalidOperationException("Invalid padding");
			}

			byte[] unpaddedData = new byte[data.Length - paddingSize];
			Array.Copy(data, unpaddedData, unpaddedData.Length);
			return unpaddedData;
		}

		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(LittleOPC_WrapperD));
			if (inputBuffer == null)
				throw new ArgumentNullException(nameof(inputBuffer));
			if (outputBuffer == null)
				throw new ArgumentNullException(nameof(outputBuffer));
			if (inputOffset < 0 || inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
				throw new ArgumentOutOfRangeException(nameof(inputCount));
			if (outputOffset < 0 || outputOffset + inputCount > outputBuffer.Length)
				throw new ArgumentOutOfRangeException(nameof(outputOffset));
			if ((inputCount % InputBlockSize) != 0)
				throw new CryptographicException("LittleOPC-128 TransformBlock input length must be a multiple of 16 bytes.");

			int blockCount = inputCount / InputBlockSize;

			Parallel.For(0, blockCount, (index) =>
			{
				int offset = inputOffset + index * InputBlockSize;
				Block128 block = LittleOPC128Codec.ReadBlock128(inputBuffer, offset);
				ulong numberOnce = LittleOPC128Codec.MakeBlockNumberOnce(nonceLow, nonceHigh, (ulong)index);

				using (LittleOPC_Wrapper opcWrapper = new LittleOPC_Wrapper())
				{
					Block128 decryptedBlock = opcWrapper.SingleBlockDecryption(block, key, numberOnce);
					LittleOPC128Codec.WriteBlock128(decryptedBlock, outputBuffer, outputOffset + index * InputBlockSize);
				}
			});

			return inputCount;
		}

		public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			byte[] finalBlock = new byte[inputCount];
			TransformBlock(inputBuffer, inputOffset, inputCount, finalBlock, 0);
			return finalBlock;
		}

		public void Dispose()
		{
			key = default;
			nonceLow = 0;
			nonceHigh = 0;
			disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	public class LittleOPCAlgorithm : SymmetricAlgorithm
	{
		public override byte[] Key
		{
			get { return this.KeyValue; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (value.Length != LittleOPC128Codec.KeySizeBytes)
					throw new CryptographicException("LittleOPC-128 Key must be exactly 16 bytes.");
				this.KeyValue = (byte[])value.Clone();
				this.KeySizeValue = this.KeyValue.Length * 8;
			}
		}

		public override byte[] IV
		{
			get { return this.IVValue; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (value.Length != LittleOPC128Codec.IVSizeBytes)
					throw new CryptographicException("LittleOPC-128 IV must be exactly 16 bytes.");
				this.IVValue = (byte[])value.Clone();
			}
		}

		public LittleOPCAlgorithm()
		{
			this.BlockSizeValue = 128;
			this.KeySizeValue = 128;
			this.FeedbackSizeValue = 128;
			this.KeyValue = new byte[LittleOPC128Codec.KeySizeBytes];
			this.IVValue = new byte[LittleOPC128Codec.IVSizeBytes];
			this.LegalBlockSizesValue = new KeySizes[] { new KeySizes(128, 128, 0) };
			this.LegalKeySizesValue = new KeySizes[] { new KeySizes(128, 128, 0) };
		}

		public override ICryptoTransform CreateEncryptor()
		{
			return CreateEncryptor(this.KeyValue, this.IVValue);
		}

		public override ICryptoTransform CreateDecryptor()
		{
			return CreateDecryptor(this.KeyValue, this.IVValue);
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
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				byte[] iv = new byte[LittleOPC128Codec.IVSizeBytes];
				rng.GetBytes(iv);
				this.IVValue = iv;
			}
		}

		public override void GenerateKey()
		{
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				byte[] key = new byte[LittleOPC128Codec.KeySizeBytes];
				rng.GetBytes(key);
				this.KeyValue = key;
				this.KeySizeValue = key.Length * 8;
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
				if (this.KeyValue != null)
				{
					CryptographicOperations.ZeroMemory(this.KeyValue);
					this.KeyValue = null;
				}

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
