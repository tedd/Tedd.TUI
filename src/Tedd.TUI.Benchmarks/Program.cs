using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Tedd.TUI.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<PaginationBenchmark>();
        BenchmarkRunner.Run<MarkdownBenchmark>();
        BenchmarkRunner.Run<VisualTreeBenchmark>();
    }
}
