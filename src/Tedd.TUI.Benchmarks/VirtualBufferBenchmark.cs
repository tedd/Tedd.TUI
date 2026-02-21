using BenchmarkDotNet.Attributes;
using System;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class VirtualBufferBenchmark
{
    private VirtualBuffer _buffer = null!;
    private VirtualBufferLegacy _legacyBuffer = null!;
    private const int Width = 200;
    private const int Height = 100;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new VirtualBuffer(Width, Height);
        _legacyBuffer = new VirtualBufferLegacy(Width, Height);
    }

    [Benchmark(Baseline = true)]
    public void Legacy_SetPixel_NoClip()
    {
        var b = _legacyBuffer;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                b.SetPixel(x, y, 'X', ConsoleColor.White, ConsoleColor.Black);
            }
        }
    }

    [Benchmark]
    public void Optimized_SetPixel_NoClip()
    {
        var b = _buffer;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                b.SetPixel(x, y, 'X', ConsoleColor.White, ConsoleColor.Black);
            }
        }
    }

    [Benchmark]
    public void Legacy_SetPixel_WithClip()
    {
        var b = _legacyBuffer;
        b.PushClip(new Rect(10, 10, 50, 50));
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                b.SetPixel(x, y, 'X', ConsoleColor.White, ConsoleColor.Black);
            }
        }
        b.PopClip();
    }

    [Benchmark]
    public void Optimized_SetPixel_WithClip()
    {
        var b = _buffer;
        b.PushClip(new Rect(10, 10, 50, 50));
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                b.SetPixel(x, y, 'X', ConsoleColor.White, ConsoleColor.Black);
            }
        }
        b.PopClip();
    }
}
