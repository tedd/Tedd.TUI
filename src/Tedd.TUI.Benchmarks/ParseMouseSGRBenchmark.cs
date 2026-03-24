using BenchmarkDotNet.Attributes;
using System;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class ParseMouseSGRBenchmark
    {
        private string _sequence = "[<0;15;32M";

        [Benchmark(Baseline = true)]
        public void ParseMouseSGR_Legacy()
        {
            var result = ParseMouseSGRLegacy.ParseMouseSGR_Legacy(_sequence);
            if (result.HasValue)
            {
                _ = result.Value.btn + result.Value.x + result.Value.y + (result.Value.isDown ? 1 : 0);
            }
        }

        [Benchmark]
        public void ParseMouseSGR_Optimized()
        {
            var result = ParseMouseSGR_Opt(_sequence);
            if (result.HasValue)
            {
                _ = result.Value.btn + result.Value.x + result.Value.y + (result.Value.isDown ? 1 : 0);
            }
        }

        public static (int btn, int x, int y, bool isDown)? ParseMouseSGR_Opt(string seq)
        {
            // Delegate to the production parser to avoid duplicating parsing logic.
            return ConsoleInputManager.ParseMouseSGR(seq.AsSpan());
        }
    }
}
