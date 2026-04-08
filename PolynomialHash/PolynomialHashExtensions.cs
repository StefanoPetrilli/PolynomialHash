namespace PolynomialHash;

public static class PolynomialHashExtensions
{
	private const int DefaultPrime = 31;
	private const int DefaultMod = 1000000009;

	public static long ToInt64PolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		int prime = DefaultPrime,
		int mod = DefaultMod)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(valueSelector);

		long hashValue = 0;
		long primePower = 1;

		foreach (T item in source)
		{
			hashValue = (hashValue + valueSelector(item) * primePower) % mod;
			primePower = primePower * prime % mod;
		}

		return hashValue;
	}

	public static int ToPolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		int prime = DefaultPrime,
		int mod = DefaultMod)
		=> (int)source.ToInt64PolynomialHash(valueSelector, prime, mod);
}