using System;
using System.Runtime.CompilerServices;

namespace TwilightDreamOfMagical.CustomSecurity.CSPRNG
{
	/// <summary>
	/// C# port of the current C++ XorConstantRotation core.
	/// Public API is kept compatible with the old C# version: Seed(), GenerateRandomNumber().
	/// New 128-bit subkey generation is exposed internally for LittleOPC's updated schedule.
	/// </summary>
	public sealed class XorConstantRotation
	{
		public const ulong XCR_CSPRNG_DEFAULT_INITIALIZE_CONSTANT = 0xADB136136669D153UL;
		public const ulong COUNTER_STEP = 0xC8522A96E53AF749UL;

		public readonly struct GeneratedSubKey128
		{
			public readonly ulong a;
			public readonly ulong b;

			public GeneratedSubKey128(ulong left, ulong right)
			{
				a = left;
				b = right;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ulong GetBit(int bitIndex)
			{
				return bitIndex < 64
					? (a >> bitIndex) & 1UL
					: (b >> (bitIndex - 64)) & 1UL;
			}
		}

		private ulong w;
		private ulong x;
		private ulong y;
		private ulong z;
		private ulong counter;

		private static readonly ulong[] XCR_ROUND_CONSTANTS = new ulong[]
		{
			0x01B70C8E97AD5F98UL, 0x243F6A8885A308D3UL, 0x9E3779B97F4A7C15UL, 0xB7E151628AED2A6AUL,
			0xC7FED6D75DF59AE9UL, 0x20B23E1962E49836UL, 0xE91E4940FE913BB5UL, 0x97DE4BFF5DFC30BAUL,
			0xEA49951290EA2540UL, 0xB3383D9DAB30AE94UL, 0x5621F313742832D0UL, 0x995116B66478BBA4UL,
			0x010441338F759DB8UL, 0x536398D10308029DUL, 0x2894ED6378C5FB69UL, 0xAD85FA88C56D7B09UL,
			0xB86C2492A5407DA3UL, 0x1FD735150A7FC7F8UL, 0x02C0801E8D2AD5C4UL, 0x345B92DDA0378C7BUL,
			0xC858E896FACBD0ECUL, 0x0D7E068FF47062A5UL, 0x4DC82F0A88EABA49UL, 0xDEAD923252D8B51EUL,
			0x02236F27027EEC7BUL, 0x8BFD5A2CD7D0DFE1UL, 0xB2D91055E1680EC7UL, 0x447F69D7BBB2DE96UL,
			0xF24675A6731D39B1UL, 0x74E6924E74F3F522UL, 0x273D55B4F2F77942UL, 0x4EC79E443FE532AEUL,
			0x29884015291E47CDUL, 0xF535CA62C5946508UL, 0x194D875B9340054EUL, 0x7DFF309C505E61D3UL,
			0x33F089308E7D76F6UL, 0x66D8ACA59455423DUL, 0x5443A4614FB8B79AUL, 0x8860A0331815A37FUL,
			0x94490B5C348488E3UL, 0x2ACA9822E5516650UL, 0x733050D3DD13B717UL, 0xF333FFB03119BCFBUL,
			0x8994C64CC9CE655BUL, 0xD1C78BB7FBF62CBCUL, 0x9CE1B356D7FD08EAUL, 0x529095A30FB494BCUL,
			0x894413382F7750FBUL, 0x8E244AF81EEA7291UL, 0x8F678CFC9B2D509DUL, 0x9B3C82A2EC6BB9A6UL,
			0x7CEC3E3BDDC61B85UL, 0x2EA6563B009E077EUL, 0x12A5327207526F19UL, 0xC4CB57D77BEF7619UL,
			0xE7C7EDF59A172E38UL, 0x356B6D4BBB9E2252UL, 0x606A34FCAF87622BUL, 0x8614CB5CDABD7B2EUL,
			0x6601482C230D6A6BUL, 0x7C5670B353A5474AUL, 0xD6632CFEC4C04A7EUL, 0x128F463DEAFA672BUL,
			0x5C7AD57132BE6D99UL, 0x66D84709D4C2C125UL, 0xDB7713D125404702UL, 0xDA87B7F935C26251UL,
			0x0283A7E70ABEF3FCUL, 0x00A2056879C6D403UL, 0xB581BDA3F9472B77UL, 0x546B6F54C7E6D880UL,
			0xB95D731A19E3A5E5UL, 0xFC7B4FB032470FD8UL, 0x280588BCD42AA267UL, 0x788B5830D262515AUL,
			0x6B85D6C080678F74UL, 0xEA8ED898D6ECB981UL, 0x67D60E41BB8008C2UL, 0x6773F7F99E997275UL,
			0xD620FC33328703F4UL, 0xDB5D284693D0C501UL, 0xCCD1485FFEE19DDCUL, 0xBA423BF2CFCBF84DUL,
			0x10038A54845BC465UL, 0x72F9BCB93F278861UL, 0xCF5610E8D98EEA9FUL, 0xA8C611FF76712442UL,
			0x8F5107F7835A2743UL, 0xE582E5371D41F3A6UL, 0x2A6A41817AD64A1FUL, 0xE1284D87D0461DD8UL,
			0x5CC60669D6133C09UL, 0x5A36FEC5CD11724EUL, 0x645ED91CEFFB0FE1UL, 0x29480DE78F4A1F66UL,
			0x0AD6643FCC72994BUL, 0x5EF91E9D64C388EFUL, 0x0934F85BF11C3A26UL, 0x924252B668E465E4UL,
			0xF2C4E7935494BE68UL, 0xF5303D0FDB1CEF2AUL, 0x6284E1F5EFEDE4A2UL, 0x5D51ABE2CC906EB3UL,
			0x07D0A125283799BAUL, 0xF1857876B708C71DUL, 0xCA7EDCCA5A57F378UL, 0x1606487FB80BA32BUL,
			0xC880E89D05A25051UL, 0x9F11B85E1B76708DUL, 0x20E033151E08C2C9UL, 0x080446531ED145A5UL,
			0x89D051C6AB320D8AUL, 0x09CF59743A58763FUL, 0xAFD20C8650719FC6UL, 0x8C9827C610851D36UL,
			0xE6823B3EE36B03ECUL, 0x9B88E73002CFAF2FUL, 0x3259D6E322745DC5UL, 0xD4380355D061E914UL,
			0x7EC7B283D4E5CF83UL, 0x3BC07BBE3D5DE00EUL, 0x03B76CA9748B0829UL, 0x996D690FC620872BUL,
			0x84FD544E588C6DC5UL, 0x62F56A662AF4C27AUL, 0xECBF3492B48777BEUL, 0xEB022EE345562417UL,
			0xD64F319FAF3877BFUL, 0x10F6AFC24765674EUL, 0x462FB541297E4E47UL, 0x5830E8FB6298DDC5UL,
			0x72177A6CA01D4160UL, 0xC9EC7DB4C500D928UL, 0xF07669F3BD4BDBF1UL, 0x10BCA5BE25E4C519UL,
			0xEA9957226B48CABEUL, 0x32BDEC964180AA7CUL, 0x73060EC748740DDFUL, 0x2F3740A4B808F6F3UL,
			0x5B5F24C00870223CUL, 0x37834E526EB85528UL, 0x342B94C1D614E690UL, 0x0C8F744AF722738AUL,
			0x25B1FE35877A6681UL, 0x96CA440448A0DD4AUL, 0x83C0F58D0D952779UL, 0x978ACE71C34FCB6BUL,
			0xE85605CA4ACEB32CUL, 0x2E4ED59FB92B0BCFUL, 0xBA1F41B35B1EEA12UL, 0xB44B1352B69058CDUL,
			0xBA3CECA3E27D32F9UL, 0x5A7441C6528131E9UL, 0xEDEB8055C017C2D7UL, 0x147EFC6CB7DAC993UL,
			0x3690BF3B60CFBF1FUL, 0xFEDA458D3B67D4DEUL, 0xD9BCB6B2FB353FF0UL, 0x906FE8490F900801UL,
			0x4B71ED710366B9F0UL, 0xC969EF0A4E9C1107UL, 0x90572C255C96834FUL, 0x5F3EB2EDBE4AC0B3UL,
			0x634F3A1B98D9933DUL, 0x6F0F361EE182539EUL, 0xEBEA4C97F7C64D8AUL, 0x1CD3159CA76B6D85UL,
			0xCDA3A82A5D66A4A5UL, 0x85339303BB54B830UL, 0x349F8B78600A084CUL, 0x1C3F55FE2AF85F36UL,
			0x02B1D6D131C2CA0DUL, 0x865732E0782DCFF9UL, 0x275A0B26C8078906UL, 0xCC2FD8020350FD07UL,
			0xE475319860CBEE73UL, 0xC9C6E475780BBA5CUL, 0xF7DDB43214F431B2UL, 0xA60FD32705228CB7UL,
			0xC2D119CC67A24DBAUL, 0xA590929D94E6373EUL, 0x64102108958672A8UL, 0xFF38B0E1252EEDACUL,
			0x551540277911DFC8UL, 0x7E82148109CCE0EFUL, 0xD029B9DF9EAEDB44UL, 0x42840C2794B543A8UL,
			0xD6F053A12F2AD5BEUL, 0x666157855DB0FD29UL, 0x2C8364D46812DD0FUL, 0x3A2BF7697FE971C9UL,
			0xB217B28F07654AFDUL, 0x0964D6A25E0C0674UL, 0x6B78D335C4B26354UL, 0xB3A0D240490834BBUL,
			0x7CBDF7E25CF5E2E8UL, 0x66096B450186248DUL, 0x3F957DC892BAB775UL, 0x204BE476CB0E6204UL,
			0x2E93270C352877A0UL, 0xCF3C54FCF7E25340UL, 0xED466B3A6AA99784UL, 0x687EE87389D3CE44UL,
			0xFCE153FC6F3F0A12UL, 0x538554FA09D45B61UL, 0x0EB22BDBCBD2E657UL, 0x99EEFF7B2DD6AF18UL,
			0x9820D3F84A539881UL, 0xF1CA001532E7C261UL, 0x7FED339969BD2C3CUL, 0xBCF5B28DC2C218C9UL,
			0xCFB439DA5C0AA91AUL, 0x07C8C143FEB901CEUL, 0xC9DFEBACC4962FE9UL, 0xA578A3E9FBB8B2C5UL,
			0x0B70DD4A8D1FF46CUL, 0xD50D0CA3E67516A6UL, 0x1B0D9AB302075E3AUL, 0xCC65E117FD8E4C5BUL,
			0xF02B8F19187DFD52UL, 0x14180B374125AA9FUL, 0x953C0C06D6A98A04UL, 0x4057B8BB6C200930UL,
			0x0DC2F04B27661C6FUL, 0xA1DF060A256B02F7UL, 0x010CF06E893F27A5UL, 0xD1A080D145080455UL,
			0x229FF6B6E7640DF1UL, 0xD24B66100D9479F5UL, 0xCEB0B1F750E7CEB0UL, 0xE2E6DBECE5714183UL,
			0xF67045AE46918AFDUL, 0x7A872DEA312A9FD8UL, 0x94297BDD53AB20F8UL, 0x4DAE2EF810BBE342UL,
			0xC1054FF43AD21FC4UL, 0x26EBF6433551B4B2UL, 0x11E3A876D6A1351FUL, 0x295D26F5462909ACUL,
			0x402C466E92A438EAUL, 0x1CC8E5C9DA286632UL, 0xE43FD84C98AF9EAAUL, 0x70C12A97AE6D8A4EUL,
			0x7B68298191A178B8UL, 0xFE27C41B52815151UL, 0xB7CB37F9A5786EE3UL, 0x8B7EC6586BFAF9E4UL,
			0x16EA247A1B8EFF1EUL, 0x7A08EEE72CE4DC99UL, 0xF8563C4F27161A69UL, 0x67776334A28721A1UL,
			0x143997E86DE1F9B6UL, 0x1C00F33CA5D83E43UL, 0x0ECBF99E2F8DA830UL, 0xBCB6A5B4641902AEUL,
			0x5ADD72AF71EC10DAUL, 0x3CB671FB55910ACBUL, 0xB556CBE0C235C663UL, 0xBF7582D30C7B7B9BUL,
			0xE6419E49D98970DBUL, 0x94C02A4E57E78417UL, 0x8A90E51246212FC6UL, 0x08E74B091203EBA1UL,
			0xBC8E2B48BE85990DUL, 0x60984E2A9011EFEAUL, 0x7F13660985641607UL, 0xA70628A5966DBE6AUL,
			0x11668A250121376FUL, 0x98E2FB9BA6830B7DUL, 0xFBCB881BD8E04362UL, 0x818B615D377D79E8UL,
			0xD363EAF614A598AAUL, 0xB98EF05D4F0FA7A0UL, 0xA1E43DEB0E801EE6UL, 0x09E89D231CE3C300UL,
			0x32B97DCC525C6C1CUL, 0xE783951DF5595388UL, 0xD19BB2B7EC16093AUL, 0x66C4177F7C1D79BFUL,
			0x7AC7EEF4150FB82CUL, 0xAD5E164AB4C0D474UL, 0x85B10215B554D1FBUL, 0xE075A3032FA906DDUL,
			0xA80EB743BC799201UL, 0x0F01944C84B85959UL, 0x757529145922CD25UL, 0x19E0EADC8379BA3CUL,
			0x1E72937F49943A41UL, 0x136C3DB980BB2DDEUL, 0x2900FFFFEBE647DDUL, 0xC292F1AED24BB838UL,
			0x6DC406A8D9A846A8UL, 0x9D4D0D3846AE9FCBUL, 0xB1B5B679EC677686UL, 0x2DA508234E18B51BUL,
			0xB67732180C341ACAUL, 0x4FFB9BE0B5EE605AUL, 0x15D5F9E38F2044CBUL, 0x352C030D11302197UL,
		};

		private static readonly int ROUND_CONSTANT_SIZE = XCR_ROUND_CONSTANTS.Length;

		public XorConstantRotation()
		{
			w = XCR_CSPRNG_DEFAULT_INITIALIZE_CONSTANT;
			x = 0;
			y = 0;
			z = 0;
			counter = COUNTER_STEP;
			StateInitialize();
		}

		public XorConstantRotation(ulong seed)
		{
			w = seed;
			x = 0;
			y = 0;
			z = 0;
			counter = COUNTER_STEP;
			StateInitialize();
		}

		public void Seed(ulong seed)
		{
			x = 0;
			y = 0;
			z = 0;
			w = seed;
			counter = COUNTER_STEP;
			StateInitialize();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte LiftedCarryAdd8(byte a, byte k)
		{
			byte p = (byte)(a ^ k);
			byte g = (byte)(a & k);
			byte n1 = (byte)(p >> 1);
			byte n2 = (byte)((p >> 2) ^ (p >> 3));

			byte w1 = g;
			byte w2 = (byte)(w1 & n1);
			byte w4 = (byte)(w2 & n2);

			return (byte)(p ^ (w1 << 1) ^ (w2 << 2) ^ (w4 << 4));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte LiftedBorrowSub8(byte a, byte k)
		{
			byte p = (byte)(a ^ k);
			byte h = (byte)((~a) & k);
			byte r = (byte)(~p);
			byte n1 = (byte)(r >> 1);
			byte n2 = (byte)((r >> 2) ^ (r >> 3));

			byte w1 = h;
			byte w2 = (byte)(w1 & n1);
			byte w4 = (byte)(w2 & n2);

			return (byte)(p ^ (w1 << 1) ^ (w2 << 2) ^ (w4 << 4));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte LtamL8(byte a, byte k)
		{
			return (byte)(a ^ ((a << 1) & k) ^ ((a << 2) & k) ^ ((a << 4) & k));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte LtamR8(byte a, byte k)
		{
			return (byte)(a ^ ((a >> 1) & k) ^ ((a >> 2) & k) ^ ((a >> 4) & k));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte Btm8(byte a, byte k)
		{
			return LtamR8(LtamL8(a, k), k);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte Hybrid8Sa(byte value)
		{
			value = Btm8(value, 0x2B);
			value = LiftedBorrowSub8(value, 0x3F);
			value = Btm8(value, 0x2B);
			value = LiftedCarryAdd8(value, 0x3F);
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte Hybrid8Sb(byte value)
		{
			value = LiftedCarryAdd8(value, 0x3F);
			value = Btm8(value, 0x2B);
			value = LiftedBorrowSub8(value, 0x3F);
			value = Btm8(value, 0x2B);
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong BigS64(ulong value, int byteOffset)
		{
			ulong result = 0;
			for (int i = 0; i < 8; ++i)
			{
				byte input = (byte)((value >> (i * 8)) & 0xFFUL);
				byte output = (((byteOffset + i) & 1) == 0) ? Hybrid8Sa(input) : Hybrid8Sb(input);
				result |= ((ulong)output) << (i * 8);
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong ShadowCarry64(ulong a, ulong b)
		{
			unchecked
			{
				ulong p = a ^ b;
				ulong c = a & b;
				ulong q = p;

				c ^= q & (c << 1);
				q &= q << 1;
				c ^= q & (c << 2);

				return p ^ (c << 1) ^ (c << 2);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong ShadowBorrow64(ulong a, ulong b)
		{
			unchecked
			{
				ulong p = a ^ b;
				ulong c = (~a) & b;
				ulong q = ~p;

				c ^= q & (c << 1);
				q &= q << 1;
				c ^= q & (c << 2);

				return p ^ (c << 1) ^ (c << 2);
			}
		}

		private void PermutationARX(ulong numberOnce)
		{
			unchecked
			{
				ulong ww = x ^ numberOnce;
				ulong xx = y ^ BitsOperation.RotateLeft(numberOnce, 9);
				ulong yy = z ^ BitsOperation.RotateLeft(numberOnce, 27);
				ulong zz = w ^ BitsOperation.RotateLeft(numberOnce, 43);

				ulong rc0 = XCR_ROUND_CONSTANTS[(int)(numberOnce % (ulong)ROUND_CONSTANT_SIZE)];
				ulong rc1 = XCR_ROUND_CONSTANTS[(int)(counter % (ulong)ROUND_CONSTANT_SIZE)];
				ulong rc2 = XCR_ROUND_CONSTANTS[(int)((numberOnce + counter) % (ulong)ROUND_CONSTANT_SIZE)];
				ulong rc3 = XCR_ROUND_CONSTANTS[(int)((numberOnce ^ BitsOperation.RotateLeft(numberOnce ^ counter, 3)) % (ulong)ROUND_CONSTANT_SIZE)];

				z = xx ^ rc0;
				w = yy ^ rc1;
				x = zz ^ rc2;
				y = ww ^ rc3;

				if ((counter & 3UL) == 0UL)
				{
					yy = BigS64(yy ^ x, 0);
					zz = BigS64(zz ^ y, 8);
					ww = BigS64(ww ^ z, 16);
					xx = BigS64(xx ^ w, 24);
				}
				else
				{
					yy = ShadowCarry64(yy, x);
					zz = ShadowBorrow64(zz, y);
					ww = ShadowCarry64(ww, z);
					xx = ShadowBorrow64(xx, w);
				}

				x ^= BitsOperation.RotateLeft(xx, 7) ^ (BitsOperation.RotateLeft(yy, 19) ^ zz);
				y ^= BitsOperation.RotateLeft(yy, 11) ^ (BitsOperation.RotateLeft(zz, 23) ^ ww);
				z ^= BitsOperation.RotateLeft(zz, 17) ^ (BitsOperation.RotateLeft(ww, 29) ^ xx);
				w ^= BitsOperation.RotateLeft(ww, 13) ^ (BitsOperation.RotateLeft(xx, 31) ^ yy);
			}
		}

		private GeneratedSubKey128 StateIteration(ulong numberOnce)
		{
			unchecked
			{
				PermutationARX(numberOnce);
				GeneratedSubKey128 output = new GeneratedSubKey128(x ^ y, z ^ w);
				counter += COUNTER_STEP;
				return output;
			}
		}

		public GeneratedSubKey128 GenerateSubKey128(ulong numberOnce)
		{
			return StateIteration(numberOnce);
		}

		public ulong GenerateRandomNumber(ulong numberOnce)
		{
			GeneratedSubKey128 output = StateIteration(numberOnce);
			return output.a ^ BitsOperation.RotateLeft(output.b, 17);
		}

		private void StateInitialize()
		{
			unchecked
			{
				ulong nonzeroFlag = (w | (0UL - w)) >> 63;
				ulong isZero = nonzeroFlag ^ 1UL;
				w += isZero;

				ulong backupSeed = w;

				ulong Ggm64Rounds(ulong seed64)
				{
					const ulong WARMUP = 0x5741524D5550UL;
					ulong out64 = seed64;

					for (int round = 0; round < 2; ++round)
					{
						ulong next64 = 0;

						for (int bitIndex = 0; bitIndex < 64; ++bitIndex)
						{
							ulong input = (WARMUP << 48)
								^ (out64 << 32)
								^ (out64 << 16)
								^ ((ulong)round << 8)
								^ (ulong)bitIndex;

							GeneratedSubKey128 output = StateIteration(input);
							ulong bit = output.GetBit(127);
							next64 = (next64 << 1) | bit;
						}

						out64 = next64;
					}

					return out64;
				}

				ulong leftInput = backupSeed ^ XCR_ROUND_CONSTANTS[ROUND_CONSTANT_SIZE - 1];
				ulong rightInput = backupSeed ^ XCR_ROUND_CONSTANTS[ROUND_CONSTANT_SIZE - 2];

				ulong leftLeaf = Ggm64Rounds(leftInput);
				ulong lx = x;
				ulong ly = y;
				ulong lz = z;

				x = 0;
				y = 0;
				z = 0;
				w = backupSeed;
				counter = COUNTER_STEP;

				ulong rightLeaf = Ggm64Rounds(rightInput);
				ulong rx = x;
				ulong ry = y;
				ulong rz = z;

				x = 0;
				y = 0;
				z = 0;
				w = backupSeed;
				counter = COUNTER_STEP;

				x = lx ^ rx;
				y = ly ^ ry;
				z = lz ^ rz;
				w = backupSeed ^ (leftLeaf ^ rightLeaf);
			}
		}
	}
}
