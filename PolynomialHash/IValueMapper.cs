using System.Globalization;
using System.Runtime.CompilerServices;

namespace PolynomialHash;

internal interface IValueMapper<in T>
{
	public ulong Map(T value);
}

internal readonly struct DelegateMapper<T>(Func<T, long> selector) : IValueMapper<T>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong Map(T value) => (ulong)selector(value);
}

internal readonly struct NumberMapper<T> : IValueMapper<T>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong Map(T value) => typeof(T) switch
{
    var t when t == typeof(long)   => (ulong)Unsafe.As<T, long>(ref value),
    var t when t == typeof(ulong)  => Unsafe.As<T, ulong>(ref value),
    var t when t == typeof(int)    => (ulong)Unsafe.As<T, int>(ref value),
    var t when t == typeof(uint)   => Unsafe.As<T, uint>(ref value),
    var t when t == typeof(short)  => (ulong)Unsafe.As<T, short>(ref value),
    var t when t == typeof(ushort) => Unsafe.As<T, ushort>(ref value),
    var t when t == typeof(byte)   => Unsafe.As<T, byte>(ref value),
    var t when t == typeof(sbyte)  => (ulong)Unsafe.As<T, sbyte>(ref value),
    var t when t == typeof(char)   => Unsafe.As<T, char>(ref value),
    _ => (ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture)
};
}
