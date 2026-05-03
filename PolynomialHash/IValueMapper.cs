using System.Globalization;
using System.Runtime.CompilerServices;

namespace PolynomialHash;

internal interface IValueMapper<in T>
{
	ulong Map(T value);
}

internal readonly struct DelegateMapper<T>(Func<T, long> selector) : IValueMapper<T>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong Map(T value) => (ulong)selector(value);
}

internal readonly struct NumberMapper<T> : IValueMapper<T>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong Map(T value)
	{
		if (typeof(T) == typeof(long)) return (ulong)(long)(object)value!;
		if (typeof(T) == typeof(int)) return (ulong)(int)(object)value!;
		if (typeof(T) == typeof(ulong)) return (ulong)(object)value!;
		if (typeof(T) == typeof(uint)) return (ulong)(uint)(object)value!;
		if (typeof(T) == typeof(short)) return (ulong)(short)(object)value!;
		if (typeof(T) == typeof(ushort)) return (ulong)(ushort)(object)value!;
		if (typeof(T) == typeof(byte)) return (ulong)(byte)(object)value!;
		if (typeof(T) == typeof(sbyte)) return (ulong)(sbyte)(object)value!;
		if (typeof(T) == typeof(char)) return (ulong)(char)(object)value!;

		return (ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture);
	}
}
