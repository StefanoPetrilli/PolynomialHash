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
		var random = new Random(42);
		_largeArray = [.. Enumerable.Range(0, N).Select(_ => random.Next(1, 1000))];
		_largeList = [.. _largeArray];
	}

	[Benchmark(Baseline = true)]
	public long Int64Hash_Array() => _largeArray.ToInt64PolynomialHash(v => v);

	[Benchmark]
	public long Int64Hash_List() => _largeList.ToInt64PolynomialHash(v => v);
}
