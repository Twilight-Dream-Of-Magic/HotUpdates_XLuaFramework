using System;
using XLua.Cast;

namespace TwilightDreamOfMagical.CustomSecurity.CSPRNG
{
	public class XorConstantRotation
	{
		private ulong counter;
		private ulong state;
		private ulong x;
		private ulong y;

		private static readonly ulong[] ROUND_CONSTANT = new ulong[]
		{
			//Concatenation of Fibonacci numbers., π, φ, e
			0x01B70C8E97AD5F98,0x243F6A8885A308D3,0x9E3779B97F4A7C15,0xB7E151628AED2A6A,

			//x ∈ [1, 138]
			//f(x) = (e^x - cos(πx)) * (φx^2 - φx - 1) * (√2x - floor(√2x)) * (√3x - floor(√3x)) * ln(1+x) * (xδ - floor(xδ)) * (xρ - floor(xρ))
			0x6a433d2ae48d4c90,0x9e2b6e6880ad26da,0x5380e7890f281d86,0x47ea9e01d8ef7c3c,
			0xb7cfc42c4640a591,0x8ba869f86f575f77,0x66ff83fd9954772c,0x0552755b7ef8c3f6,
			0xe4931d40d079c5cb,0xd6065bf025a81d13,0x586ceb7761d284af,0x5407a44155b8e341,
			0x7810f48181dff9e2,0x0f44524582d1d6cf,0x919ad67c2cd7118c,0x926d94a3923cb938,
			0xc3f400bd67479e59,0x83cb03ba7366b70e,0x629043e6e5712e5c,0x69589ff399736efb,
			0x834d96f80eea56d7,0x02992cb1835476aa,0x78502c2a1b947013,0xbca81dad05eac8c7,
			0x43216fe770f57c2d,0x604a5ccfe888eef1,0xfcf5bdd0ea8a112c,0xeb13dc4ba7327617,
			0xf8587cc0dd587813,0x092b98e058140b26,0x1e044153ec902650,0xd13ef3afb71efc3e,
			0x55af3f5bca28309e,0xcf478054be1173c8,0x99bb2b591f35ac72,0xd3f5e092a0c7c2bb,
			0xdc120bced1935766,0xbb2525cf28193ea8,0x6a06eb360550e537,0x4501817d5023f9bb,
			0x6c9e6ef207e06420,0xa12e023656301669,0x2692fa5ed25b6a2b,0xeb48ef08fd6fbdb7,
			0xfe8db57151c600fb,0x51197bfba60c36ff,0xe95328ef18701542,0x0663e86118debfdd,
			0xee0b0fcbaf12d0d0,0xc92c72f7a14c35ea,0x21ca0bd30529c74c,0x70243d7854330319,
			0x193b70b72995d737,0xa936acbbbe88f426,0x61da22530a461898,0x49afa0f477bda24c,
			0x795bbbc0bf0cdc23,0x3b5f4cf676e0fc41,0xdeec67413dc24105,0x1af46f766498679d,
			0xa9f37172c15f8e20,0x292b237adf6467a9,0x09538ddc3733c79e,0xde5c2f22b2c1aa42,
			0x6204c7ebee5a90d8,0x4359ac75de286849,0x7e616650ab318ae8,0xd7552e509ab0d5a6,
			0xffaf2a408f8cfa95,0x4289e66a0b74427e,0xc5e9869af1856c6d,0x336aa2e2b3dbfeda,
			0x9835ff10bf4b7e3c,0xc0c5d995789a9c04,0x09dce0a22fccbe60,0x7cc16b5458b38ec9,
			0x880d6019ab1aa3fa,0xb9ac43e6d90c89dc,0xe0c876bea28b38be,0xafca75b1c80bc8fa,
			0xf4e5b08059acb0bd,0x643587ac551f3aa0,0x83fa523817844ac9,0x3e97eca86cc41268,
			0xd53517b095a47a79,0x418aaab53810d432,0xde9ad8739ba769b7,0x6f53b6fb08b9809c,
			0xe5d41d82eb6a0d63,0x42137200d3b75b64,0x9ee670cd25143c29,0xdc2b3edf3617c034,
			0xf5d6d70093472506,0xeaca4e8f7eaa4b68,0x0e7b78a6eca0e67e,0x67db9133f144d92d,
			0xa2f043bdf0bfc70d,0x679513157c68480e,0xc7359f77d43ecedb,0xa73610dd579db5e8,
			0xd33f00a73c40b3f4,0x1f6693cdc79f41cf,0x402aba3326ff09e4,0xc2f06d96a33ed417,
			0x16882cd0ac38796e,0xde2342960e538c6e,0xee16a05c0f946350,0xb76895e14d9f81b0,
			0x8d8e566bbc5b2b65,0x1b1881ca8831ba3c,0x0fb99dab44900c06,0x51701c39eabb7550,
			0x98c5cadd4f0446cd,0x12cd6ac42824463f,0x815f799d0d2b6b8d,0xd34bed6a3284fb8f,
			0x1f4f71425e521345,0x5ec3427cc37ef4b7,0x41ca4c3fbb4ae014,0x4d4a5a8399958a44,
			0x6f21b526d0c7ee3c,0xe85d52cfba2818c0,0x09d0b2cc4deccc35,0x1b13c064ccec4d2e,
			0x92b538d3b747c6ac,0x58719d59011b3fae,0xedde21671368f97e,0xfc4dbeff22c77aab,
			0x66997342600d0997,0x6a173e62da2821d7,0xe657b797f1f23506,0x7052226e4dde4ce0,
			0xcec9d219091d3713,0x46b20fcd9abd9b13,0x0a8bbb7b077261a8,0x8cf03c3c366533db,
			0x9d167cec4a7f4953,0xed8bbf927c48dbf9,0x21e8d4a1dd84e782,0x4ac104ee6fa65e69,
			0x5cb955963da25bee,0xa0f791f755ed9ead,0x1125fa77491b7c6a,0x3c0560dc8d08a6b6,
			0x20cb39c7b8690d0c,0x29a3a26ccc8540de,0x3ba44a4cbb906982,0xddf9454bc0acb110,
			0xa989a47d915cc360,0xb90af4a05b78e702,0x7f20b78fb8d8eae8,0xedb6cb8180b81603,
			0xdfe86decf8f940b5,0x4c6baf1de449fc4d,0x165f86d08961df51,0x4c038e6a96040825,
			0xf4f2cb95b6276944,0xe7f98f0aae90ff54,0xd90fc39cae09f82e,0x45ef9b03350e102c,
			0xba319140b8a35152,0xa1c8bf3071254d17,0x6d942b49712b2ff0,0x687ab4e1a35f3a7f,
			0x8fa2a50edfdfce2d,0x1b123d5c5ba08e5b,0x287209f7e4ad4cd4,0xaae61796f1414dd9,
			0xabd88a4167ec1728,0x584654213d59d9ac,0x1010e8491f4e2d7d,0x01b6087b68d105e5,
			0xd478306668f2aed3,0x35b78cf5c30272db,0x4e9b1bd35706711d,0xfbee714f84a270e5,
			0x8855b3fe8d108055,0x1829c0415ef92080,0x2a6238b05b1e17f1,0x270e32a624ce5105,
			0x03a089b9cf427251,0x468ff8821f5007cd,0xf3f13de46ea0de52,0x2353e2eb32dd119c,
			0x5deef337d58f8050,0x4627b46ab323ee76,0x6bc50f6c85bf5ee4,0x4e85d72c7ad96e41,
			0xb3a3842fd79e9b66,0xc1b355c2514cc12b,0x4d8d8e57e20a533f,0x9a230f94a80cc9cc,
			0x20287e80ba5f6a99,0xbf798e5356d5544d,0xa4b98b8f7cf5d947,0x5dfec4b0cf53d480,
			0xaff6108433392823,0xc77e7eafb9c35034,0x627f1e008407d3a4,0xd8187da069398c24,
			0x5b82e2951399fb6b,0x8f4165a5b13ef5e5,0xccc6836e6da90f20,0x5bc18466d41ea4b4,
			0xae57d5f0e7469301,0x382ec77f6dda7973,0x3334a04bfaf89130,0x560ae692d459495d,
			0xad396981b2cc54c6,0x721ee73a08477f9d,0xac3af4d5f2b948ae,0x8f027b0998907e6a,
			0xa2aa2576933135d2,0xf977e97a32d0ff40,0xc9ec4b2937331421,0x0a60651dd255075e,
			0xbc57a87285ad8ce8,0x05f745bb0f2f26c5,0xdbcb6ea37829349e,0xac85ec736c6c05f0,
			0xa0b8478607780956,0xe1a6cfc18a52c5cf,0xfdc0c9870db192cb,0x6fef6fa94de1275f,
			0xe7095cf3a87858df,0xa9382116dc12addf,0xfe43770e8ee1fdd0,0x12b5911c68f5a4fa,
			0xf674859107a9946e,0xbcbcec98535a2e90,0x487bbba9ec45c860,0xa6690ca5bfae55ef,
			0x2e90b70e4a6edd45,0xf75f315df85c92de,0x73c4b5d3f00c8ff6,0x16e7c2df5e0cc2fd,
			0x4d3450b5d1238d73,0x3be2360b8e8b5abf,0xaa9f15256af3545e,0x0b78b50380d558f5,
			0x35b1cd715c1a79c2,0xa5fd04e9b573386e,0xe8287684ad00498d,0x3af5a5175be12d85,
			0x00bad43e22f3efd0,0x2424d7c00ce3eea8,0x43be6edf2c578cf0,0x4640b84a827945fc,
			0x7e85782d5ed0fb6d,0xffde4449d800463d,0x5505de67825caf7c,0x958bad14a0d2bebd,
			0x19031376b81730d2,0xffe7c1cfd5aaf333,0x4a7cd21c4d61a00c,0xd955c74fee9622b4,
			0xdb600428f8ec65bd,0x412e30c19e4e9b47,0x1b39e37cd46c51fc,0x0b328354c1031b99,
			0x71eb9da5c27e6be7,0x56dd31a71467973d,0x9cefe510b69e8058,0x516e50ccb614f4a3,
			0x2feb109a1269f007,0x5bed5039f264362c,0x5a35a81fc188b664,0x86da46de6967b611,
			0x21cbe3aa2bf1e587,0x814748b95e35060d,0x4532a469e90aafc3,0xe7cdfd61261c5f5f,
			0x5f9ed3b7b2f0e4c7,0x8633484a1fe91578,0x07982616ddb26917,0x0a4a8fa267fd8e35,
			0x0169aa3ddb17bbe0,0x7ad23781004a8abb,0x8a99977154276184,0xf5aa49eb805db993,
			0xa91402c443f56747,0x3a158fd200401788,0x90d1286159a88e33,0x225ba3c00271a613,
			0xee87820cfe2bc5c1,0xf9cdfc0003d47859,0x58c3aeb0ed7bd81b,0x9dd2e17302417c1c,
			0x83236763812fd272,0x66337800026dd3d8,0x67926c64cdb2e951,0x28cd00001a9deeb6,
			0x7f5198092527e597,0x87de18001de39c2a,0x2389f07669962eee,0x4f2800002f2e26ac,
		};

