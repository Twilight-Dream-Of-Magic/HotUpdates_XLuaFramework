using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace TwilightDreamOfMagical.CustomSecurity.UnsafeTool
{
	static class MaskGen
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort CreateMask16()
		{
			Span<byte> b = stackalloc byte[2];
			RandomNumberGenerator.Fill(b);
			return BinaryPrimitives.ReadUInt16LittleEndian(b);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint CreateMask32()
		{
			Span<byte> b = stackalloc byte[4];
			RandomNumberGenerator.Fill(b);
			return BinaryPrimitives.ReadUInt32LittleEndian(b);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong CreateMask64()
		{
			Span<byte> b = stackalloc byte[8];
			RandomNumberGenerator.Fill(b);
			return BinaryPrimitives.ReadUInt64LittleEndian(b);
		}
	}

	// ====== 各基础类型特化 ======
	// 支持 short, ushort, int, uint, long, ulong, float, double

	// 结构体模板：可复制到各类型
	// 包含：算术、比较、位运算（整数）、隐式转换

	#region CryptedShort
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedShort : IEquatable<CryptedShort>
	{
		private readonly ushort mask;
		private ushort encrypted;
		public CryptedShort(short v)
		{
			mask = MaskGen.CreateMask16();
			encrypted = (ushort)((ushort)v ^ mask);
		}
		public short Value
		{
			get => (short)(encrypted ^ mask);
			set => encrypted = (ushort)((ushort)value ^ mask);
		}
		public static implicit operator short(CryptedShort d) => d.Value;
		public static implicit operator CryptedShort(short v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator +(CryptedShort a, CryptedShort b) => new((short)(a.Value + b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator -(CryptedShort a, CryptedShort b) => new((short)(a.Value - b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator *(CryptedShort a, CryptedShort b) => new((short)(a.Value * b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator /(CryptedShort a, CryptedShort b) => new((short)(a.Value / b.Value));
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedShort a, CryptedShort b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedShort a, CryptedShort b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedShort a, CryptedShort b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedShort a, CryptedShort b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedShort a, CryptedShort b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedShort a, CryptedShort b) => a.Value >= b.Value;
		// 位运算
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator &(CryptedShort a, CryptedShort b) => new((short)(a.Value & b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator |(CryptedShort a, CryptedShort b) => new((short)(a.Value | b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator ^(CryptedShort a, CryptedShort b) => new((short)(a.Value ^ b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator ~(CryptedShort a) => new((short)~a.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator <<(CryptedShort a, int shift) => new((short)(a.Value << shift));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort operator >>(CryptedShort a, int shift) => new((short)(a.Value >> shift));
		// Equals & GetHashCode
		public bool Equals(CryptedShort other) => this == other;
		public override bool Equals(object obj) => obj is CryptedShort cs && Equals(cs);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedUShort
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedUShort : IEquatable<CryptedUShort>
	{
		private readonly ushort mask;
		private ushort encrypted;
		public CryptedUShort(ushort v)
		{
			mask = MaskGen.CreateMask16();
			encrypted = (ushort)(v ^ mask);
		}
		public ushort Value
		{
			get => (ushort)(encrypted ^ mask);
			set => encrypted = (ushort)(value ^ mask);
		}

		public static implicit operator ushort(CryptedUShort d) => d.Value;
		public static implicit operator CryptedUShort(ushort v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator +(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value + b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator -(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value - b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator *(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value * b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator /(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value / b.Value));
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedUShort a, CryptedUShort b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedUShort a, CryptedUShort b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedUShort a, CryptedUShort b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedUShort a, CryptedUShort b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedUShort a, CryptedUShort b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedUShort a, CryptedUShort b) => a.Value >= b.Value;
		// 位运算
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator &(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value & b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator |(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value | b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator ^(CryptedUShort a, CryptedUShort b) => new((ushort)(a.Value ^ b.Value));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator ~(CryptedUShort a) => new((ushort)~a.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator <<(CryptedUShort a, int shift) => new((ushort)(a.Value << shift));
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort operator >>(CryptedUShort a, int shift) => new((ushort)(a.Value >> shift));
		// Equals & GetHashCode
		public bool Equals(CryptedUShort other) => this == other;
		public override bool Equals(object obj) => obj is CryptedUShort us && Equals(us);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedInt
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedInt : IEquatable<CryptedInt>
	{
		private readonly uint mask;
		private uint encrypted;
		public CryptedInt(int v)
		{
			mask = MaskGen.CreateMask32();
			encrypted = ((uint)v) ^ mask;
		}
		public int Value
		{
			get => (int)(encrypted ^ mask);
			set => encrypted = ((uint)value) ^ mask;
		}

		public static implicit operator int(CryptedInt d) => d.Value;
		public static implicit operator CryptedInt(int v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator +(CryptedInt a, CryptedInt b) => new(a.Value + b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator -(CryptedInt a, CryptedInt b) => new(a.Value - b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator *(CryptedInt a, CryptedInt b) => new(a.Value * b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator /(CryptedInt a, CryptedInt b) => new(a.Value / b.Value);
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedInt a, CryptedInt b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedInt a, CryptedInt b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedInt a, CryptedInt b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedInt a, CryptedInt b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedInt a, CryptedInt b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedInt a, CryptedInt b) => a.Value >= b.Value;
		// 位运算
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator &(CryptedInt a, CryptedInt b) => new(a.Value & b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator |(CryptedInt a, CryptedInt b) => new(a.Value | b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator ^(CryptedInt a, CryptedInt b) => new(a.Value ^ b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator ~(CryptedInt a) => new(~a.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator <<(CryptedInt a, int shift) => new(a.Value << shift);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt operator >>(CryptedInt a, int shift) => new(a.Value >> shift);
		// Equals & GetHashCode
		public bool Equals(CryptedInt other) => this == other;
		public override bool Equals(object obj) => obj is CryptedInt i && Equals(i);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedUInt
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedUInt : IEquatable<CryptedUInt>
	{
		private readonly uint mask;
		private uint encrypted;
		public CryptedUInt(uint v)
		{
			mask = MaskGen.CreateMask32();
			encrypted = v ^ mask;
		}
		public uint Value
		{
			get => encrypted ^ mask;
			set => encrypted = value ^ mask;
		}

		public static implicit operator uint(CryptedUInt d) => d.Value;
		public static implicit operator CryptedUInt(uint v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator +(CryptedUInt a, CryptedUInt b) => new(a.Value + b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator -(CryptedUInt a, CryptedUInt b) => new(a.Value - b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator *(CryptedUInt a, CryptedUInt b) => new(a.Value * b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator /(CryptedUInt a, CryptedUInt b) => new(a.Value / b.Value);
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedUInt a, CryptedUInt b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedUInt a, CryptedUInt b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedUInt a, CryptedUInt b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedUInt a, CryptedUInt b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedUInt a, CryptedUInt b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedUInt a, CryptedUInt b) => a.Value >= b.Value;
		// 位运算
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator &(CryptedUInt a, CryptedUInt b) => new(a.Value & b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator |(CryptedUInt a, CryptedUInt b) => new(a.Value | b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator ^(CryptedUInt a, CryptedUInt b) => new(a.Value ^ b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator ~(CryptedUInt a) => new(~a.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator <<(CryptedUInt a, int shift) => new(a.Value << shift);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt operator >>(CryptedUInt a, int shift) => new(a.Value >> shift);
		// Equals & GetHashCode
		public bool Equals(CryptedUInt other) => this == other;
		public override bool Equals(object obj) => obj is CryptedUInt ui && Equals(ui);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedLong
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedLong : IEquatable<CryptedLong>
	{
		private readonly ulong mask;
		private ulong encrypted;
		public CryptedLong(long v)
		{
			mask = MaskGen.CreateMask64();
			encrypted = ((ulong)v) ^ mask;
		}
		public long Value
		{
			get => (long)(encrypted ^ mask);
			set => encrypted = ((ulong)value) ^ mask;
		}

		public static implicit operator long(CryptedLong d) => d.Value;
		public static implicit operator CryptedLong(long v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator +(CryptedLong a, CryptedLong b) => new(a.Value + b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator -(CryptedLong a, CryptedLong b) => new(a.Value - b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator *(CryptedLong a, CryptedLong b) => new(a.Value * b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator /(CryptedLong a, CryptedLong b) => new(a.Value / b.Value);
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedLong a, CryptedLong b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedLong a, CryptedLong b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedLong a, CryptedLong b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedLong a, CryptedLong b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedLong a, CryptedLong b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedLong a, CryptedLong b) => a.Value >= b.Value;
		// 位运算
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator &(CryptedLong a, CryptedLong b) => new(a.Value & b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator |(CryptedLong a, CryptedLong b) => new(a.Value | b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator ^(CryptedLong a, CryptedLong b) => new(a.Value ^ b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator ~(CryptedLong a) => new(~a.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator <<(CryptedLong a, int shift) => new(a.Value << shift);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong operator >>(CryptedLong a, int shift) => new(a.Value >> shift);
		// Equals & GetHashCode
		public bool Equals(CryptedLong other) => this == other;
		public override bool Equals(object obj) => obj is CryptedLong l && Equals(l);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedULong
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedULong : IEquatable<CryptedULong>
	{
		private readonly ulong mask;
		private ulong encrypted;
		public CryptedULong(ulong v)
		{
			mask = MaskGen.CreateMask64();
			encrypted = v ^ mask;
		}
		public ulong Value
		{
			get => encrypted ^ mask;
			set => encrypted = value ^ mask;
		}

		public static implicit operator ulong(CryptedULong d) => d.Value;
		public static implicit operator CryptedULong(ulong v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator +(CryptedULong a, CryptedULong b) => new(a.Value + b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator -(CryptedULong a, CryptedULong b) => new(a.Value - b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator *(CryptedULong a, CryptedULong b) => new(a.Value * b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator /(CryptedULong a, CryptedULong b) => new(a.Value / b.Value);
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedULong a, CryptedULong b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedULong a, CryptedULong b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedULong a, CryptedULong b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedULong a, CryptedULong b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedULong a, CryptedULong b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedULong a, CryptedULong b) => a.Value >= b.Value;
		// 位运算
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator &(CryptedULong a, CryptedULong b) => new(a.Value & b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator |(CryptedULong a, CryptedULong b) => new(a.Value | b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator ^(CryptedULong a, CryptedULong b) => new(a.Value ^ b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator ~(CryptedULong a) => new(~a.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator <<(CryptedULong a, int shift) => new(a.Value << shift);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong operator >>(CryptedULong a, int shift) => new(a.Value >> shift);
		// Equals & GetHashCode
		public bool Equals(CryptedULong other) => this == other;
		public override bool Equals(object obj) => obj is CryptedULong ul && Equals(ul);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedFloat
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedFloat : IEquatable<CryptedFloat>
	{
		private readonly uint mask;
		private uint encrypted;
		public CryptedFloat(float v)
		{
			mask = MaskGen.CreateMask32();
			encrypted = (uint)BitConverter.SingleToInt32Bits(v) ^ mask;
		}
		public float Value
		{
			get => BitConverter.Int32BitsToSingle((int)(encrypted ^ mask));
			set
			{
				encrypted = (uint)BitConverter.SingleToInt32Bits(value) ^ mask;
			}
		}

		public static implicit operator float(CryptedFloat d) => d.Value;
		public static implicit operator CryptedFloat(float v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat operator +(CryptedFloat a, CryptedFloat b) => new(a.Value + b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat operator -(CryptedFloat a, CryptedFloat b) => new(a.Value - b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat operator *(CryptedFloat a, CryptedFloat b) => new(a.Value * b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat operator /(CryptedFloat a, CryptedFloat b) => new(a.Value / b.Value);
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedFloat a, CryptedFloat b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedFloat a, CryptedFloat b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedFloat a, CryptedFloat b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedFloat a, CryptedFloat b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedFloat a, CryptedFloat b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedFloat a, CryptedFloat b) => a.Value >= b.Value;
		// Equals & GetHashCode
		public bool Equals(CryptedFloat other) => this == other;
		public override bool Equals(object obj) => obj is CryptedFloat f && Equals(f);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	#region CryptedDouble
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CryptedDouble : IEquatable<CryptedDouble>
	{
		private readonly ulong mask;
		private ulong encrypted;
		public CryptedDouble(double v)
		{
			mask = MaskGen.CreateMask64();
			encrypted = (ulong)BitConverter.DoubleToInt64Bits(v) ^ mask;
		}
		public double Value
		{
			get => BitConverter.Int64BitsToDouble((long)(encrypted ^ mask));
			set
			{
				encrypted = (ulong)BitConverter.DoubleToInt64Bits(value) ^ mask;
			}
		}

		public static implicit operator double(CryptedDouble d) => d.Value;
		public static implicit operator CryptedDouble(double v) => new(v);
		// 算术
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble operator +(CryptedDouble a, CryptedDouble b) => new(a.Value + b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble operator -(CryptedDouble a, CryptedDouble b) => new(a.Value - b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble operator *(CryptedDouble a, CryptedDouble b) => new(a.Value * b.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble operator /(CryptedDouble a, CryptedDouble b) => new(a.Value / b.Value);
		// 比较
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(CryptedDouble a, CryptedDouble b) => a.Value == b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(CryptedDouble a, CryptedDouble b) => a.Value != b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <(CryptedDouble a, CryptedDouble b) => a.Value < b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >(CryptedDouble a, CryptedDouble b) => a.Value > b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator <=(CryptedDouble a, CryptedDouble b) => a.Value <= b.Value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator >=(CryptedDouble a, CryptedDouble b) => a.Value >= b.Value;
		// Equals & GetHashCode
		public bool Equals(CryptedDouble other) => this == other;
		public override bool Equals(object obj) => obj is CryptedDouble d && Equals(d);
		public override int GetHashCode() => Value.GetHashCode();
	}
	#endregion

	// ====== 转换扩展方法 ======
	public static class CryptedConversions
	{
		// 从 CryptedShort
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedShort s) => new((ushort)s.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedShort s) => new(s.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedShort s) => new((uint)s.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedShort s) => new(s.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedShort s) => new((ulong)s.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedShort s) => new(s.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedShort s) => new(s.Value);
		// 从 CryptedUShort
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedUShort u) => new((short)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedUShort u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedUShort u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedUShort u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedUShort u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedUShort u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedUShort u) => new(u.Value);
		// 从 CryptedInt
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedInt i) => new((short)i.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedInt i) => new((ushort)i.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedInt i) => new((uint)i.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedInt i) => new(i.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedInt i) => new((ulong)i.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedInt i) => new(i.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedInt i) => new(i.Value);
		// 从 CryptedUInt
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedUInt u) => new((short)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedUInt u) => new((ushort)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedUInt u) => new((int)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedUInt u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedUInt u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedUInt u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedUInt u) => new(u.Value);
		// 从 CryptedLong
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedLong l) => new((short)l.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedLong l) => new((ushort)l.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedLong l) => new((int)l.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedLong l) => new((uint)l.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedLong l) => new((ulong)l.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedLong l) => new(l.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedLong l) => new(l.Value);
		// 从 CryptedULong
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedULong u) => new((short)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedULong u) => new((ushort)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedULong u) => new((int)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedULong u) => new((uint)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedULong u) => new((long)u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedULong u) => new(u.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedULong u) => new(u.Value);
		// 从 CryptedFloat
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedFloat f) => new((short)f.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedFloat f) => new((ushort)f.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedFloat f) => new((int)f.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedFloat f) => new((uint)f.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedFloat f) => new((long)f.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedFloat f) => new((ulong)f.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedDouble ToCryptedDouble(this CryptedFloat f) => new(f.Value);
		// 从 CryptedDouble
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedShort ToCryptedShort(this CryptedDouble d) => new((short)d.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUShort ToCryptedUShort(this CryptedDouble d) => new((ushort)d.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedInt ToCryptedInt(this CryptedDouble d) => new((int)d.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedUInt ToCryptedUInt(this CryptedDouble d) => new((uint)d.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedLong ToCryptedLong(this CryptedDouble d) => new((long)d.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedULong ToCryptedULong(this CryptedDouble d) => new((ulong)d.Value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static CryptedFloat ToCryptedFloat(this CryptedDouble d) => new((float)d.Value);
	}

	// CryptedShort 复合赋值扩展
	public static class CryptedShortExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedShort x, CryptedShort y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedShort x, CryptedShort y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedShort x, CryptedShort y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedShort x, CryptedShort y) { x.Value /= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AndAssign(this ref CryptedShort x, CryptedShort y) { x.Value &= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void OrAssign(this ref CryptedShort x, CryptedShort y) { x.Value |= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void XorAssign(this ref CryptedShort x, CryptedShort y) { x.Value ^= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftLeftAssign(this ref CryptedShort x, int s) { x.Value <<= s; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftRightAssign(this ref CryptedShort x, int s) { x.Value >>= s; }
	}

	// CryptedUShort 复合赋值扩展
	public static class CryptedUShortExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value /= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AndAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value &= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void OrAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value |= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void XorAssign(this ref CryptedUShort x, CryptedUShort y) { x.Value ^= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftLeftAssign(this ref CryptedUShort x, int s) { x.Value <<= s; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftRightAssign(this ref CryptedUShort x, int s) { x.Value >>= s; }
	}

	// CryptedInt 复合赋值扩展
	public static class CryptedIntExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedInt x, CryptedInt y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedInt x, CryptedInt y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedInt x, CryptedInt y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedInt x, CryptedInt y) { x.Value /= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AndAssign(this ref CryptedInt x, CryptedInt y) { x.Value &= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void OrAssign(this ref CryptedInt x, CryptedInt y) { x.Value |= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void XorAssign(this ref CryptedInt x, CryptedInt y) { x.Value ^= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftLeftAssign(this ref CryptedInt x, int s) { x.Value <<= s; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftRightAssign(this ref CryptedInt x, int s) { x.Value >>= s; }
	}

	// CryptedUInt 复合赋值扩展
	public static class CryptedUIntExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value /= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AndAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value &= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void OrAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value |= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void XorAssign(this ref CryptedUInt x, CryptedUInt y) { x.Value ^= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftLeftAssign(this ref CryptedUInt x, int s) { x.Value <<= s; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftRightAssign(this ref CryptedUInt x, int s) { x.Value >>= s; }
	}

	// CryptedLong 复合赋值扩展
	public static class CryptedLongExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedLong x, CryptedLong y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedLong x, CryptedLong y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedLong x, CryptedLong y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedLong x, CryptedLong y) { x.Value /= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AndAssign(this ref CryptedLong x, CryptedLong y) { x.Value &= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void OrAssign(this ref CryptedLong x, CryptedLong y) { x.Value |= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void XorAssign(this ref CryptedLong x, CryptedLong y) { x.Value ^= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftLeftAssign(this ref CryptedLong x, int s) { x.Value <<= s; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftRightAssign(this ref CryptedLong x, int s) { x.Value >>= s; }
	}

	// CryptedULong 复合赋值扩展
	public static class CryptedULongExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedULong x, CryptedULong y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedULong x, CryptedULong y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedULong x, CryptedULong y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedULong x, CryptedULong y) { x.Value /= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AndAssign(this ref CryptedULong x, CryptedULong y) { x.Value &= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void OrAssign(this ref CryptedULong x, CryptedULong y) { x.Value |= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void XorAssign(this ref CryptedULong x, CryptedULong y) { x.Value ^= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftLeftAssign(this ref CryptedULong x, int s) { x.Value <<= s; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShiftRightAssign(this ref CryptedULong x, int s) { x.Value >>= s; }
	}

	// CryptedFloat 复合赋值扩展
	public static class CryptedFloatExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedFloat x, CryptedFloat y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedFloat x, CryptedFloat y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedFloat x, CryptedFloat y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedFloat x, CryptedFloat y) { x.Value /= y.Value; }
	}

	// CryptedDouble 复合赋值扩展
	public static class CryptedDoubleExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void AddAssign(this ref CryptedDouble x, CryptedDouble y) { x.Value += y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SubtractAssign(this ref CryptedDouble x, CryptedDouble y) { x.Value -= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void MultiplyAssign(this ref CryptedDouble x, CryptedDouble y) { x.Value *= y.Value; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void DivideAssign(this ref CryptedDouble x, CryptedDouble y) { x.Value /= y.Value; }
	}
}
