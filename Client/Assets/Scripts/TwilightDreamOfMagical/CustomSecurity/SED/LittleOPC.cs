using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TwilightDreamOfMagical.CustomSecurity.CSPRNG;

// Symmetric encryption and decryption
namespace TwilightDreamOfMagical.CustomSecurity.SED
{
	/*
		LittleOPC C# port updated to the current C++ 128-bit public core.

		Public core interface:
		- EncryptionCoreFunction(Block128 data, Key128 key, ulong numberOnce)
		- DecryptionCoreFunction(Block128 data, Key128 key, ulong numberOnce)
		- ResetPRNG(ulong seed)

		The implementation follows the C++ 128-bit block/key shape:
		- Block128 = two 64-bit lanes.
		- Key128   = two 64-bit lanes.
		- XCR key schedule uses two 256-bit XorConstantRotation states.
		- Round subkeys are derived from GenerateSubKey128().
		- NeoAlzette V6.5 second-schedule ARX box is applied diagonally across lanes.
	*/
	public readonly struct Block128 : IEquatable<Block128>
	{
		public readonly ulong First;
		public readonly ulong Second;

		public Block128(ulong first, ulong second)
		{
			First = first;
			Second = second;
		}

		public bool Equals(Block128 other) => First == other.First && Second == other.Second;
		public override bool Equals(object obj) => obj is Block128 other && Equals(other);
		public override int GetHashCode() => First.GetHashCode() ^ BitsOperation.RotateLeft(Second, 17).GetHashCode();
		public static bool operator ==(Block128 left, Block128 right) => left.Equals(right);
		public static bool operator !=(Block128 left, Block128 right) => !left.Equals(right);
	}

	public readonly struct Key128 : IEquatable<Key128>
	{
		public readonly ulong First;
		public readonly ulong Second;

		public Key128(ulong first, ulong second)
		{
			First = first;
			Second = second;
		}

		public bool Equals(Key128 other) => First == other.First && Second == other.Second;
		public override bool Equals(object obj) => obj is Key128 other && Equals(other);
		public override int GetHashCode() => First.GetHashCode() ^ BitsOperation.RotateLeft(Second, 29).GetHashCode();
		public static bool operator ==(Key128 left, Key128 right) => left.Equals(right);
		public static bool operator !=(Key128 left, Key128 right) => !left.Equals(right);
	}

	public class LittleOPC : IDisposable
	{
		private const int rounds = 4;

		private readonly XorConstantRotation xcr = new XorConstantRotation(1);
		private readonly XorConstantRotation xcrSecond = new XorConstantRotation(~1UL ^ BitsOperation.RotateLeft(1UL, 32));
		private readonly List<KeyState> KeyStates = new List<KeyState>(rounds);
		private ulong seed = 1;


		private struct KeyState
		{
			public Key128 Subkey;
			public ulong ChoiceFunction;
			public int BitRotationAmountA;
			public int BitRotationAmountB;
		}

		private static readonly uint[] ROUND_CONSTANTS = new uint[]
		{
			0x16B2C40B, 0xC117176A, 0x0F9A2598, 0xA1563ACA,
			0x243F6A88, 0x85A308D3, 0x13198A2E, 0x03707344,
			0x9E3779B9, 0x7F4A7C15, 0xF39CC060, 0x5CEDC834,
			0xB7E15162, 0x8AED2A6A, 0xBF715880, 0x9CF4F3C7
		};

		private const int CROSS_XOR_ROT_R0 = 22;
		private const int CROSS_XOR_ROT_R1 = 13;

		private static readonly uint RC7_R24 = BitsOperation.RotateRight(ROUND_CONSTANTS[7], 24);
		private static readonly uint RC8_R24 = BitsOperation.RotateRight(ROUND_CONSTANTS[8], 24);
		private static readonly uint RC13_R24 = BitsOperation.RotateRight(ROUND_CONSTANTS[13], 24);
		private static readonly uint RC2_L8 = BitsOperation.RotateLeft(ROUND_CONSTANTS[2], 8);
		private static readonly uint RC3_L8 = BitsOperation.RotateLeft(ROUND_CONSTANTS[3], 8);
		private static readonly uint RC12_L8 = BitsOperation.RotateLeft(ROUND_CONSTANTS[12], 8);
		private static readonly uint MASK0_RC7 = GenerateDynamicDiffusionMask0(ROUND_CONSTANTS[7]);
		private static readonly uint MASK1_RC2 = GenerateDynamicDiffusionMask1(ROUND_CONSTANTS[2]);

