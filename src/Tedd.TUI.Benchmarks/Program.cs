using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks
{
    // A concrete subclass to test the new optimized Panel
    public class TestPanel : Panel
    {
    }

    // A concrete subclass to test the legacy unoptimized Panel
    public class TestLegacyPanel : LegacyPanel
    {
    }

    [MemoryDiagnoser]
    public class PanelZIndexBenchmark
    {
        private TestPanel _panel;
        private TestLegacyPanel _legacyPanel;

        [Params(10, 50, 100)]
        public int ChildCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _panel = new TestPanel();
            _legacyPanel = new TestLegacyPanel();

            for (int i = 0; i < ChildCount; i++)
            {
                var el1 = new TextBlock { Text = $"Child {i}" };
                Panel.SetZIndex(el1, ChildCount - i); // Add in reverse order to force sorting
                _panel.AddChild(el1);

                var el2 = new TextBlock { Text = $"Child {i}" };
                Panel.SetZIndex(el2, ChildCount - i);
                _legacyPanel.AddChild(el2);
            }
        }

        [Benchmark(Baseline = true)]
        public void EnsureZSorted_Legacy()
        {
            _legacyPanel.InvalidateZState();
            _legacyPanel.EnsureZSorted();
        }

        [Benchmark]
        public void EnsureZSorted_Optimized()
        {
            _panel.InvalidateZState();
            var c = _panel.GetVisualChild(0);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
