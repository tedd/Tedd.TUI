using BenchmarkDotNet.Attributes;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;
[MemoryDiagnoser]
public class TextBlockBenchmark
{
    private string _text = string.Empty;

    [Params(20, 50, 100)]
    public int MaxWidth { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _text = "This is a long text that needs to be wrapped. It contains many words and spaces. " +
                "Some words are incredibly long, like thisoneherewhichisveryverylongindeed. " +
                "Let's see how the performance compares when word-wrapping this paragraph multiple times. " +
                "It is important to optimize the layout pass because text wrapping happens frequently " +
                "during MeasureOverride.   We   also   have   multiple   spaces   here.\r\n" +
                "And here is an explicit new line. Followed by more text to ensure we hit all branches.\n" +
                "Let's add even more text to make the benchmark more pronounced.";
    }

    [Benchmark(Baseline = true)]
    public void WrapText_Legacy()
    {
        // WrapText is a private method, we need to invoke it via reflection or
        // trigger MeasureOverride on a fresh control.
        var tb = new TextBlockLegacy { Text = _text, TextWrapping = Tedd.TUI.TextWrapping.Wrap };
        tb.Measure(new Size(MaxWidth, 1000));
    }

    [Benchmark]
    public void WrapText_Optimized()
    {
        var tb = new TextBlock { Text = _text, TextWrapping = Tedd.TUI.TextWrapping.Wrap };
        tb.Measure(new Size(MaxWidth, 1000));
    }
}
