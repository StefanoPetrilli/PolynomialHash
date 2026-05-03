namespace PolynomialHash;

public static class PolynomialHashExtensions
{
	public static ulong ToPolynomialHash<T>(this IEnumerable<T> source,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
	{
		var hasher = new PolynomialHasher<T>(null, prime, mod);
		return hasher.ComputeHash(source);
	}

	public static ulong ToPolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
	{
		var hasher = new PolynomialHasher<T>(valueSelector, prime, mod);
		return hasher.ComputeHash(source);
	}

	public static uint ToUInt32PolynomialHash<T>(this IEnumerable<T> source,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
		=> (uint)source.ToPolynomialHash(prime, mod);

	public static uint ToUInt32PolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
		=> (uint)source.ToPolynomialHash(valueSelector, prime, mod);

	public static int ToInt32PolynomialHash<T>(this IEnumerable<T> source,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
		=> unchecked((int)source.ToPolynomialHash(prime, mod));

	public static int ToInt32PolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
		=> unchecked((int)source.ToPolynomialHash(valueSelector, prime, mod));

	public static ulong ToUInt64PolynomialHash<T>(this IEnumerable<T> source,
		Func<T, long> valueSelector,
		ulong prime = HashConstants.DefaultPrime,
		ulong mod = HashConstants.DefaultMod)
		=> source.ToPolynomialHash(valueSelector, prime, mod);
}
