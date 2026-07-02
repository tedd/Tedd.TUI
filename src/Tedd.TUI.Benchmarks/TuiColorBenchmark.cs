using BenchmarkDotNet.Attributes;
using System;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class TuiColorBenchmark
{
    private string[] _colors = new[]
    {
        "rgb(255, 128, 64)",
        "rgba(255, 128, 64, 128)",
        "rgba( 255 , 128 , 64 , 128 )"
    };

    [Benchmark(Baseline = true)]
    public void LegacyParseFunctional()
    {
        foreach (var color in _colors)
        {
            TuiColorArchive.FromHex(color);
        }
    }

    [Benchmark]
    public void OptimizedParseFunctional()
    {
        foreach (var color in _colors)
        {
            TuiColor.FromHex(color);
        }
    }
}
