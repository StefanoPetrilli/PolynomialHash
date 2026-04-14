namespace PolynomialHash;

public static class PolynomialHashExtensions
{
	private const ulong DefaultPrime = 31;
	private const ulong DefaultMod = 1000000009;

	public static ulong ToUInt64PolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		ulong prime = DefaultPrime,
		ulong mod = DefaultMod)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(valueSelector);

		ulong hashValue = 0, primePower = 1, itemValue;

		foreach (T item in source)
		{
			itemValue = (ulong)valueSelector(item) % mod;

			hashValue = (hashValue + (itemValue * primePower)) % mod;
			primePower = primePower * prime % mod;
		}

		return hashValue;
	}

	public static uint ToUInt32PolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		ulong prime = DefaultPrime,
		ulong mod = DefaultMod)
		=> (uint)source.ToUInt64PolynomialHash(valueSelector, prime, mod);
}
