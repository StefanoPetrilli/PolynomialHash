using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PolynomialHash;

public sealed class PolynomialHasher<T> : IEqualityComparer<IEnumerable<T>>
{
	private readonly Func<T, long>? _valueSelector;
	private readonly ulong _prime;
	private readonly ulong _mod;
	private readonly bool _isModPowerOfTwo;

	public PolynomialHasher(Func<T, long>? valueSelector = null,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
	{
		if (valueSelector is null && !IsNumericType())
		{
			throw new ArgumentNullException(nameof(valueSelector), $"A {nameof(valueSelector)} must be provided for non-numeric type {typeof(T).Name}.");
		}

		_valueSelector = valueSelector;
		_prime = prime;
		_mod = mod;
		_isModPowerOfTwo = _mod == 0 || (_mod & (_mod - 1)) == 0;
	}

	private static bool IsNumericType() => typeof(T) switch
	{
		var t when t == typeof(char) || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(System.Numerics.INumber<>)) => true,
		_ => false
	};

	public ulong ComputeHash(ReadOnlySpan<T> source)
	=> _valueSelector != null
			? ComputeHashInternal(source, new DelegateMapper<T>(_valueSelector))
			: ComputeHashInternal(source, new NumberMapper<T>());

	public ulong ComputeHash(IEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);

