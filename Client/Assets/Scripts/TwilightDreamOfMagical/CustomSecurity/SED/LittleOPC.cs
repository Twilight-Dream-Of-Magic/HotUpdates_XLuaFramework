using System;
using System.Collections.Generic;
using TwilightDreamOfMagical.CustomSecurity.CSPRNG;

//Symmetric encryption and decryption
namespace TwilightDreamOfMagical.CustomSecurity.SED
{
	/*
		Implementation of Custom Data Encrypting Worker and Decrypting Worker
		自定义加密和解密数据工作器的实现

		OaldresPuzzle-Cryptic (Type 1)
		隐秘的奥尔德雷斯之谜 (类型 1)
	*/

	public class LittleOPC : IDisposable
	{
		private static readonly uint[] ROUND_CONSTANT = new uint[]
		{
			//1,2,3,5,8,13,21,34,55,89,144,233,377,610,987,1597,2584,4181 (Fibonacci numbers)
			//Concatenation of Fibonacci numbers : 123581321345589144233377610987159725844181
			//Hexadecimal : 16b2c40bc117176a0f9a2598a1563aca6d5
			0x16B2C40B,0xC117176A,0x0F9A2598,0xA1563ACA,

			/*
				Mathematical Constants - Millions of Digits
				http://www.numberworld.org/constants.html
			*/

			//π Pi (3.243f6a8885a308d313198a2e0370734)
			0x243F6A88,0x85A308D3,0x13198102,0xE0370734,
			//φ Golden ratio (1.9e3779b97f4a7c15f39cc0605cedc834)
			0x9E3779B9,0x7F4A7C15,0xF39CC060,0x5CEDC834,
			//e Natural Constant (2.b7e151628aed2a6abf7158809cf4f3c7)
			0xB7E15162,0x8AED2A6A,0xBF715880,0x9CF4F3C7
		};

		private XorConstantRotation xcr = new XorConstantRotation();
		private const int rounds = 8; // Assuming the total number of rounds is 16

		private struct KeyState
		{
			public ulong Subkey;
			public ulong ChoiceFunction;
			public int BitRotationAmountA;
			public int BitRotationAmountB;
			public uint RoundConstantIndex;
		}

		private List<KeyState> KeyStates = new List<KeyState>(rounds);

		private void NeoAlzetteForwardLayer(ref uint a, ref uint b, uint rc)
		{
			b = b ^ a;
			a = BitsOperation.RotateRight(a + b, 31);
			a = a ^ rc;

			b = b + a;
			a = BitsOperation.RotateLeft(a ^ b, 24);
			a = a + rc;

			//a = a - BitsOperation.RotateLeft(b ^ rc, 17);
			//b = b + (a ^ rc);
			//b = b - BitsOperation.RotateRight(a ^ rc, 24);
			//a = a + (b ^ rc);
			b = BitsOperation.RotateLeft(b, 8) ^ rc;
			a = a + b;

			a = a ^ b;
			b = BitsOperation.RotateRight(a + b, 17);
			b = b ^ rc;

			a = a + b;
			b = BitsOperation.RotateLeft(a ^ b, 16);
			b = b + rc;
		}

		private void NeoAlzetteBackwardLayer(ref uint a, ref uint b, uint rc)
		{
			b = b - rc;
			b = BitsOperation.RotateRight(b, 16) ^ a;
			a = a - b;

			b = b ^ rc;
			b = BitsOperation.RotateLeft(b, 17) - a;
			a = a ^ b;

			a = a - b;
			b = BitsOperation.RotateRight(b ^ rc, 8);
			//a = a - (b ^ rc);
			//b = b + BitsOperation.RotateRight(a ^ rc, 24);
			//b = b - (a ^ rc);
			//a = a + BitsOperation.RotateLeft(b ^ rc, 17);

			a = a - rc;
			a = BitsOperation.RotateRight(a, 24) ^ b;
			b = b - a;

			a = a ^ rc;
			a = BitsOperation.RotateLeft(a, 31) - b;
			b = b ^ a;
		}

