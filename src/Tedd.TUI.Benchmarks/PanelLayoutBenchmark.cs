using BenchmarkDotNet.Attributes;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Tedd.TUI.Archive.Controls;
using System.Collections.Generic;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class PanelLayoutBenchmark
{
    private CanvasLegacy _canvasLegacy;
    private Canvas _canvasOptimized;
    private StackPanelLegacy _stackPanelLegacy;
    private StackPanel _stackPanelOptimized;
    private Size _availableSize;

    [GlobalSetup]
    public void Setup()
    {
        _availableSize = new Size(100, 100);

        _canvasLegacy = new CanvasLegacy();
        _canvasOptimized = new Canvas();
        _stackPanelLegacy = new StackPanelLegacy();
        _stackPanelOptimized = new StackPanel();

        // Add typical number of children
        for (int i = 0; i < 50; i++)
        {
            _canvasLegacy.Children.Add(new TextBlock { Text = "Test" });
            _canvasOptimized.Children.Add(new TextBlock { Text = "Test" });
            _stackPanelLegacy.Children.Add(new TextBlock { Text = "Test" });
            _stackPanelOptimized.Children.Add(new TextBlock { Text = "Test" });
        }
    }

    [Benchmark(Baseline = true)]
    public void CanvasLegacy()
    {
        _canvasLegacy.Measure(_availableSize);
        _canvasLegacy.Arrange(new Rect(0, 0, 100, 100));
    }

    [Benchmark]
    public void CanvasOptimized()
    {
        _canvasOptimized.Measure(_availableSize);
        _canvasOptimized.Arrange(new Rect(0, 0, 100, 100));
    }

    [Benchmark]
    public void StackPanelLegacy()
    {
        _stackPanelLegacy.Measure(_availableSize);
        _stackPanelLegacy.Arrange(new Rect(0, 0, 100, 100));
    }

    [Benchmark]
    public void StackPanelOptimized()
    {
        _stackPanelOptimized.Measure(_availableSize);
        _stackPanelOptimized.Arrange(new Rect(0, 0, 100, 100));
    }
}
