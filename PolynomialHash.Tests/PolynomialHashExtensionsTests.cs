namespace PolynomialHash.Tests;

public class PolynomialHashExtensionsTests()
{
	[Fact]
	public void EmptySource_ExpectsZero()
	{
		IEnumerable<int> source = [];
		Assert.Equal(0, source.ToInt64PolynomialHash(v => v));
	}

	[Fact]
	public void DifferentLengths_ExpectsDifferentHashes()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [1, 2, 3, 4];
		Assert.NotEqual(source1.ToInt64PolynomialHash(v => v), source2.ToInt64PolynomialHash(v => v));
	}

	[Fact]
	public void IdenticalContent_ExpectsSameHash()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [1, 2, 3];
		Assert.Equal(source1.ToInt64PolynomialHash(v => v), source2.ToInt64PolynomialHash(v => v));
	}

	[Fact]
	public void IdenticalContentDifferentPrime_ExpectsDifferentHashes()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [1, 2, 3];
		Assert.NotEqual(source1.ToInt64PolynomialHash(v => v, prime: 3), source2.ToInt64PolynomialHash(v => v, prime: 7));
	}

	[Fact]
	public void ValuesExceedingMod_ExpectsResultWithinModRange()
	{
		int customPrime = 10;
		int customMod = 7;
		int[] source = [2, 5];

		long result = source.ToInt64PolynomialHash(v => v, customPrime, customMod);

		Assert.True(result < customMod);
		Assert.Equal(3, result);
	}

	[Fact]
	public void LargeValues_ExpectsNoOverflow()
	{
		long[] source = [long.MaxValue - 1, long.MaxValue - 1];
		long result = source.ToInt64PolynomialHash(v => v);

		Assert.True(result >= 0);
	}

	[Fact]
	public void DifferentOrder_ExpectsDifferentHashes()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [3, 2, 1];
		Assert.NotEqual(source1.ToInt64PolynomialHash(v => v), source2.ToInt64PolynomialHash(v => v));
	}

	[Fact]
	public void NullSource_ExpectsArgumentNullException()
	{
		IEnumerable<int>? source = null;
		_ = Assert.Throws<ArgumentNullException>(() => source!.ToInt64PolynomialHash(v => v));
	}

	[Fact]
	public void NullSelector_ExpectsArgumentNullException()
	{
		int[] source = [1, 2, 3];
		_ = Assert.Throws<ArgumentNullException>(() => source.ToInt64PolynomialHash(null!));
	}

	[Fact]
	public void HashExceedsInt32Range_ExpectsTruncatedInt()
	{
		int largeMod = 2000000000;
		int[] source = [100, 200];
		int prime = 31;

		long longHash = source.ToInt64PolynomialHash(v => v, prime, largeMod);
		int intHash = source.ToPolynomialHash(v => v, prime, largeMod);

		Assert.Equal((int)longHash, intHash);
	}

	[Fact]
	public void ConsistentParameters_ExpectsMatchingResults()
	{
		int[] source = [1, 2, 3];

		long longResult = source.ToInt64PolynomialHash(v => v);
		int intResult = source.ToPolynomialHash(v => v);

		Assert.Equal((int)longResult, intResult);
	}
}
