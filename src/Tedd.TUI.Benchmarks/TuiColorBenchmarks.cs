using BenchmarkDotNet.Attributes;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class TuiColorBenchmarks
    {
        [Params("rgb(255, 128, 0)", "rgba(255, 128, 0, 0.5)")]
        public string ColorString { get; set; } = "";

        [Benchmark(Baseline = true)]
        public void LegacyFunctional()
        {
            _ = TuiColorLegacy.ParseFunctional_Legacy(ColorString);
        }

        [Benchmark]
        public void OptimizedFunctional()
        {
            _ = TuiColor.FromHex(ColorString);
        }
    }
}
