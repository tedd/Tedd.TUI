using BenchmarkDotNet.Running;
using System.Reflection;

namespace Tedd.TUI.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<ConsoleRendererBenchmarks>();
        }
    }
}
