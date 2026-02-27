using BenchmarkDotNet.Attributes;
using Tedd.TUI.Benchmarks.Legacy;
using Tedd.TUI.Platform.Console;
using Tedd.TUI.Archive;
using System;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class ConsoleRendererBenchmark
{
    private VirtualBuffer _buffer;
    private MockConsole _mockConsole;
    private ConsoleRenderer _renderer; // New optimized renderer
    private ConsoleRendererArchive _rendererArchive; // The one we just archived (previous version)

    // We also keep the legacy one from before just in case, but our main comparison is Archive (Baseline) vs Optimized.
    // Actually, let's replace "Legacy" with "Archive" as the baseline, as that represents the state before this change.

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
        _rendererArchive = new ConsoleRendererArchive(_mockConsole);
    }

    [Benchmark(Baseline = true)]
    public void Archive_Render_Full()
    {
        // Force full redraw by new instance
        var renderer = new ConsoleRendererArchive(_mockConsole);
        renderer.Render(_buffer);
    }

    [Benchmark]
    public void Optimized_Render_Full()
    {
        // Force full redraw by new instance
        var renderer = new ConsoleRenderer(_mockConsole);
        renderer.Render(_buffer);
    }

    [Benchmark]
    public void Archive_Render_NoChange()
    {
        // Pre-render
        _rendererArchive.Render(_buffer);
        _mockConsole.ResetStats();
        _rendererArchive.Render(_buffer);
    }

    [Benchmark]
    public void Optimized_Render_NoChange()
    {
        // Pre-render
        _renderer.Render(_buffer);
        _mockConsole.ResetStats();
        _renderer.Render(_buffer);
    }

    [Benchmark]
    public void Archive_Render_SmallChange()
    {
        // Pre-render
        _rendererArchive.Render(_buffer);

        // Change one pixel
        _buffer.SetPixel(10, 10, 'X', ConsoleColor.Red, ConsoleColor.Black);

        _mockConsole.ResetStats();
        _rendererArchive.Render(_buffer);

        // Revert
        _buffer.SetPixel(10, 10, '.', ConsoleColor.White, ConsoleColor.Black);
    }

    [Benchmark]
    public void Optimized_Render_SmallChange()
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
