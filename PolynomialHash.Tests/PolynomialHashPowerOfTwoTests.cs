using System.Reflection.Metadata;

namespace PolynomialHash.Tests;

public class PolynomialHashPowerOfTwoTests
{
	[Fact]
	public void PowerOfTwoMod_MatchesManualCalculation()
	{
		int[] source = [1, 2, 3];
		ulong prime = 31;
		ulong mod = 256;

		ulong hash = source.ToUInt64PolynomialHash(v => v, prime, mod);

		Assert.Equal((ulong)130, hash);
	}

	[Fact]
	public void ModuloZero_HandlesAsPowerOfTwoOverflow()
	{
		int[] source = [1, 2, 3];
		ulong prime = 31;
		ulong mod = 0;

		ulong hash = source.ToUInt64PolynomialHash(v => v, prime, mod);

		Assert.Equal((ulong)2946, hash);
	}

	[Fact]
	public void FastPath0_MatchesStandardPathMax_ForSmallInputs()
	{
		int[] source = [1, 2, 3];
		ulong prime = 31;

		// mod 0 -> Fast path (native 2^64 overflow)
		ulong hash0 = source.ToUInt64PolynomialHash(v => v, prime, 0);

		// mod ulong.MaxValue -> Standard path (2^64 - 1)
		ulong hashMax = source.ToUInt64PolynomialHash(v => v, prime, HashConstants.DefaultMod);

		// For small inputs that don't wrap around 2^64 - 1, they should be identical.
		Assert.Equal(hashMax, hash0);
	}
}

