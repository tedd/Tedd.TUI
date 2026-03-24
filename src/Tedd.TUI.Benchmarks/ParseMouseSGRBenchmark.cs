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
            try
            {
                ReadOnlySpan<char> span = seq.AsSpan();

                // Format: [<0;x;yM or [<0;x;ym
                if (span.Length < 6 || !span.StartsWith("[<")) return null;

                ReadOnlySpan<char> clean = span.Slice(2);
                char lastChar = clean[^1];
                clean = clean.Slice(0, clean.Length - 1);

                int firstSemi = clean.IndexOf(';');
                if (firstSemi == -1) return null;

                int secondSemi = clean.Slice(firstSemi + 1).IndexOf(';');
                if (secondSemi == -1) return null;
                secondSemi += firstSemi + 1;

                if (int.TryParse(clean.Slice(0, firstSemi), out int btn) &&
                    int.TryParse(clean.Slice(firstSemi + 1, secondSemi - firstSemi - 1), out int x) &&
                    int.TryParse(clean.Slice(secondSemi + 1), out int y))
                {
                    x -= 1;
                    y -= 1;
                    bool isDown = (lastChar == 'M');
                    return (btn, x, y, isDown);
                }
            }
            catch { }
            return null;
        }
    }
}
