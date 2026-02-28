using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;
using Tedd.TUI.Archive;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class ConsoleRendererBenchmarks
{
    private VirtualBuffer _buffer;
    private MockConsole _console;
    private ConsoleRenderer _modernRenderer;
    private ConsoleRendererArchive _legacyRenderer;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new VirtualBuffer(120, 30);
        _console = new MockConsole();
        _console.WindowWidth = 120;
        _console.WindowHeight = 30;
        _console.BufferWidth = 120;
        _console.BufferHeight = 30;

        _modernRenderer = new ConsoleRenderer(_console);
        _legacyRenderer = new ConsoleRendererArchive(_console);

        // Populate buffer to simulate typical UI
        for (int y = 0; y < 30; y++)
        {
            for (int x = 0; x < 120; x++)
            {
                _buffer.SetPixel(x, y, (char)('A' + (x % 26)), ConsoleColor.White, ConsoleColor.Black);
            }
        }

        // Initial render to fill backbuffers
        _modernRenderer.Render(_buffer);
        _legacyRenderer.Render(_buffer);

        // Modify half cells to trigger delta rendering
        for (int i = 0; i < 60; i++)
        {
            for(int j = 0; j < 30; j++)
            {
                _buffer.SetPixel(i, j, 'X', ConsoleColor.Red, ConsoleColor.Yellow);
            }
        }
    }

    [Benchmark(Baseline = true)]
    public void LegacyRender()
    {
        _legacyRenderer.Render(_buffer);
    }

    [Benchmark]
    public void ModernRender()
    {
        _modernRenderer.Render(_buffer);
    }
}
