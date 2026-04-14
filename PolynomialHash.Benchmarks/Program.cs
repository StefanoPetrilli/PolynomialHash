using BenchmarkDotNet.Running;


namespace PolynomialHash.Benchmarks;

public class Program
{
	public static void Main() => _ = BenchmarkRunner.Run<PolynomialHashBenchmarks>();
}
