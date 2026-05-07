using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System.Runtime.Intrinsics.X86;

namespace PolynomialHash.Benchmarks;

public class HardwareSupportFilter : IFilter
{
  public bool Predicate(BenchmarkCase benchmarkCase)
  {
    string[] categories = benchmarkCase.Descriptor.Categories;

    if (categories.Contains("LeafAVX2") && !Avx2.IsSupported)
    {
      return false;
    }

    return !categories.Contains("LeafAVX512") || Avx512F.IsSupported;
  }
}

public class HardwareConfig : ManualConfig
{
  public HardwareConfig()
  {
    _ = AddFilter(new HardwareSupportFilter());
  }
}
