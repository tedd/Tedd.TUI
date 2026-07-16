using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class ColorParseBenchmark
    {
        [Params("rgba(255, 128, 64, 0.5)", "rgb(255, 128, 64)")]
        public string ColorString { get; set; } = string.Empty;

        [Benchmark(Baseline = true)]
        public void LegacyColorParse()
        {
            TuiColorLegacy.FromHex(ColorString);
        }

        [Benchmark]
        public void OptimizedColorParse()
        {
            TuiColor.FromHex(ColorString);
        }
    }
}
