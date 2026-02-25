using BenchmarkDotNet.Attributes;
using Tedd.TUI.Benchmarks.Legacy;
using Tedd.TUI.Platform.Console;
using System;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class ConsoleRendererBenchmark
{
    private VirtualBuffer _buffer;
    private MockConsole _mockConsole;
    private ConsoleRenderer _renderer;
    private ConsoleRendererLegacy _rendererLegacy;

    [GlobalSetup]
    public void Setup()
    {
        _mockConsole = new MockConsole();
        _buffer = new VirtualBuffer(80, 25);

        // Fill buffer with some content
        for (int y = 0; y < 25; y++)
        {
            _buffer.DrawString(0, y, $"Line {y} content...", ConsoleColor.White, ConsoleColor.Black);
        }

        _renderer = new ConsoleRenderer(_mockConsole);
        _rendererLegacy = new ConsoleRendererLegacy(_mockConsole);
    }

    [Benchmark(Baseline = true)]
    public void Legacy_Render_Full()
    {
        _mockConsole.ResetStats();
        _rendererLegacy.Render(_buffer);
    }

    [Benchmark]
    public void New_Render_Full()
    {
        // To benchmark full render, we need to force a full redraw.
        // We can do this by using a fresh renderer instance which has no backbuffer state.
        // This includes allocation cost of the renderer, but that's minimal compared to I/O usually.
        // However, in this microbenchmark with MockConsole, allocation might dominate.
        // A better way is to pretend we have a fresh renderer.
        var renderer = new ConsoleRenderer(_mockConsole);
        renderer.Render(_buffer);
    }

    [Benchmark]
    public void Legacy_Render_NoChange()
    {
        // Pre-render to set state (irrelevant for legacy but good for consistency)
        _rendererLegacy.Render(_buffer);
        _mockConsole.ResetStats();

        _rendererLegacy.Render(_buffer);
    }

    [Benchmark]
    public void New_Render_NoChange()
    {
        // Pre-render to populate backbuffer
        _renderer.Render(_buffer);
        _mockConsole.ResetStats();

        // This should be very fast (diff is zero)
        _renderer.Render(_buffer);
    }

    [Benchmark]
    public void Legacy_Render_SmallChange()
    {
        // Pre-render
        _rendererLegacy.Render(_buffer);

        // Change one pixel
        _buffer.SetPixel(10, 10, 'X', ConsoleColor.Red, ConsoleColor.Black);

        _mockConsole.ResetStats();
        _rendererLegacy.Render(_buffer);

        // Revert
        _buffer.SetPixel(10, 10, '.', ConsoleColor.White, ConsoleColor.Black);
    }

    [Benchmark]
    public void New_Render_SmallChange()
    {
        // Pre-render
        _renderer.Render(_buffer);

        // Change one pixel
        _buffer.SetPixel(10, 10, 'X', ConsoleColor.Red, ConsoleColor.Black);

        _mockConsole.ResetStats();
        _renderer.Render(_buffer);

        // Revert
        _buffer.SetPixel(10, 10, '.', ConsoleColor.White, ConsoleColor.Black);
    }
}
