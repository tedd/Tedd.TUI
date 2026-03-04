using BenchmarkDotNet.Attributes;
using System;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class BulkRenderBenchmark
{
    private VirtualBuffer _buffer = null!;
    private VirtualBufferBaseline _baseline = null!;
    private const int Width = 200;
    private const int Height = 100;
    private const string ShortText = "Hello World";
    private const string LongText = "This is a longer string to test the performance of bulk rendering in VirtualBuffer compared to per-character SetPixel calls.";

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new VirtualBuffer(Width, Height);
        _baseline = new VirtualBufferBaseline(Width, Height);
    }

    [Benchmark(Baseline = true)]
    public void Baseline_DrawString_Short()
    {
        var b = _baseline;
        string text = ShortText;
        for (int i = 0; i < text.Length; i++)
        {
            b.SetPixel(10 + i, 10, text[i], ConsoleColor.White, ConsoleColor.Black);
        }
    }

    [Benchmark]
    public void Optimized_DrawString_Short()
    {
        _buffer.DrawString(10, 10, ShortText, ConsoleColor.White, ConsoleColor.Black);
    }

    [Benchmark]
    public void Baseline_DrawString_Long()
    {
        var b = _baseline;
        string text = LongText;
        for (int i = 0; i < text.Length; i++)
        {
            b.SetPixel(10 + i, 20, text[i], ConsoleColor.White, ConsoleColor.Black);
        }
    }

    [Benchmark]
    public void Optimized_DrawString_Long()
    {
        _buffer.DrawString(10, 20, LongText, ConsoleColor.White, ConsoleColor.Black);
    }

    [Benchmark]
    public void Baseline_DrawHLine()
    {
        var b = _baseline;
        int len = 100;
        for (int i = 0; i < len; i++)
        {
            b.SetPixel(10 + i, 30, '-', ConsoleColor.White, ConsoleColor.Black);
        }
    }

    [Benchmark]
    public void Optimized_DrawHLine()
    {
        _buffer.DrawHLine(10, 30, 100, '-', ConsoleColor.White, ConsoleColor.Black);
    }

    [Benchmark]
    public void Baseline_DrawVLine()
    {
        var b = _baseline;
        int len = 50;
        for (int i = 0; i < len; i++)
        {
            b.SetPixel(40, 10 + i, '|', ConsoleColor.White, ConsoleColor.Black);
        }
    }

    [Benchmark]
    public void Optimized_DrawVLine()
    {
        _buffer.DrawVLine(40, 10, 50, '|', ConsoleColor.White, ConsoleColor.Black);
    }

    [Benchmark]
    public void Baseline_FillRect()
    {
        var b = _baseline;
        int w = 50;
        int h = 20;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                b.SetPixel(50 + x, 40 + y, '#', ConsoleColor.White, ConsoleColor.Black);
            }
        }
    }

    [Benchmark]
    public void Optimized_FillRect()
    {
        _buffer.FillRect(50, 40, 50, 20, '#', ConsoleColor.White, ConsoleColor.Black);
    }
}
