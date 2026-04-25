namespace PolynomialHash.Tests;

public class PolynomialHasherBranchTests
{
	private static readonly int[] _data = [1, 2, 3, 4, 5];
	public static TheoryData<ulong> AllMods => [HashConstants.DefaultPrime, 1u << 20, 0];
	public static TheoryData<ulong> PowerOfTwoMods => [1u << 20, 0];

	[Theory, MemberData(nameof(AllMods))]
	public void AllDataTypes_ProduceSameHash(ulong mod)
	{
		var hasher = new PolynomialHasher<int>(v => v, HashConstants.DefaultPrime, mod);
		var list = new List<int>(_data);

		ulong hashArray = hasher.ComputeHash(_data);
		ulong hashList = hasher.ComputeHash(list);
		ulong hashEnum = hasher.ComputeHash(_data.AsEnumerable());
		ulong hashSpan = hasher.ComputeHash(new ReadOnlySpan<int>(_data));

		Assert.Equal(hashArray, hashList);
		Assert.Equal(hashArray, hashEnum);
		Assert.Equal(hashArray, hashSpan);
	}

	[Theory, MemberData(nameof(AllMods))]
	public void ComputeHash_IsDeterministic(ulong mod)
	{
		var hasher = new PolynomialHasher<int>(v => v, HashConstants.DefaultPrime, mod);

		Assert.Equal(hasher.ComputeHash(_data), hasher.ComputeHash(_data));
		Assert.Equal(hasher.ComputeHash(new List<int>(_data)), hasher.ComputeHash(new List<int>(_data)));
		Assert.Equal(hasher.ComputeHash(_data.AsEnumerable()), hasher.ComputeHash(_data.AsEnumerable()));
	}

	[Theory, MemberData(nameof(PowerOfTwoMods))]
	public void PowerOfTwoMod_Array_DoesNotThrow(ulong mod)
	{
		var hasher = new PolynomialHasher<int>(v => v, HashConstants.DefaultPrime, mod);
		Assert.Null(Record.Exception(() => hasher.ComputeHash(_data)));
	}

	[Theory, MemberData(nameof(PowerOfTwoMods))]
	public void PowerOfTwoMod_List_DoesNotThrow(ulong mod)
	{
		var hasher = new PolynomialHasher<int>(v => v, HashConstants.DefaultPrime, mod);
		Assert.Null(Record.Exception(() => hasher.ComputeHash(new List<int>(_data))));
	}

	[Theory, MemberData(nameof(PowerOfTwoMods))]
	public void PowerOfTwoMod_IEnumerable_DoesNotThrow(ulong mod)
	{
		var hasher = new PolynomialHasher<int>(v => v, HashConstants.DefaultPrime, mod);
		Assert.Null(Record.Exception(() => hasher.ComputeHash(_data.AsEnumerable())));
	}

	[Theory, MemberData(nameof(PowerOfTwoMods))]
	public void PowerOfTwoMod_ReadOnlySpan_DoesNotThrow(ulong mod)
	{
		var hasher = new PolynomialHasher<int>(v => v, HashConstants.DefaultPrime, mod);
		Assert.Null(Record.Exception(() => hasher.ComputeHash(new ReadOnlySpan<int>(_data))));
	}
}
