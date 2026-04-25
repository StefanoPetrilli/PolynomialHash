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
		ulong hashValue = 0, primePower = 1, itemValue;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		foreach (T item in source)
		{
			itemValue = (ulong)_valueSelector(item) & mask;

			unchecked
			{
				hashValue = (hashValue + (itemValue * primePower)) & mask;
				primePower = (primePower * _prime) & mask;
			}
		}

		return hashValue;
	}

	private ulong SelectComputeHashWithBitwiseMask(ReadOnlySpan<T> source) => true switch
	{
		_ when Vector512.IsHardwareAccelerated && Avx512F.IsSupported => ComputeHashWithBitwiseMaskAVX512(source),
		_ when Vector256.IsHardwareAccelerated && Avx2.IsSupported => ComputeHashWithBitwiseMaskAVX2(source),
		_ => ComputeHashWithBitwiseMask(source)
	};

	private ulong ComputeHashWithBitwiseMaskAVX512(ReadOnlySpan<T> source)
	{
		return 0;
	}

	private ulong ComputeHashWithBitwiseMaskAVX2(ReadOnlySpan<T> source)
	{
		return 0;
	}

	private ulong ComputeHashWithBitwiseMask(ReadOnlySpan<T> source)
	{
		ulong hashValue = 0, primePower = 1, itemValue;
		ulong mask = _mod == 0 ? ulong.MaxValue : _mod - 1;

		foreach (T item in source)
		{
			itemValue = (ulong)_valueSelector(item) & mask;

			unchecked
			{
				hashValue = (hashValue + (itemValue * primePower)) & mask;
				primePower = (primePower * _prime) & mask;
			}
		}

		return hashValue;
	}
}
