using BenchmarkDotNet.Attributes;

namespace PolynomialHash.Benchmarks;

[MemoryDiagnoser]
public class PolynomialHashBenchmarks
{
	private int[] _largeArray = null!;
	private List<int> _largeList = null!;
	private readonly int N = 1_000_000;

	[GlobalSetup]
	public void Setup()
	{
		_largeArray = [.. Enumerable.Range(0, N)];
		_largeList = [.. _largeArray];
	}

	[Benchmark(Baseline = true)]
	public ulong UInt64Hash_Array() => _largeArray.ToUInt64PolynomialHash(v => v);

	[Benchmark]
	public ulong UInt64Hash_List() => _largeList.ToUInt64PolynomialHash(v => v);
}
