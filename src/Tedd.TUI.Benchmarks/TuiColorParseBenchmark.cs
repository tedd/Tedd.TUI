using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class TuiColorParseBenchmark
{
    [Benchmark(Baseline = true)]
    public void ParseHexLegacy()
    {
        TuiColorLegacy.FromHex("#AABBCC");
        TuiColorLegacy.FromHex("#11223344");
        TuiColorLegacy.FromHex("rgb(255, 128, 64)");
        TuiColorLegacy.FromHex("rgba(255, 128, 64, 0.5)");
        TuiColorLegacy.FromHex("Red");
    }

    [Benchmark]
    public void ParseHexOptimized()
    {
        TuiColor.FromHex("#AABBCC");
        TuiColor.FromHex("#11223344");
        TuiColor.FromHex("rgb(255, 128, 64)");
        TuiColor.FromHex("rgba(255, 128, 64, 0.5)");
        TuiColor.FromHex("Red");
    }
}
