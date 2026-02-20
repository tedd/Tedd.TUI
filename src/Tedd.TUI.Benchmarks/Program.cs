using BenchmarkDotNet.Running;
using Tedd.TUI.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<MarkdownBenchmark>();
    }
}
