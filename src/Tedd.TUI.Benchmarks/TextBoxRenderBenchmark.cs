using BenchmarkDotNet.Attributes;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class TextBoxRenderBenchmark
{
    private VirtualBuffer _buffer;
    private TextBoxLegacy _legacyTextBox;
    private TextBox _optimizedTextBox;

    [Params(10, 50, 200)]
    public int TextLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new VirtualBuffer(300, 300);

        string passwordText = new string('A', TextLength);

        _legacyTextBox = new TextBoxLegacy
        {
            Text = passwordText,
            IsPassword = true,
            PasswordChar = '*',
            Width = 200
        };

        _optimizedTextBox = new TextBox
        {
            Text = passwordText,
            IsPassword = true,
            PasswordChar = '*',
            Width = 200
        };

        _legacyTextBox.Measure(new Size(200, 1));
        _legacyTextBox.Arrange(new Rect(0, 0, 200, 1));

        _optimizedTextBox.Measure(new Size(200, 1));
        _optimizedTextBox.Arrange(new Rect(0, 0, 200, 1));
    }

    [Benchmark(Baseline = true)]
    public void LegacyRender()
    {
        _legacyTextBox.Render(_buffer, 0, 0);
    }

    [Benchmark]
    public void OptimizedRender()
    {
        _optimizedTextBox.Render(_buffer, 0, 0);
    }
}
