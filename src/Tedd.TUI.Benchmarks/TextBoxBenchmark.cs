using BenchmarkDotNet.Attributes;
using System;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class TextBoxBenchmark
{
    private VirtualBuffer _buffer;
    private TextBox _modernTextBox;
    private TextBoxLegacy _legacyTextBox;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new VirtualBuffer(120, 30);

        _modernTextBox = new TextBox
        {
            IsPassword = true,
            Text = "SuperSecretPassword123!",
            Width = 50,
            Height = 1
        };
        _modernTextBox.Measure(new Size(120, 30));
        _modernTextBox.Arrange(new Rect(0, 0, 50, 1));

        _legacyTextBox = new TextBoxLegacy
        {
            IsPassword = true,
            Text = "SuperSecretPassword123!",
            Width = 50,
            Height = 1
        };
        _legacyTextBox.Measure(new Size(120, 30));
        _legacyTextBox.Arrange(new Rect(0, 0, 50, 1));
    }

    [Benchmark(Baseline = true)]
    public void Render_Legacy()
    {
        _legacyTextBox.Render(_buffer, 0, 0);
    }

    [Benchmark]
    public void Render_Modern()
    {
        _modernTextBox.Render(_buffer, 0, 0);
    }
}
