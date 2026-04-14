namespace PolynomialHash.Tests;

public class PolynomialHashExtensionsTests()
{
	[Fact]
	public void EmptySource_ExpectsZero()
	{
		IEnumerable<int> source = [];
		Assert.Equal((ulong)0, source.ToUInt64PolynomialHash(v => v));
	}

	[Fact]
	public void DifferentLengths_ExpectsDifferentHashes()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [1, 2, 3, 4];
		Assert.NotEqual(source1.ToUInt64PolynomialHash(v => v), source2.ToUInt64PolynomialHash(v => v));
	}

	[Fact]
	public void IdenticalContent_ExpectsSameHash()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [1, 2, 3];
		Assert.Equal(source1.ToUInt64PolynomialHash(v => v), source2.ToUInt64PolynomialHash(v => v));
	}

	[Fact]
	public void IdenticalContentDifferentPrime_ExpectsDifferentHashes()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [1, 2, 3];
		Assert.NotEqual(source1.ToUInt64PolynomialHash(v => v, prime: 3), source2.ToUInt64PolynomialHash(v => v, prime: 7));
	}

	[Fact]
	public void ValuesExceedingMod_ExpectsResultWithinModRange()
	{
		ulong customPrime = 10;
		ulong customMod = 7;
		int[] source = [2, 5];

		ulong result = source.ToUInt64PolynomialHash(v => v, customPrime, customMod);

		Assert.True(result < customMod);
		Assert.Equal((ulong)3, result);
	}

	[Fact]
	public void LargeValues_ExpectsNoOverflow()
	{
		long[] source = [long.MaxValue - 1, long.MaxValue - 1];
		ulong result = source.ToUInt64PolynomialHash(v => v);

		Assert.True(result >= 0);
	}

	[Fact]
	public void DifferentOrder_ExpectsDifferentHashes()
	{
		IEnumerable<int> source1 = [1, 2, 3];
		IEnumerable<int> source2 = [3, 2, 1];
		Assert.NotEqual(source1.ToUInt64PolynomialHash(v => v), source2.ToUInt64PolynomialHash(v => v));
	}

	[Fact]
	public void NullSource_ExpectsArgumentNullException()
	{
		IEnumerable<int>? source = null;
		_ = Assert.Throws<ArgumentNullException>(() => source!.ToUInt64PolynomialHash(v => v));
	}

	[Fact]
	public void NullSelector_ExpectsArgumentNullException()
	{
		int[] source = [1, 2, 3];
		_ = Assert.Throws<ArgumentNullException>(() => source.ToUInt64PolynomialHash(null!));
	}

	[Fact]
	public void HashExceedsInt32Range_ExpectsTruncatedInt()
	{
		int[] source = [100, 200];
		ulong largeMod = 2000000000;
		ulong prime = 31;

		ulong longHash = source.ToUInt64PolynomialHash(v => v, prime, largeMod);
		uint intHash = source.ToUInt32PolynomialHash(v => v, prime, largeMod);

		Assert.Equal((uint)longHash, intHash);
	}

	[Fact]
	public void ConsistentParameters_ExpectsMatchingResults()
	{
		int[] source = [1, 2, 3];

		ulong longResult = source.ToUInt64PolynomialHash(v => v);
		uint intResult = source.ToUInt32PolynomialHash(v => v);

		Assert.Equal((uint)longResult, intResult);
	}
}
