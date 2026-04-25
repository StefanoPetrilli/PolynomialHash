using BenchmarkDotNet.Attributes;

namespace PolynomialHash.Benchmarks;

[MemoryDiagnoser]
public class PolynomialHashBenchmarks
{
	private int[] _largeArray = null!;
	private List<int> _largeList = null!;
	private IEnumerable<int> _largeEnumerable = null!;
	private PolynomialHasher<int> _moduloHasher = null!;
	private PolynomialHasher<int> _bitMaskHasher = null!;
	private PolynomialHasher<int> _zeroBitMaskHasher = null!;
	private const ulong StandardMod = 1_000_000_007;
	private const ulong PowerOfTwoMod = 1u << 20;
	private const ulong ZeroMod = 0;
	private readonly int N = 1_000_000;

	[GlobalSetup]
	public void Setup()
	{
		_largeArray = [.. Enumerable.Range(0, N)];
		_largeList = [.. _largeArray];
		_largeEnumerable = _largeArray.Select(x => x);

		_moduloHasher = new PolynomialHasher<int>(v => v, mod: StandardMod);
		_bitMaskHasher = new PolynomialHasher<int>(v => v, mod: PowerOfTwoMod);
		_zeroBitMaskHasher = new PolynomialHasher<int>(v => v, mod: ZeroMod);
	}

	[Benchmark(Baseline = true)]
	public ulong IEnumerable_WithModulo()
		=> _moduloHasher.ComputeHash(_largeEnumerable);

	[Benchmark]
	public ulong IEnumerable_WithBitwiseMask()
		=> _bitMaskHasher.ComputeHash(_largeEnumerable);

	[Benchmark]
	public ulong Array_WithModulo()
		=> _moduloHasher.ComputeHash(_largeArray);

	[Benchmark]
	public ulong Array_WithBitwiseMask()
		=> _bitMaskHasher.ComputeHash(_largeArray);

	[Benchmark]
	public ulong Array_WithBitwiseMask_ZeroMod()
		=> _zeroBitMaskHasher.ComputeHash(_largeArray);

	[Benchmark]
	public ulong List_WithModulo()
		=> _moduloHasher.ComputeHash(_largeList);

	[Benchmark]
	public ulong List_WithBitwiseMask()
		=> _bitMaskHasher.ComputeHash(_largeList);

	[Benchmark]
	public ulong List_WithBitwiseMask_ZeroMod()
		=> _zeroBitMaskHasher.ComputeHash(_largeList);
}
