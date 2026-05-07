using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace PolynomialHash.Benchmarks;

[MemoryDiagnoser]
public class PolynomialHashBenchmarks
{
	private int[] _intArray = null!;
	private long[] _longArray = null!;
	private string[] _stringArray = null!;

	private PolynomialHasher<int> _intHasherBitmask = null!;
	private PolynomialHasher<int> _intHasherModulo = null!;
	private PolynomialHasher<long> _longHasherBitmask = null!;
	private PolynomialHasher<string> _stringHasherBitmask = null!;

	private const int N = 100_000;

	[GlobalSetup]
	public void Setup()
	{
		_intArray = [.. Enumerable.Range(0, N)];
		_longArray = [.. Enumerable.Range(0, N).Select(x => (long)x)];
		_stringArray = [.. Enumerable.Range(0, N).Select(static x => x.ToString(CultureInfo.InvariantCulture))];

		_intHasherBitmask = new PolynomialHasher<int>(v => v, mod: 0);
		_intHasherModulo = new PolynomialHasher<int>(v => v, mod: 1_000_000_007);
		_longHasherBitmask = new PolynomialHasher<long>(v => v, mod: 0);
		_stringHasherBitmask = new PolynomialHasher<string>(v => v.Length, mod: 0);
	}

	[Benchmark]
	[BenchmarkCategory("PublicAPI")]
	public ulong ComputeHash_Array_Int32_Bitmask()
	  => _intHasherBitmask.ComputeHash(_intArray);

	[Benchmark]
	[BenchmarkCategory("PublicAPI")]
	public ulong ComputeHash_Array_Int32_Modulo()
	  => _intHasherModulo.ComputeHash(_intArray);

	[Benchmark]
	[BenchmarkCategory("LeafScalar")]
	public ulong Leaf_Scalar_Modulo_Int32()
	  => _intHasherModulo.ComputeHashWithModulo(_intArray.AsSpan(), new NumberMapper<int>());

	[Benchmark]
	[BenchmarkCategory("LeafScalar")]
	public ulong Leaf_Scalar_Bitmask_Int32()
	  => _intHasherBitmask.ComputeHashWithBitwiseMask(_intArray.AsSpan(), new NumberMapper<int>());

	[Benchmark]
	[BenchmarkCategory("LeafAVX2")]
	public ulong Leaf_AVX2_Int32()
	  => _intHasherBitmask.ComputeHashWithBitwiseMaskAVX2_Int32(_intArray);

	[Benchmark]
	[BenchmarkCategory("LeafAVX2")]
	public ulong Leaf_AVX2_Int64()
	  => _longHasherBitmask.ComputeHashWithBitwiseMaskAVX2_Int64(_longArray);

	[Benchmark]
	[BenchmarkCategory("LeafAVX2")]
	public ulong Leaf_AVX2_Generic()
	=> _stringHasherBitmask.ComputeHashWithBitwiseMaskAVX2_Generic(_stringArray, new DelegateMapper<string>(v => v.Length));

	[Benchmark]
	[BenchmarkCategory("LeafAVX512")]
	public ulong Leaf_AVX512_Int32()
	=> _intHasherBitmask.ComputeHashWithBitwiseMaskAVX512_Int32(_intArray);

	[Benchmark]
	[BenchmarkCategory("LeafAVX512")]
	public ulong Leaf_AVX512_Int64()
	  => _longHasherBitmask.ComputeHashWithBitwiseMaskAVX512_Int64(_longArray);

	[Benchmark]
	[BenchmarkCategory("LeafAVX512")]
	public ulong Leaf_AVX512_Generic()
	  => _stringHasherBitmask.ComputeHashWithBitwiseMaskAVX512_Generic(_stringArray, new DelegateMapper<string>(v => v.Length));
}
