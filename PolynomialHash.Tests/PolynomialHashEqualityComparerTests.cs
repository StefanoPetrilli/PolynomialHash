namespace PolynomialHash.Tests;

public class PolynomialHashEqualityComparerTests
{
	private readonly PolynomialHasher<int> _comparer = new(x => x);
	private static readonly int[] firstTestArray = [1, 2, 3];
	private static readonly int[] secondTestArray = [3, 2, 1];

	[Fact]
	public void Equals_SameReference_ReturnsTrue()
	{
		IEnumerable<int> source = [1, 2, 3];
		Assert.True(_comparer.Equals(source, source));
	}

	[Fact]
	public void Equals_IdenticalContent_ReturnsTrue()
	{
		IEnumerable<int> a = [1, 2, 3];
		IEnumerable<int> b = [1, 2, 3];
		Assert.True(_comparer.Equals(a, b));
	}

	[Fact]
	public void Equals_DifferentContent_ReturnsFalse()
	{
		IEnumerable<int> a = [1, 2, 3];
		IEnumerable<int> b = [1, 2, 4];
		Assert.False(_comparer.Equals(a, b));
	}

	[Fact]
	public void Equals_DifferentOrder_ReturnsFalse()
	{
		IEnumerable<int> a = [1, 2, 3];
		IEnumerable<int> b = [3, 2, 1];
		Assert.False(_comparer.Equals(a, b));
	}

	[Fact]
	public void Equals_DifferentLengths_ReturnsFalse()
	{
		IEnumerable<int> a = [1, 2, 3];
		IEnumerable<int> b = [1, 2, 3, 4];
		Assert.False(_comparer.Equals(a, b));
	}

	[Fact]
	public void Equals_BothNull_ReturnsTrue()
	=> Assert.True(_comparer.Equals(null, null));

	[Fact]
	public void Equals_OneNull_ReturnsFalse()
	{
		IEnumerable<int> a = [1, 2, 3];
		Assert.False(_comparer.Equals(a, null));
		Assert.False(_comparer.Equals(null, a));
	}

	[Fact]
	public void Equals_BothEmpty_ReturnsTrue()
	=> Assert.True(_comparer.Equals([], []));

	[Fact]
	public void GetHashCode_IdenticalContent_ReturnsSameHashCode()
	{
		IEnumerable<int> a = [1, 2, 3];
		IEnumerable<int> b = [1, 2, 3];
		Assert.Equal(_comparer.GetHashCode(a), _comparer.GetHashCode(b));
	}

	[Fact]
	public void GetHashCode_DifferentContent_ReturnsDifferentHashCode()
	{
		IEnumerable<int> a = [1, 2, 3];
		IEnumerable<int> b = [3, 2, 1];
		Assert.NotEqual(_comparer.GetHashCode(a), _comparer.GetHashCode(b));
	}

	[Fact]
	public void GetHashCode_EmptySequence_ReturnsZero()
	{
		IEnumerable<int> empty = [];
		Assert.Equal(0, _comparer.GetHashCode(empty));
	}

	[Fact]
	public void GetHashCode_NullSource_ThrowsArgumentNullException()
	=> _ = Assert.Throws<ArgumentNullException>(() => _comparer.GetHashCode(null!));

	[Fact]
	public void HashSet_DuplicateSequence_IsNotAdded()
	{
		var set = new HashSet<IEnumerable<int>>(_comparer)
		{ firstTestArray,
			secondTestArray,
			firstTestArray
		};

		Assert.Equal(2, set.Count);
	}

	[Fact]
	public void HashSet_Contains_FindsSequenceByValue()
	{
		var set = new HashSet<IEnumerable<int>>(_comparer) { firstTestArray };
		Assert.Contains([1, 2, 3], set);
	}

	[Fact]
	public void HashSet_DifferentOrderSequences_AreStoredSeparately()
	{
		var set = new HashSet<IEnumerable<int>>(_comparer)
		{
			firstTestArray,
			secondTestArray
		};

		Assert.Equal(2, set.Count);
	}

	[Fact]
	public void Dictionary_LookupByEqualSequence_ReturnsValue()
	{
		var dict = new Dictionary<IEnumerable<int>, string>(_comparer)
		{
			{ [10, 20, 30], "found it" }
		};

		Assert.Equal("found it", dict[[10, 20, 30]]);
	}

	[Fact]
	public void Dictionary_AddDuplicateKey_ThrowsArgumentException()
	{
		var dict = new Dictionary<IEnumerable<int>, string>(_comparer)
		{
			{ [1, 2, 3], "first" }
		};

		_ = Assert.Throws<ArgumentException>(() => dict.Add([1, 2, 3], "second"));
	}

	[Fact]
	public void Dictionary_DifferentSequences_StoredUnderSeparateKeys()
	{
		var dict = new Dictionary<IEnumerable<int>, string>(_comparer)
		{
			{ [1, 2, 3], "abc" },
			{ [3, 2, 1], "xyz" }
		};

		Assert.Equal(2, dict.Count);
		Assert.Equal("abc", dict[[1, 2, 3]]);
		Assert.Equal("xyz", dict[[3, 2, 1]]);
	}
}
