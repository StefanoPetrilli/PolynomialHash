namespace PolynomialHash;

public sealed class PolynomialHasher<T> : IEqualityComparer<IEnumerable<T>>
{
	private readonly Func<T, long> _valueSelector;
	private readonly ulong _prime;
	private readonly ulong _mod;

	public PolynomialHasher(Func<T, long> valueSelector, ulong prime = HashConstants.DefaultPrime, ulong mod = HashConstants.DefaultMod)
	{
		ArgumentNullException.ThrowIfNull(valueSelector);
		_valueSelector = valueSelector;
		_prime = prime;
		_mod = mod;
	}

	public ulong ComputeHash(IEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);

		ulong hashValue = 0, primePower = 1, itemValue;

		foreach (T item in source)
		{
			itemValue = (ulong)_valueSelector(item) % _mod;

			hashValue = (hashValue + (itemValue * primePower)) % _mod;
			primePower = primePower * _prime % _mod;
		}

		return hashValue;
	}

	public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y) => (x, y) switch
	{
		_ when ReferenceEquals(x, y) => true,
		_ when x is null || y is null => false,
		_ => x.SequenceEqual(y)
	};

	public int GetHashCode(IEnumerable<T> obj)
	=> unchecked((int)ComputeHash(obj));
}
