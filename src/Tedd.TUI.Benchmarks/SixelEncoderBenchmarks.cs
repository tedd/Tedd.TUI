using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using Tedd.TUI.Media;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]

public class SixelEncoderBenchmarks
{
    private byte[] _pixels = null!;

    [Params(16, 64, 256)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pixels = new byte[Size * Size * 4];
        var rnd = new Random(42);
        rnd.NextBytes(_pixels);
    }

    [Benchmark(Baseline = true)]
    public string Legacy_StringBuilder()
    {
        return Tedd.TUI.Archive.Media.SixelEncoderCore.EncodePixels(_pixels, Size, Size);
    }

    [Benchmark]
    public string Optimized_ArrayPool()
    {
        return Tedd.TUI.Media.SixelEncoderCore.EncodePixels(_pixels, Size, Size);
    }
}
