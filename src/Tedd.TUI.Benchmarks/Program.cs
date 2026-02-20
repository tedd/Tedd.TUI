using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<PaginationBenchmark>();
        }
    }

    [MemoryDiagnoser]
    public class PaginationBenchmark
    {
        private Table _table = null!;

        [GlobalSetup]
        public void Setup()
        {
            _table = new Table();
            _table.PageSize = 10;
            // Add rows to simulate total pages
            for (int i = 0; i < 1000; i++)
            {
                _table.AddRow("Row " + i);
            }
            // Ensure CurrentPage is set to something in the middle to test ellipses
            _table.CurrentPage = 50; // Total pages = 1000 / 10 = 100. Page 50 is in middle.
        }

        [Benchmark]
        public string ShortString()
        {
            // width 20, should fall back to "< 51 of 100 >"
            return _table.GetPaginationString(20, 100);
        }

        [Benchmark]
        public string FullList()
        {
             // width 100, total pages 10. Should show all.
             // We force totalPages arg even if table has different count, as the method takes it as arg.
             return _table.GetPaginationString(100, 10);
        }

        [Benchmark]
        public string Ellipses()
        {
             // width 100, total pages 100. Should show ellipses.
             return _table.GetPaginationString(100, 100);
        }
    }
}
