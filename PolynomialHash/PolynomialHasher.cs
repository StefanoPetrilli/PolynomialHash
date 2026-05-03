using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PolynomialHash;

public sealed class PolynomialHasher<T> : IEqualityComparer<IEnumerable<T>>
{
	private readonly Func<T, long> _valueSelector;
	private readonly ulong _prime;
	private readonly ulong _mod;
	private readonly bool _isModPowerOfTwo;

	public PolynomialHasher(Func<T, long> valueSelector,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
	{
		ArgumentNullException.ThrowIfNull(valueSelector);
		_valueSelector = valueSelector;
		_prime = prime;
		_mod = mod;
		_isModPowerOfTwo = _mod == 0 || (_mod & (_mod - 1)) == 0;
	}

	public ulong ComputeHash(ReadOnlySpan<T> source)
		=> _isModPowerOfTwo ? SelectComputeHashWithBitwiseMask(source) : ComputeHashWithModulo(source);

	public ulong ComputeHash(IEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);

		return source switch
		{
			T[] array => ComputeHash(new ReadOnlySpan<T>(array)),
			List<T> list => ComputeHash(CollectionsMarshal.AsSpan(list)),
			_ => _isModPowerOfTwo ? ComputeHashWithBitwiseMask(source) : ComputeHashWithModulo(source)
		};
	}

	public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y) => (x, y) switch
	{
		_ when ReferenceEquals(x, y) => true,
		_ when x is null || y is null => false,
		_ => x.SequenceEqual(y)
	};

	public int GetHashCode(IEnumerable<T> obj)
	=> unchecked((int)ComputeHash(obj));

	private ulong ComputeHashWithModulo(IEnumerable<T> source)
	{
		ulong hashValue = 0, primePower = 1, itemValue;

		foreach (T item in source)
		{
			itemValue = (ulong)_valueSelector(item) % _mod;

			hashValue = (hashValue + (itemValue * primePower)) % _mod;
			primePower = primePower * _prime % _mod;
		}

		return hashValue;
	}

	private ulong ComputeHashWithModulo(ReadOnlySpan<T> source)
	{
		ulong hashValue = 0, primePower = 1;

		foreach (T item in source)
		{
			ulong itemValue = (ulong)_valueSelector(item) % _mod;

			hashValue = (hashValue + (itemValue * primePower)) % _mod;
			primePower = primePower * _prime % _mod;
		}

		return hashValue;
	}

	private ulong ComputeHashWithBitwiseMask(IEnumerable<T> source)
	{
		ulong hashValue = 0, primePower = 1;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		foreach (T item in source)
		{
			ulong itemValue = (ulong)_valueSelector(item);

			unchecked
			{
				hashValue += itemValue * primePower;
				primePower *= _prime;
			}
		}

		return hashValue & mask;
	}

	private ulong SelectComputeHashWithBitwiseMask(ReadOnlySpan<T> source) => true switch
	{
		_ when Vector512.IsHardwareAccelerated && Avx512F.IsSupported => ComputeHashWithBitwiseMaskAVX512(source),
		_ when Vector256.IsHardwareAccelerated && Avx2.IsSupported => ComputeHashWithBitwiseMaskAVX2(source),
		_ => ComputeHashWithBitwiseMask(source)
	};

	private ulong ComputeHashWithBitwiseMaskAVX512(ReadOnlySpan<T> source)
	{
		int length = source.Length;
		int remainder = length % 8;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		ulong p1 = _prime;
		ulong p2 = unchecked(p1 * p1);
		ulong p3 = unchecked(p2 * p1);
		ulong p4 = unchecked(p2 * p2);
		ulong p8 = unchecked(p4 * p4);

		var lower = Vector256.Create(1ul, p1, p2, p3);
		var upper = Vector256.Multiply(lower, Vector256.Create(p4));

		var powersVec = Vector512.Create(lower, upper);
		var jumpVec = Vector512.Create(p8);

		Vector512<ulong> accVec = Vector512<ulong>.Zero;

		Func<T, long> selector = _valueSelector;
		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		Span<ulong> buffer = stackalloc ulong[8];
		for (int i = 0; i < loopLimit; i += 8)
		{
			buffer[0] = (ulong)selector(Unsafe.Add(ref sourceRef, i));
			buffer[1] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 1));
			buffer[2] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 2));
			buffer[3] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 3));
			buffer[4] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 4));
			buffer[5] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 5));
			buffer[6] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 6));
			buffer[7] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 7));

			var dataVec = Vector512.Create(buffer);

			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		if (remainder == 0)
		{
			return Vector512.Sum(accVec) & mask;
		}

		Span<ulong> tailData = stackalloc ulong[8];
		tailData.Clear();
		for (int j = 0; j < remainder; j++)
		{
			tailData[j] = (ulong)selector(Unsafe.Add(ref sourceRef, loopLimit + j));
		}
		var tailDataVec = Vector512.Create(tailData);
		unchecked { accVec += tailDataVec * powersVec; }

		return Vector512.Sum(accVec) & mask;
	}

	private ulong ComputeHashWithBitwiseMaskAVX2(ReadOnlySpan<T> source)
	{
		int length = source.Length;
		int remainder = length % 4;
		int loopLimit = length - remainder;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		// Precomputing the Powers Vector [p^0, p^1, p^2, p^3]
		Span<ulong> powers = stackalloc ulong[4];
		ulong currentPower = 1;
		for (int j = 0; j < 4; j++)
		{
			powers[j] = currentPower;
			unchecked { currentPower *= _prime; }
		}
		var powersVec = Vector256.Create(powers);

		// Jump vector: [p^4, p^4, p^4, p^4]
		ulong p4 = 1;
		for (int j = 0; j < 4; j++)
		{
			unchecked { p4 *= _prime; }
		}
		var jumpVec = Vector256.Create(p4);

		Vector256<ulong> accVec = Vector256<ulong>.Zero;

		var selector = _valueSelector;
		ref T sourceRef = ref MemoryMarshal.GetReference(source);

		Span<ulong> buffer = stackalloc ulong[4];
		for (int i = 0; i < loopLimit; i += 4)
		{
			buffer[0] = (ulong)selector(Unsafe.Add(ref sourceRef, i));
			buffer[1] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 1));
			buffer[2] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 2));
			buffer[3] = (ulong)selector(Unsafe.Add(ref sourceRef, i + 3));

			var dataVec = Vector256.Create(buffer);

			unchecked
			{
				accVec += dataVec * powersVec;
				powersVec *= jumpVec;
			}
		}

		// Horizontal Reduction
		ulong hashValue = Vector256.Sum(accVec);

		// Handling the "Tail"
		ulong primePower = powersVec.GetElement(0);

		for (int i = loopLimit; i < length; i++)
		{
			ulong itemValue = (ulong)selector(Unsafe.Add(ref sourceRef, i));
			unchecked
			{
				hashValue += itemValue * primePower;
				primePower *= _prime;
			}
		}

		return hashValue & mask;
	}

	private ulong ComputeHashWithBitwiseMask(ReadOnlySpan<T> source)
	{
		ulong hashValue = 0, primePower = 1;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		foreach (T item in source)
		{
			ulong itemValue = (ulong)_valueSelector(item);

			unchecked
			{
				hashValue += itemValue * primePower;
				primePower *= _prime;
			}
		}

		return hashValue & mask;
	}
}
