using BenchmarkDotNet.Running;

namespace Tedd.TUI.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SplitBenchmark>();
        }
    }
}
