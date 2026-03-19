using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Tedd.TUI;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class SortBenchmark
    {
        private UIElement[] _elements;

        [Params(10, 100)]
        public int Count;

        private PanelArchive _archivePanel;
        private Canvas _currentPanel;

        [GlobalSetup]
        public void Setup()
        {
            _archivePanel = new ArchiveCanvas(); // A derived class to instantiate it
            _currentPanel = new Canvas();

            var rnd = new Random(42);
            for(int i=0; i<Count; i++)
            {
                var el = new Tedd.TUI.Canvas();
                int z = rnd.Next(-2, 3);

                var el2 = new Tedd.TUI.Canvas();
                Tedd.TUI.Panel.SetZIndex(el, z);
                Tedd.TUI.Archive.PanelArchive.SetZIndex(el2, z);

                _currentPanel.AddChild(el);
                _archivePanel.AddChild(el2);
            }
        }

        private class ArchiveCanvas : PanelArchive
        {
            protected override Size MeasureOverride(Size availableSize) => new Size();
        }

        [Benchmark]
        public UIElement IterativeMergeSort_Archive()
        {
            _archivePanel.InvalidateZState();
            return _archivePanel.GetVisualChild(Count - 1);
        }

        [Benchmark]
        public UIElement MemoryExtensionsSort_Optimized()
        {
            _currentPanel.InvalidateZState();
            return _currentPanel.GetVisualChild(Count - 1);
        }
    }
}
