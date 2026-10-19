using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class TuiColorBenchmark
{
    private readonly string[] _colors = new[]
    {
        "rgb(255, 128, 0)",
        "rgba(255, 128, 0, 0.5)",
        "rgb(10,20,30)",
        "rgba( 100, 200, 50, 128 )"
    };

    [Benchmark(Baseline = true)]
    public void ParseFunctional_Legacy()
    {
        foreach (var c in _colors)
        {
            var _ = TuiColorLegacy.FromHex(c);
        }
    }

    [Benchmark]
    public void ParseFunctional_Optimized()
    {
        foreach (var c in _colors)
        {
            var _ = TuiColor.FromHex(c);
        }
    }
}
