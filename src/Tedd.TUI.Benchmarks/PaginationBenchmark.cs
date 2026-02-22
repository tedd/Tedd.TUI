using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Tedd.TUI.Benchmarks.Legacy;

namespace Tedd.TUI.Benchmarks;

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
        _table.CurrentPage = 50;
    }

    // Legacy Benchmarks (Baseline)

    [Benchmark(Baseline = true)]
    public string Legacy_ShortString()
    {
        // width 20, should fall back to "< 51 of 100 >"
        return PaginationLegacy.GetPaginationString(20, 100, 50);
    }

    [Benchmark]
    public string Legacy_FullList()
    {
        // width 100, total pages 10. Should show all.
        return PaginationLegacy.GetPaginationString(100, 10, 5);
    }

    [Benchmark]
    public string Legacy_Ellipses()
    {
        // width 100, total pages 100. Should show ellipses.
        return PaginationLegacy.GetPaginationString(100, 100, 50);
    }

    // Optimized Benchmarks

    [Benchmark]
    public int Optimized_ShortString()
    {
        Span<char> buffer = stackalloc char[256];
        return Table.GetPaginationString(buffer, 20, 100, 50);
    }

    [Benchmark]
    public int Optimized_FullList()
    {
        Span<char> buffer = stackalloc char[256];
        return Table.GetPaginationString(buffer, 100, 10, 5);
    }

    [Benchmark]
    public int Optimized_Ellipses()
    {
        Span<char> buffer = stackalloc char[256];
        return Table.GetPaginationString(buffer, 100, 100, 50);
    }
}
