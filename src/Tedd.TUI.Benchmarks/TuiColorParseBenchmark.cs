using BenchmarkDotNet.Attributes;
using System;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class TuiColorParseBenchmark
{
    private const string RgbInput = "rgb(255, 128, 64)";
    private const string RgbaInput = "rgba(255, 128, 64, 0.5)";

    [Benchmark(Baseline = true)]
    public TuiColor LegacyRgb() => TuiColorLegacy.ParseFunctional(RgbInput);

    [Benchmark]
    public TuiColor LegacyRgba() => TuiColorLegacy.ParseFunctional(RgbaInput);

    [Benchmark]
    public TuiColor OptimizedRgb() => TuiColor.FromHex(RgbInput);

    [Benchmark]
    public TuiColor OptimizedRgba() => TuiColor.FromHex(RgbaInput);
}
