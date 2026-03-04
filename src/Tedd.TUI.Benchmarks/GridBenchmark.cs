using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class GridBenchmark
{
    private Grid _gridImplicit;
    private GridLegacy _gridLegacyImplicit;

    private Grid _gridStar;
    private GridLegacy _gridLegacyStar;

    private Size _availableSize;

    [GlobalSetup]
    public void Setup()
    {
        _availableSize = new Size(100, 50);

        // Implicit Setup
        _gridImplicit = new Grid();
        _gridImplicit.AddChild(new Button { Content = "Button 1" });
        _gridImplicit.AddChild(new TextBlock { Text = "Text Block" });

        _gridLegacyImplicit = new GridLegacy();
        _gridLegacyImplicit.AddChild(new Button { Content = "Button 1" });
        _gridLegacyImplicit.AddChild(new TextBlock { Text = "Text Block" });

        // Star Setup
        _gridStar = new Grid();
        _gridStar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _gridStar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        _gridStar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _gridStar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        var btn = new Button { Content = "Star Button" };
        Grid.SetColumn(btn, 0);
        Grid.SetRow(btn, 0);
        _gridStar.AddChild(btn);

        var txt = new TextBlock { Text = "Star Text" };
        Grid.SetColumn(txt, 1);
        Grid.SetRow(txt, 1);
        _gridStar.AddChild(txt);


        _gridLegacyStar = new GridLegacy();
        _gridLegacyStar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _gridLegacyStar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        _gridLegacyStar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _gridLegacyStar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        var btnLegacy = new Button { Content = "Star Button" };
        GridLegacy.SetColumn(btnLegacy, 0); // Using GridLegacy attached property
        GridLegacy.SetRow(btnLegacy, 0);
        _gridLegacyStar.AddChild(btnLegacy);

        var txtLegacy = new TextBlock { Text = "Star Text" };
        GridLegacy.SetColumn(txtLegacy, 1);
        GridLegacy.SetRow(txtLegacy, 1);
        _gridLegacyStar.AddChild(txtLegacy);
    }

    [Benchmark(Baseline = true)]
    public void Legacy_Measure_Implicit()
    {
        _gridLegacyImplicit.Measure(_availableSize);
    }

    [Benchmark]
    public void Optimized_Measure_Implicit()
    {
        _gridImplicit.Measure(_availableSize);
    }

    [Benchmark]
    public void Legacy_Measure_Star()
    {
        _gridLegacyStar.Measure(_availableSize);
    }

    [Benchmark]
    public void Optimized_Measure_Star()
    {
        _gridStar.Measure(_availableSize);
    }
}
