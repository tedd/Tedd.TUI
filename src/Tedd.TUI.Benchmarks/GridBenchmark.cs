using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Benchmarks.Legacy;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class GridBenchmark
{
    private Grid _grid;
    private GridLegacy _gridLegacy;
    private Size _availableSize;

    [GlobalSetup]
    public void Setup()
    {
        _grid = new Grid();
        // Add some children to make it do work
        _grid.AddChild(new Button { Content = "Button 1" });
        _grid.AddChild(new TextBlock { Text = "Text Block" });

        _gridLegacy = new GridLegacy();
        _gridLegacy.AddChild(new Button { Content = "Button 1" });
        _gridLegacy.AddChild(new TextBlock { Text = "Text Block" });

        _availableSize = new Size(100, 50);
    }

    [Benchmark(Baseline = true)]
    public void Legacy_Measure_ImplicitDefinitions()
    {
        _gridLegacy.Measure(_availableSize);
    }

    [Benchmark]
    public void Optimized_Measure_ImplicitDefinitions()
    {
        _grid.Measure(_availableSize);
    }
}