		return source switch
		{
			T[] array => ComputeHash(new ReadOnlySpan<T>(array)),
			List<T> list => ComputeHash(CollectionsMarshal.AsSpan(list)),
			_ => ComputeHashGeneric(source)
		};
	}

	private ulong ComputeHashGeneric(IEnumerable<T> source)
		=> _valueSelector != null
			? ComputeHashWithDelegateMapper(source, _valueSelector)
			: ComputeHashWithNumberMapper(source);

	private ulong ComputeHashWithDelegateMapper(IEnumerable<T> source, Func<T, long> selector)
		=> ComputeHashWithMapper(source, new DelegateMapper<T>(selector));

	private ulong ComputeHashWithNumberMapper(IEnumerable<T> source)
		=> ComputeHashWithMapper(source, new NumberMapper<T>());

	private ulong ComputeHashWithMapper<TMapper>(IEnumerable<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
		=> _isModPowerOfTwo
			? ComputeHashWithBitwiseMask(source, mapper)
			: ComputeHashWithModulo(source, mapper);

	public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y) => (x, y) switch
	{
		_ when ReferenceEquals(x, y) => true,
		_ when x is null || y is null => false,
		_ => x.SequenceEqual(y)
	};

	public int GetHashCode(IEnumerable<T> obj)
		=> unchecked((int)ComputeHash(obj));

	private ulong ComputeHashInternal<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
		=> _isModPowerOfTwo
			? SelectComputeHashWithBitwiseMask(source, mapper)
			: ComputeHashWithModulo(source, mapper);

	private ulong ComputeHashWithModulo<TMapper>(IEnumerable<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
	{
		ulong hashValue = 0, primePower = 1;

		foreach (T item in source)
		{
			ulong itemValue = mapper.Map(item) % _mod;

			hashValue = (hashValue + (itemValue * primePower)) % _mod;
			primePower = primePower * _prime % _mod;
		}

		return hashValue;
	}

	private ulong ComputeHashWithModulo<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
	{
		ulong hashValue = 0, primePower = 1;

		foreach (T item in source)
		{
			ulong itemValue = mapper.Map(item) % _mod;

			hashValue = (hashValue + (itemValue * primePower)) % _mod;
			primePower = primePower * _prime % _mod;
		}

		return hashValue;
	}

	private ulong ComputeHashWithBitwiseMask<TMapper>(IEnumerable<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
	{
		ulong hashValue = 0, primePower = 1;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		foreach (T item in source)
		{
			ulong itemValue = mapper.Map(item);

			unchecked
			{
				hashValue += itemValue * primePower;
				primePower *= _prime;
			}
		}

		return hashValue & mask;
	}

	private ulong SelectComputeHashWithBitwiseMask<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
    where TMapper : struct, IValueMapper<T>
    => (Vector512.IsHardwareAccelerated, Avx512F.IsSupported) switch
    {
        (true, true) => ComputeHashWithBitwiseMaskAVX512(source, mapper),
        _ when Vector256.IsHardwareAccelerated && Avx2.IsSupported => ComputeHashWithBitwiseMaskAVX2(source, mapper),
        _ => ComputeHashWithBitwiseMask(source, mapper)
    };

	private ulong ComputeHashWithBitwiseMaskAVX512<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
		=> typeof(TMapper) == typeof(NumberMapper<T>)
			? ComputeHashWithBitwiseMaskAVX512_NumberMapper(source)
			: ComputeHashWithBitwiseMaskAVX512_Generic(source, mapper);

	private ulong ComputeHashWithBitwiseMaskAVX512_NumberMapper(ReadOnlySpan<T> source)
		=> typeof(T) switch
		{
			_ when typeof(T) == typeof(long) || typeof(T) == typeof(ulong) => ComputeHashWithBitwiseMaskAVX512_Int64(source),
			_ when typeof(T) == typeof(int) || typeof(T) == typeof(uint) => ComputeHashWithBitwiseMaskAVX512_Int32(source),
			_ => ComputeHashWithBitwiseMaskAVX512_Generic(source, new NumberMapper<T>())
		};

	private ulong ComputeHashWithBitwiseMaskAVX512_Int64(ReadOnlySpan<T> source)
	{
		int length = source.Length;
		int remainder = length % 8;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		var (powersVec, jumpVec) = GetAVX512Constants();
		Vector512<ulong> accVec = Vector512<ulong>.Zero;

		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		for (int i = 0; i < loopLimit; i += 8)
		{
			var dataVec = Vector512.LoadUnsafe(ref Unsafe.As<T, ulong>(ref Unsafe.Add(ref sourceRef, i)));
			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		return FinalizeAVX512Hash(accVec, powersVec, source, loopLimit, remainder, new NumberMapper<T>(), mask);
	}

	private ulong ComputeHashWithBitwiseMaskAVX512_Int32(ReadOnlySpan<T> source)
	{
		int length = source.Length;
		int remainder = length % 8;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		var (powersVec, jumpVec) = GetAVX512Constants();
		Vector512<ulong> accVec = Vector512<ulong>.Zero;

		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		for (int i = 0; i < loopLimit; i += 8)
		{
			var intVec = Vector256.LoadUnsafe(ref Unsafe.As<T, uint>(ref Unsafe.Add(ref sourceRef, i)));
			var dataVec = Vector512.Create(
				Vector256.WidenLower(intVec).AsUInt64(),
				Vector256.WidenUpper(intVec).AsUInt64());

			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		return FinalizeAVX512Hash(accVec, powersVec, source, loopLimit, remainder, new NumberMapper<T>(), mask);
	}

	private ulong ComputeHashWithBitwiseMaskAVX512_Generic<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
	{
		int length = source.Length;
		int remainder = length % 8;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		var (powersVec, jumpVec) = GetAVX512Constants();
		Vector512<ulong> accVec = Vector512<ulong>.Zero;

		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		for (int i = 0; i < loopLimit; i += 8)
		{
			var dataVec = Vector512.Create(
				mapper.Map(Unsafe.Add(ref sourceRef, i)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 1)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 2)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 3)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 4)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 5)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 6)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 7)));

			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		return FinalizeAVX512Hash(accVec, powersVec, source, loopLimit, remainder, mapper, mask);
	}

	private (Vector512<ulong> powersVec, Vector512<ulong> jumpVec) GetAVX512Constants()
	{
		ulong p1 = _prime;
		ulong p2 = unchecked(p1 * p1);
		ulong p3 = unchecked(p2 * p1);
		ulong p4 = unchecked(p2 * p2);
		ulong p8 = unchecked(p4 * p4);

		var lower = Vector256.Create(1ul, p1, p2, p3);
		var upper = Vector256.Multiply(lower, Vector256.Create(p4));

		return (Vector512.Create(lower, upper), Vector512.Create(p8));
	}

	private ulong FinalizeAVX512Hash<TMapper>(Vector512<ulong> accVec, Vector512<ulong> powersVec, ReadOnlySpan<T> source, int loopLimit, int remainder, TMapper mapper, ulong mask)
		where TMapper : struct, IValueMapper<T>
	{
		if (remainder == 0)
		{
			return Vector512.Sum(accVec) & mask;
		}

		ref T sourceRef = ref MemoryMarshal.GetReference(source);
		Span<ulong> tailData = stackalloc ulong[8];
		tailData.Clear();
		for (int j = 0; j < remainder; j++)
		{
			tailData[j] = mapper.Map(Unsafe.Add(ref sourceRef, loopLimit + j));
		}

		var tailDataVec = Vector512.Create(tailData);
		unchecked { accVec += tailDataVec * powersVec; }

		return Vector512.Sum(accVec) & mask;
	}

	private ulong ComputeHashWithBitwiseMaskAVX2<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
		=> typeof(TMapper) == typeof(NumberMapper<T>)
			? ComputeHashWithBitwiseMaskAVX2_NumberMapper(source)
			: ComputeHashWithBitwiseMaskAVX2_Generic(source, mapper);

	private ulong ComputeHashWithBitwiseMaskAVX2_NumberMapper(ReadOnlySpan<T> source)
		=> typeof(T) switch
		{
			_ when typeof(T) == typeof(long) || typeof(T) == typeof(ulong) => ComputeHashWithBitwiseMaskAVX2_Int64(source),
			_ when typeof(T) == typeof(int) || typeof(T) == typeof(uint) => ComputeHashWithBitwiseMaskAVX2_Int32(source),
			_ => ComputeHashWithBitwiseMaskAVX2_Generic(source, new NumberMapper<T>())
		};

	private ulong ComputeHashWithBitwiseMaskAVX2_Int64(ReadOnlySpan<T> source)
	{
		int length = source.Length;
		int remainder = length % 4;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		var (powersVec, jumpVec) = GetAVX2Constants();
		Vector256<ulong> accVec = Vector256<ulong>.Zero;

		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		for (int i = 0; i < loopLimit; i += 4)
		{
			var dataVec = Vector256.LoadUnsafe(ref Unsafe.As<T, ulong>(ref Unsafe.Add(ref sourceRef, i)));
			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		return FinalizeAVX2Hash(accVec, powersVec, source, loopLimit, remainder, new NumberMapper<T>(), mask);
	}

	private ulong ComputeHashWithBitwiseMaskAVX2_Int32(ReadOnlySpan<T> source)
	{
		int length = source.Length;
		int remainder = length % 4;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		var (powersVec, jumpVec) = GetAVX2Constants();
		Vector256<ulong> accVec = Vector256<ulong>.Zero;

		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		for (int i = 0; i < loopLimit; i += 4)
		{
			var intVec = Vector128.LoadUnsafe(ref Unsafe.As<T, uint>(ref Unsafe.Add(ref sourceRef, i)));
			var dataVec = Vector256.Create(
				Vector128.WidenLower(intVec).AsUInt64(),
				Vector128.WidenUpper(intVec).AsUInt64());

			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		return FinalizeAVX2Hash(accVec, powersVec, source, loopLimit, remainder, new NumberMapper<T>(), mask);
	}

	private ulong ComputeHashWithBitwiseMaskAVX2_Generic<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
	{
		int length = source.Length;
		int remainder = length % 4;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		var (powersVec, jumpVec) = GetAVX2Constants();
		Vector256<ulong> accVec = Vector256<ulong>.Zero;

		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		for (int i = 0; i < loopLimit; i += 4)
		{
			var dataVec = Vector256.Create(
				mapper.Map(Unsafe.Add(ref sourceRef, i)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 1)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 2)),
				mapper.Map(Unsafe.Add(ref sourceRef, i + 3)));

			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		return FinalizeAVX2Hash(accVec, powersVec, source, loopLimit, remainder, mapper, mask);
	}

	private (Vector256<ulong> powersVec, Vector256<ulong> jumpVec) GetAVX2Constants()
	{
		Span<ulong> powers = stackalloc ulong[4];
		ulong currentPower = 1;
		for (int j = 0; j < 4; j++)
		{
			powers[j] = currentPower;
			unchecked { currentPower *= _prime; }
		}

		ulong p4 = 1;
		for (int j = 0; j < 4; j++)
		{
			unchecked { p4 *= _prime; }
		}

		return (Vector256.Create(powers), Vector256.Create(p4));
	}

	private ulong FinalizeAVX2Hash<TMapper>(Vector256<ulong> accVec, Vector256<ulong> powersVec, ReadOnlySpan<T> source, int loopLimit, int remainder, TMapper mapper, ulong mask)
		where TMapper : struct, IValueMapper<T>
	{
		ulong hashValue = Vector256.Sum(accVec);
		ulong primePower = powersVec.GetElement(0);

		ref T sourceRef = ref MemoryMarshal.GetReference(source);
		int length = source.Length;

		for (int i = loopLimit; i < length; i++)
		{
			ulong itemValue = mapper.Map(Unsafe.Add(ref sourceRef, i));
			unchecked
			{
				hashValue += itemValue * primePower;
				primePower *= _prime;
			}
		}

		return hashValue & mask;
	}
	private ulong ComputeHashWithBitwiseMask<TMapper>(ReadOnlySpan<T> source, TMapper mapper)
		where TMapper : struct, IValueMapper<T>
	{
		ulong hashValue = 0, primePower = 1;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		foreach (T item in source)
		{
			ulong itemValue = mapper.Map(item);

			unchecked
			{
				hashValue += itemValue * primePower;
				primePower *= _prime;
			}
		}

		return hashValue & mask;
	}
}