		private void StateInitialize()
		{
			if (state == 0)
				state = 1;

			ulong state_0 = state;
			ulong random = state;

			//Goldreich-Goldwasser-Micali Construct PRF’s <How to construct random functions>
			//https://www.wisdom.weizmann.ac.il/~oded/X/ggm.pdf 
			for (ulong round = 0; round < 4; round++)
			{
				ulong next_random = 0;
				for (ulong bit_index = 0; bit_index < 64; bit_index++)
				{
					//Iterative use of PRG to generate random values
					//迭代使用PRG生成随机值
					random = this.StateIteration(random);

					if ((random & 1) == 1)
					{
						//If the generated random value is odd, set the current bit of the next random value to zero
						//如果生成的随机值是奇数,把下一次随机值当前比特设置为0
						//y=PRG[0](x)
						next_random |= 0;
						next_random <<= 1;
					}
					else
					{
						//Otherwise the random value generated is even, set the current bit of the next random value to 1
						//否则生成的随机值是偶数,把下一次随机值当前比特设置为1
						//y=PRG[1](x)
						next_random |= 1;
						next_random <<= 1;
					}
				}

				//Updates the next random value to the current random value.
				//把下一次的随机值更新为当前的随机值。
				random = next_random;
			}

			//Securely whitened uniformly randomized seeds.
			//安全白化的均匀随机种子。
			state ^= state_0 + random;
		}

