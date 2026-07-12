using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Archive.Controls;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class TextBlockWrapBenchmark
    {
        [Params(10, 40, 100)]
        public int MaxWidth { get; set; }

        private const string ShortText = "This is a short line of text.";
        private const string LongText = "This is a much longer line of text designed to force the text block wrapping algorithm to do some actual work and hopefully show a significant difference between the old StringBuilder approach and the new Span approach.";
        private const string EdgeCaseText = "VeryLongWordWithoutSpacesThatWillForceAHardBreakInTheWrappingAlgorithmBecauseItIsTooLongForAnyReasonableWidth.";

        [Benchmark(Baseline = true)]
        public void LegacyWrapShort()
        {
            var output = new List<string>();
            TextBlockLegacy.WrapSingleLine(ShortText, MaxWidth, output);
        }

        [Benchmark]
        public void OptimizedWrapShort()
        {
            var output = new List<string>();
            TextBlock.WrapSingleLine(ShortText, MaxWidth, output);
        }

        [Benchmark]
        public void LegacyWrapLong()
        {
            var output = new List<string>();
            TextBlockLegacy.WrapSingleLine(LongText, MaxWidth, output);
        }

        [Benchmark]
        public void OptimizedWrapLong()
        {
            var output = new List<string>();
            TextBlock.WrapSingleLine(LongText, MaxWidth, output);
        }

        [Benchmark]
        public void LegacyWrapEdgeCase()
        {
            var output = new List<string>();
            TextBlockLegacy.WrapSingleLine(EdgeCaseText, MaxWidth, output);
        }

        [Benchmark]
        public void OptimizedWrapEdgeCase()
        {
            var output = new List<string>();
            TextBlock.WrapSingleLine(EdgeCaseText, MaxWidth, output);
        }
    }
}