		public LittleOPC()
		{
			for (int i = 0; i < rounds; i++)
				KeyStates.Add(new KeyState());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong Pack64(uint hi, uint lo)
		{
			return ((ulong)hi << 32) | lo;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Unpack64(ulong value, out uint hi, out uint lo)
		{
			hi = (uint)(value >> 32);
			lo = (uint)value;
		}


		private void GenerateAndStoreKeyStates(Key128 key128, ulong numberOnce)
		{
			unchecked
			{
				for (ulong round = 0; round < (ulong)rounds; ++round)
				{
					KeyState keyState = KeyStates[(int)round];

					ulong inputLeft = numberOnce ^ round;
					ulong inputRight = (numberOnce ^ (round << 1)) ^ (round >> 1);

					XorConstantRotation.GeneratedSubKey128 outLeft = xcr.GenerateSubKey128(inputLeft);
					XorConstantRotation.GeneratedSubKey128 outRight = xcrSecond.GenerateSubKey128(inputRight);

					ulong a = outLeft.a;
					ulong b = outLeft.b;
					ulong c = outRight.a;
					ulong d = outRight.b;

					ulong subkeyFirst = (key128.First + a) ^ BitsOperation.RotateRight(c, (int)(round & 63UL));
					ulong subkeySecond = (key128.Second - b) ^ BitsOperation.RotateRight(d, (int)((round + 1UL) & 63UL));
					keyState.Subkey = new Key128(subkeyFirst, subkeySecond);

					keyState.ChoiceFunction = (a ^ b ^ c ^ d) & 3UL;

					ulong rotPool =
						(a ^ b) ^ (c ^ d) ^
						BitsOperation.RotateLeft(keyState.Subkey.First, 1) ^
						BitsOperation.RotateLeft(keyState.Subkey.Second, 3);

					keyState.BitRotationAmountA = (int)(rotPool & 63UL);
					keyState.BitRotationAmountB = (int)((rotPool >> 6) & 63UL);

					KeyStates[(int)round] = keyState;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint GenerateDynamicDiffusionMask0(uint value)
		{
			uint v0 = value;
			uint v1 = v0 ^ BitsOperation.RotateLeft(v0, 2);
			uint v2 = v0 ^ BitsOperation.RotateLeft(v1, 17);
			uint v3 = v0 ^ BitsOperation.RotateLeft(v2, 4);
			uint v4 = v3 ^ BitsOperation.RotateLeft(v3, 24);
			return v2 ^ BitsOperation.RotateLeft(v4, 7);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint GenerateDynamicDiffusionMask1(uint value)
		{
			uint v0 = value;
			uint v1 = v0 ^ BitsOperation.RotateRight(v0, 2);
			uint v2 = v0 ^ BitsOperation.RotateRight(v1, 17);
			uint v3 = v0 ^ BitsOperation.RotateRight(v2, 4);
			uint v4 = v3 ^ BitsOperation.RotateRight(v3, 24);
			return v2 ^ BitsOperation.RotateRight(v4, 7);
		}

		private static void CdInjectionFromB(uint b, out uint c, out uint d)
		{
			uint companion0 = BitsOperation.RotateRight(b, 24);
			uint mask = GenerateDynamicDiffusionMask0(b);
			uint companionMask = BitsOperation.RotateRight(mask, 24) ^ MASK0_RC7;
			uint maskR1 = BitsOperation.RotateRight(mask, 5);

			uint x0 = companion0 ^ mask;
			uint x1 = b ^ mask;
			uint view = companion0 ^ companionMask;
			uint bridgeState = BitsOperation.RotateRight(b, 19) ^ (b << 9);

			uint qStateNa = RC7_R24 ^ (~(b & mask));
			uint qCompNo = companion0 ^ b ^ RC8_R24 ^ (~(companion0 | maskR1));
			uint qBridge = bridgeState ^ b ^ RC13_R24 ^ (~(bridgeState & companionMask));
			uint qShared = qStateNa ^ qCompNo;

			uint crossQ = (b ^ maskR1) & BitsOperation.RotateRight(mask ^ companionMask, 7);
			uint antiQ = ((x1 >> 3) ^ (view >> 5) ^ maskR1) & (b ^ BitsOperation.RotateRight(x0, 11));

			c = qShared ^ BitsOperation.RotateRight(qCompNo, 5) ^ BitsOperation.RotateRight(qCompNo, 11) ^ antiQ;
			d = qShared ^ BitsOperation.RotateRight(qStateNa, 5) ^ BitsOperation.RotateRight(qBridge, 13) ^ crossQ ^ antiQ;
		}

		private static void CdInjectionFromA(uint a, out uint c, out uint d)
		{
			uint companion0 = BitsOperation.RotateLeft(a, 8);
			uint mask = GenerateDynamicDiffusionMask1(a);
			uint companionMask = BitsOperation.RotateLeft(mask, 8) ^ MASK1_RC2;
			uint maskR1 = BitsOperation.RotateRight(mask, 5);

			uint x0 = companion0 ^ mask;
			uint x1 = a ^ mask;
			uint view = companion0 ^ companionMask;
			uint bridgeState = BitsOperation.RotateLeft(a, 19) ^ (a >> 9);

			uint qStateNo = RC2_L8 ^ (~(a | mask));
			uint qCompNa = companion0 ^ a ^ RC3_L8 ^ (~(companion0 & maskR1));
			uint qBridge = bridgeState ^ a ^ RC12_L8 ^ (~(bridgeState | companionMask));
			uint qShared = qStateNo ^ qCompNa;

			uint crossQ = (a ^ maskR1) & BitsOperation.RotateLeft(mask ^ companionMask, 13);
			uint antiQ = ((x1 << 3) ^ (view << 5) ^ maskR1) | (a ^ BitsOperation.RotateLeft(x0, 11));

			c = qShared ^ BitsOperation.RotateLeft(qCompNa, 5) ^ BitsOperation.RotateLeft(qCompNa, 11) ^ antiQ;
			d = qShared ^ BitsOperation.RotateLeft(qStateNo, 5) ^ BitsOperation.RotateLeft(qBridge, 13) ^ crossQ ^ antiQ;
		}

		private static void NeoAlzetteForwardLayer(ref uint a, ref uint b)
		{
			unchecked
			{
				uint A = a;
				uint B = b;

				B -= ROUND_CONSTANTS[1];

				CdInjectionFromB(B, out uint c0, out uint d0);
				uint cd0 = (c0 << 2) ^ (d0 >> 2);
				uint cd1 = (c0 >> 5) ^ (d0 << 5);

				A ^= BitsOperation.RotateLeft(B, 24)
					^ BitsOperation.RotateLeft(c0, 16)
					^ BitsOperation.RotateLeft(B, 8);

				A += BitsOperation.RotateLeft(cd0, 31) ^ BitsOperation.RotateLeft(cd1, 17) ^ ROUND_CONSTANTS[0];

				B ^= BitsOperation.RotateLeft(A, CROSS_XOR_ROT_R0) ^ ROUND_CONSTANTS[4];
				A ^= BitsOperation.RotateLeft(B, CROSS_XOR_ROT_R1);

				A -= ROUND_CONSTANTS[6];

				CdInjectionFromA(A, out uint c1, out uint d1);
				uint cd2 = (c1 >> 3) ^ (d1 << 3);
				uint cd3 = (c1 << 1) ^ (d1 >> 1);

				B ^= BitsOperation.RotateRight(A, 24)
					^ BitsOperation.RotateRight(d1, 16)
					^ BitsOperation.RotateRight(A, 8);

				B += cd2 ^ cd3 ^ ROUND_CONSTANTS[5];

				A ^= BitsOperation.RotateLeft(B, 5) ^ ROUND_CONSTANTS[9];
				B ^= BitsOperation.RotateLeft(A, 25);

				A ^= ROUND_CONSTANTS[10];
				B ^= ROUND_CONSTANTS[11];

				a = A;
				b = B;
			}
		}

		private static void NeoAlzetteBackwardLayer(ref uint a, ref uint b)
		{
			unchecked
			{
				uint A = a;
				uint B = b;

				B ^= ROUND_CONSTANTS[11];
				A ^= ROUND_CONSTANTS[10];

				B ^= BitsOperation.RotateLeft(A, 25);
				A ^= BitsOperation.RotateLeft(B, 5) ^ ROUND_CONSTANTS[9];

				CdInjectionFromA(A, out uint c1, out uint d1);
				uint cd2 = (c1 >> 3) ^ (d1 << 3);
				uint cd3 = (c1 << 1) ^ (d1 >> 1);

				B -= cd2 ^ cd3 ^ ROUND_CONSTANTS[5];

				B ^= BitsOperation.RotateRight(A, 24)
					^ BitsOperation.RotateRight(d1, 16)
					^ BitsOperation.RotateRight(A, 8);

				A += ROUND_CONSTANTS[6];

				A ^= BitsOperation.RotateLeft(B, CROSS_XOR_ROT_R1);
				B ^= BitsOperation.RotateLeft(A, CROSS_XOR_ROT_R0) ^ ROUND_CONSTANTS[4];

				CdInjectionFromB(B, out uint c0, out uint d0);
				uint cd0 = (c0 << 2) ^ (d0 >> 2);
				uint cd1 = (c0 >> 5) ^ (d0 << 5);

				A -= BitsOperation.RotateLeft(cd0, 31) ^ BitsOperation.RotateLeft(cd1, 17) ^ ROUND_CONSTANTS[0];

				A ^= BitsOperation.RotateLeft(B, 24)
					^ BitsOperation.RotateLeft(c0, 16)
					^ BitsOperation.RotateLeft(B, 8);

				B += ROUND_CONSTANTS[1];

				a = A;
				b = B;
			}
		}

		private static ulong ConstantTimeEqualMask(ulong x, ulong y)
		{
			unchecked
			{
				ulong q = x ^ y;
				q |= 0UL - q;
				q >>= 63;
				return q - 1UL;
			}
		}

		private static void MixLinearTransformForward(ref ulong lane0, ref ulong lane1, KeyState keyState)
		{
			ulong lane0Case0 = lane0 ^ keyState.Subkey.First;
			ulong lane1Case0 = lane1 ^ keyState.Subkey.Second;

			ulong lane0Case1 = (~lane0) ^ keyState.Subkey.First;
			ulong lane1Case1 = (~lane1) ^ keyState.Subkey.Second;

			ulong lane0Case2 = BitsOperation.RotateLeft(lane0, keyState.BitRotationAmountB);
			ulong lane1Case2 = BitsOperation.RotateLeft(lane1, keyState.BitRotationAmountB);

			ulong lane0Case3 = BitsOperation.RotateRight(lane0, keyState.BitRotationAmountB);
			ulong lane1Case3 = BitsOperation.RotateRight(lane1, keyState.BitRotationAmountB);

			ulong m0 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 0UL);
			ulong m1 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 1UL);
			ulong m2 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 2UL);
			ulong m3 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 3UL);

			lane0 = (lane0Case0 & m0) | (lane0Case1 & m1) | (lane0Case2 & m2) | (lane0Case3 & m3);
			lane1 = (lane1Case0 & m0) | (lane1Case1 & m1) | (lane1Case2 & m2) | (lane1Case3 & m3);
		}

		private static void MixLinearTransformBackward(ref ulong lane0, ref ulong lane1, KeyState keyState)
		{
			ulong lane0Case0 = lane0 ^ keyState.Subkey.First;
			ulong lane1Case0 = lane1 ^ keyState.Subkey.Second;

			ulong lane0Case1 = (~lane0) ^ keyState.Subkey.First;
			ulong lane1Case1 = (~lane1) ^ keyState.Subkey.Second;

			ulong lane0Case2 = BitsOperation.RotateRight(lane0, keyState.BitRotationAmountB);
			ulong lane1Case2 = BitsOperation.RotateRight(lane1, keyState.BitRotationAmountB);

			ulong lane0Case3 = BitsOperation.RotateLeft(lane0, keyState.BitRotationAmountB);
			ulong lane1Case3 = BitsOperation.RotateLeft(lane1, keyState.BitRotationAmountB);

			ulong m0 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 0UL);
			ulong m1 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 1UL);
			ulong m2 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 2UL);
			ulong m3 = ConstantTimeEqualMask(keyState.ChoiceFunction & 3UL, 3UL);