		private ulong StateIteration(ulong numberOnce)
		{
			//使用本次轮常量
			ulong RC0 = ROUND_CONSTANT[numberOnce % (ulong)ROUND_CONSTANT.Length];
			ulong RC1 = ROUND_CONSTANT[(counter + numberOnce) % (ulong)ROUND_CONSTANT.Length];
			ulong RC2 = ROUND_CONSTANT[state % (ulong)ROUND_CONSTANT.Length];

			if (x == 0)
				x = RC0;
			else
			{
				/*
					扩散层：通过BitRotate和XOR操作实现各自状态之间的独立变换
					Diffusion layer: independent transformations between respective states via BitRotate and XOR operations
				*/

				y ^= BitsOperation.RotateLeft(x, 19) ^ BitsOperation.RotateLeft(x, 32);
				state ^= BitsOperation.RotateLeft(y, 32) ^ BitsOperation.RotateLeft(y, 47) ^ BitsOperation.RotateLeft(y, 63) ^ counter;
				x ^= BitsOperation.RotateLeft(state, 7) ^ BitsOperation.RotateLeft(state, 19) ^ RC0 ^ numberOnce;
			}

			/*
				混淆层：通过混合内部状态和非线性运算实现各自状态之间复杂关联变换
				Confusion layer: complex associative transformations between states by mixing internal states and nonlinear operations
			*/

			state += y ^ BitsOperation.RotateRight(y, 1) ^ RC0;
			x ^= state + BitsOperation.RotateRight(state, 1) + RC1;
			y += x ^ BitsOperation.RotateRight(x, 1) ^ RC2;

			counter++;
			return y;
		}

		public XorConstantRotation()
		{
			this.x = 0;
			this.y = 0;
			this.state = 1;
			this.counter = 0;

			this.StateInitialize();
		}

		public XorConstantRotation(ulong seed)
		{
			this.x = 0;
			this.y = 0;
			this.state = seed;
			this.counter = 0;

			this.StateInitialize();
		}

		public void Seed(ulong seed)
		{
			this.x = 0;
			this.y = 0;
			this.state = seed;
			this.counter = 0;

			this.StateInitialize();
		}

		public void ChangeCondition(ulong value)
		{
			this.x = value;
			this.y = 0;

			this.StateInitialize();
		}

		public ulong GenerateRandomNumber(ulong numberOnce)
		{
			return this.StateIteration(numberOnce);
		}
	}
}