		private void GenerateAndStoreKeyStates(ulong key, ulong numberOnce)
		{
			uint roundConstantIndex = 0;
			for (int round = 0; round < rounds; round++)
			{
				KeyState keyState = this.KeyStates[round];

				keyState.Subkey = key ^ xcr.GenerateRandomNumber(numberOnce ^ (ulong)round);
				keyState.ChoiceFunction = xcr.GenerateRandomNumber(keyState.Subkey ^ (key >> 1));
				keyState.BitRotationAmountA = (int)(xcr.GenerateRandomNumber(keyState.Subkey ^ keyState.ChoiceFunction) % 64);
				keyState.BitRotationAmountB = (int)((xcr.GenerateRandomNumber(keyState.Subkey ^ keyState.ChoiceFunction) >> 6) % 64);
				keyState.ChoiceFunction %= 4;

				keyState.RoundConstantIndex = (uint)(roundConstantIndex >> 1) % 16;
				roundConstantIndex += 2;

				this.KeyStates[round] = keyState;
			}
		}

		public ulong EncryptionCoreFunction(ulong data, ulong key, ulong numberOnce)
		{
			GenerateAndStoreKeyStates(key, numberOnce);

			ulong result = data;

			for (int round = 0; round < rounds; round++)
			{
				KeyState keyState = KeyStates[round];

				uint leftValue = (uint)(result >> 32);
				uint rightValue = (uint)result;
				NeoAlzetteForwardLayer(ref leftValue, ref rightValue, ROUND_CONSTANT[keyState.RoundConstantIndex]);
				result = ((ulong)leftValue << 32) | rightValue;

				/*
					Mix Linear Transform Layer (Forward)
				*/
				switch (keyState.ChoiceFunction)
				{
					case 0:
						result ^= keyState.Subkey;
						break;
					case 1:
						result = ~result ^ keyState.Subkey;
						break;
					case 2:
						result = BitsOperation.RotateLeft(result, keyState.BitRotationAmountB);
						break;
					case 3:
						result = BitsOperation.RotateRight(result, keyState.BitRotationAmountB);
						break;
					default:
						// Optionally handle invalid choice function values
						throw new InvalidOperationException("Invalid choice function value.");
				}

				// Random Bit Tweak (Nonlinear)
				result ^= ((ulong)(1) << (keyState.BitRotationAmountA % 64));

				// Add Round Key (Key Mix)
				result += (key ^ keyState.Subkey);
				result = BitsOperation.RotateRight(result ^ key, 16);
				result ^= BitsOperation.RotateLeft(key + keyState.Subkey, 48);
			}

			return result;
		}

		public ulong DecryptionCoreFunction(ulong data, ulong key, ulong numberOnce)
		{
			GenerateAndStoreKeyStates(key, numberOnce);

			ulong result = data;

			for (int round = rounds - 1; round >= 0; round--)
			{
				KeyState keyState = KeyStates[round];

				// Subtract Round key (Key UnMix)
				result ^= BitsOperation.RotateLeft(key + keyState.Subkey, 48);
				result = BitsOperation.RotateLeft(result, 16) ^ key;
				result -= (key ^ keyState.Subkey);

				// Reverse Random Bit Tweak (Nonlinear)
				result ^= ((ulong)(1) << (keyState.BitRotationAmountA % 64));

				switch (keyState.ChoiceFunction)
				{
					case 0:
						result ^= keyState.Subkey;
						break;
					case 1:
						result = ~result ^ keyState.Subkey;
						break;
					case 2:
						result = BitsOperation.RotateRight(result, keyState.BitRotationAmountB);
						break;
					case 3:
						result = BitsOperation.RotateLeft(result, keyState.BitRotationAmountB);
						break;
					default:
						// Optionally handle invalid choice function values
						throw new InvalidOperationException("Invalid choice function value.");
				}

				uint leftValue = (uint)(result >> 32);
				uint rightValue = (uint)result;
				NeoAlzetteBackwardLayer(ref leftValue, ref rightValue, ROUND_CONSTANT[keyState.RoundConstantIndex]);
				result = ((ulong)leftValue << 32) | rightValue;
			}

			return result;
		}

		public LittleOPC()
		{
			for (int i = 0; i < rounds; i++)
			{
				KeyStates.Add(new KeyState());
			}
		}

		public void ResetPRNG(ulong seed)
		{
			xcr.Seed(seed);
		}

		public void Dispose()
		{
			KeyStates.Clear();
		}
	}
}

