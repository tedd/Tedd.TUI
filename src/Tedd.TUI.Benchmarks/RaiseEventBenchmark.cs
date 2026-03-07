using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class RaiseEventBenchmark
{
    private UIElement _leaf;
    private RoutedEventArgs _eventArgs;

    private Tedd.TUI.Archive.UIElementLegacy _leafLegacy;

    [GlobalSetup]
    public void Setup()
    {
        var root = new Grid();
        UIElement current = root;

        for (int i = 0; i < 50; i++)
        {
            var child = new Border();
            child.Parent = current;
            current = child;
        }

        _leaf = current;
        _eventArgs = new MouseEventArgs(UIElement.MouseDownEvent, _leaf)
        {
            GlobalX = 10,
            GlobalY = 10
        };

        // Setup legacy hierarchy
        var rootLegacy = new Tedd.TUI.Archive.UIElementLegacy();
        Tedd.TUI.Archive.UIElementLegacy currentLegacy = rootLegacy;

        for (int i = 0; i < 50; i++)
        {
            var child = new Tedd.TUI.Archive.UIElementLegacy();
            child.Parent = currentLegacy;
            currentLegacy = child;
        }

        _leafLegacy = currentLegacy;
    }

    [Benchmark(Baseline = true)]
    public void LegacyRaiseEvent()
    {
        _eventArgs.Handled = false;
        _leafLegacy.RaiseEvent(_eventArgs);
    }

    [Benchmark]
    public void OptimizedRaiseEvent()
    {
        _eventArgs.Handled = false;
        _leaf.RaiseEvent(_eventArgs);
    }
}
