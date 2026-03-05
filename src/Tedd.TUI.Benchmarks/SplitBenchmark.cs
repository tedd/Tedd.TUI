using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class SplitBenchmark
    {
        private string _attachedProperty = "Grid.Row";
        private string _editorLines = "Line 1\r\nLine 2\nLine 3\rLine 4\r\n";
        private string _markdownText = "Hello world this is a test of split performance with many words.";
        private string _tableLine = "| Header 1 | Header 2 | Header 3 |";

        [Benchmark]
        public void AttachedProperty_Legacy() => SplitLegacy.SetAttachedProperty_Legacy(_attachedProperty);

        [Benchmark]
        public void AttachedProperty_Optimized() => SplitOptimized.SetAttachedProperty_Optimized(_attachedProperty);

        [Benchmark]
        public void TextEditorLines_Legacy() => SplitLegacy.TextEditorLines_Legacy(_editorLines);

        [Benchmark]
        public void TextEditorLines_Optimized() => SplitOptimized.TextEditorLines_Optimized(_editorLines);

        [Benchmark]
        public void MarkdownWords_Legacy() => SplitLegacy.ParseInlineWords_Legacy(_markdownText);

        [Benchmark]
        public void MarkdownWords_Optimized() => SplitOptimized.ParseInlineWords_Optimized(_markdownText);

        [Benchmark]
        public void MarkdownTable_Legacy() => SplitLegacy.ParseTableLine_Legacy(_tableLine);

        [Benchmark]
        public void MarkdownTable_Optimized() => SplitOptimized.ParseTableLine_Optimized(_tableLine);
    }
}