			lane0 = (lane0Case0 & m0) | (lane0Case1 & m1) | (lane0Case2 & m2) | (lane0Case3 & m3);
			lane1 = (lane1Case0 & m0) | (lane1Case1 & m1) | (lane1Case2 & m2) | (lane1Case3 & m3);
		}

		public Block128 EncryptionCoreFunction(Block128 data, Key128 key, ulong numberOnce)
		{
			unchecked
			{
				GenerateAndStoreKeyStates(key, numberOnce);

				ulong lane0 = data.First;
				ulong lane1 = data.Second;

				for (int round = 0; round < rounds; round++)
				{
					KeyState currentKeyState = KeyStates[round];

					// Add round key.
					lane0 ^= currentKeyState.Subkey.First;
					lane1 ^= currentKeyState.Subkey.Second;

					Unpack64(lane0, out uint w0, out uint w1);
					Unpack64(lane1, out uint w2, out uint w3);

					// Diagonal NeoAlzette ARX layer: (w0,w2) and (w1,w3).
					NeoAlzetteForwardLayer(ref w0, ref w2);
					NeoAlzetteForwardLayer(ref w1, ref w3);

					lane0 = Pack64(w0, w1);
					lane1 = Pack64(w2, w3);

					MixLinearTransformForward(ref lane0, ref lane1, currentKeyState);

					lane0 ^= 1UL << currentKeyState.BitRotationAmountA;
					lane1 ^= 1UL << (63 - currentKeyState.BitRotationAmountA);
				}

				return new Block128(lane0, lane1);
			}
		}

