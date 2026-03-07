using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class RaiseEventBenchmark2
{
    private UIElement _leaf;
    private RoutedEventArgs _eventArgs;

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
    }

    [Benchmark(Baseline = true)]
    public void LegacyRaiseEvent()
    {
        var e = _eventArgs;
        var route = new List<UIElement>();
        var current = _leaf;
        while (current != null)
        {
            route.Add(current);
            current = current.Parent;
        }

        if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
        {
            for (int i = route.Count - 1; i >= 0; i--)
            {
                if (route[i] == null) break;
            }
        }
        else if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble)
        {
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i] == null) break;
            }
        }
    }

    [Benchmark]
    public void OptimizedRaiseEventArrayPool()
    {
        var e = _eventArgs;

        int depth = 0;
        var current = _leaf;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }

        UIElement[] array = System.Buffers.ArrayPool<UIElement>.Shared.Rent(depth);
        try
        {
            current = _leaf;
            int idx = 0;
            while (current != null)
            {
                array[idx++] = current;
                current = current.Parent;
            }

            var route = array.AsSpan(0, depth);

            if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
            {
                for (int i = route.Length - 1; i >= 0; i--)
                {
                    if (route[i] == null) break;
                }
            }
            else if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble)
            {
                for (int i = 0; i < route.Length; i++)
                {
                    if (route[i] == null) break;
                }
            }
        }
        finally
        {
            System.Buffers.ArrayPool<UIElement>.Shared.Return(array, clearArray: true);
        }
    }
}
