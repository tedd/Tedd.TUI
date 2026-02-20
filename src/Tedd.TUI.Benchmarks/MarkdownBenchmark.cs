using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Tedd.TUI.Markdown;
using System.Text;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class MarkdownBenchmark
{
    private string _markdown = string.Empty;
    private MarkdownParser _parser = default!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new MarkdownParser(new MarkdownTheme());

        var sb = new StringBuilder();
        // Generate a large number of quote lines to trigger the inefficient string concatenation
        for (int i = 0; i < 5000; i++)
        {
            sb.AppendLine($"> This is quote line number {i} which will be concatenated efficiently or inefficiently depending on the implementation.");
        }
        _markdown = sb.ToString();
    }

    [Benchmark]
    public void ParseQuotes()
    {
        _parser.Parse(_markdown);
    }
}