		public Block128 DecryptionCoreFunction(Block128 data, Key128 key, ulong numberOnce)
		{
			unchecked
			{
				GenerateAndStoreKeyStates(key, numberOnce);

				ulong lane0 = data.First;
				ulong lane1 = data.Second;

				for (int round = rounds - 1; round >= 0; round--)
				{
					KeyState currentKeyState = KeyStates[round];

					lane0 ^= 1UL << currentKeyState.BitRotationAmountA;
					lane1 ^= 1UL << (63 - currentKeyState.BitRotationAmountA);

					MixLinearTransformBackward(ref lane0, ref lane1, currentKeyState);

					Unpack64(lane0, out uint w0, out uint w1);
					Unpack64(lane1, out uint w2, out uint w3);

					NeoAlzetteBackwardLayer(ref w1, ref w3);
					NeoAlzetteBackwardLayer(ref w0, ref w2);

					lane0 = Pack64(w0, w1);
					lane1 = Pack64(w2, w3);

					lane0 ^= currentKeyState.Subkey.First;
					lane1 ^= currentKeyState.Subkey.Second;
				}

				return new Block128(lane0, lane1);
			}
		}

		public void ResetPRNG(ulong seed)
		{
			this.seed = seed;
			xcr.Seed(seed);
			xcrSecond.Seed((~seed) ^ BitsOperation.RotateLeft(seed, 32));
		}

		public void Dispose()
		{
			for (int i = 0; i < KeyStates.Count; ++i)
				KeyStates[i] = default;

			KeyStates.Clear();
			seed = 0;
		}
	}
}
