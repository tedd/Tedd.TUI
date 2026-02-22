using Xunit.Abstractions;
// using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Tests;

public class ConsoleRendererBenchmark
{
    /*
    private readonly ITestOutputHelper _output;

    public ConsoleRendererBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_Render_Updates()
    {
        var mockConsole = new MockConsole();
        mockConsole.WindowWidth = 80;
        mockConsole.WindowHeight = 25;
        mockConsole.BufferWidth = 80;
        mockConsole.BufferHeight = 25;

        var renderer = new Tedd.TUI.Platform.Console.ConsoleRenderer(mockConsole);
        var buffer = new VirtualBuffer(80, 25);

        // Fill buffer with something
        for (int y = 0; y < 25; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                buffer.SetPixel(x, y, '.', ConsoleColor.Gray, ConsoleColor.Black);
            }
        }

        // 1. Initial Render (Full Redraw expected)
        mockConsole.ResetStats();
        renderer.Render(buffer);
        long initialWrites = mockConsole.WriteCount;
        long initialMoves = mockConsole.SetCursorPositionCount;

        _output.WriteLine($"Initial Render: {initialWrites} writes, {initialMoves} moves");

        // 2. Small Update (One character change)
        buffer.SetPixel(10, 10, 'X', ConsoleColor.Red, ConsoleColor.Black);

        mockConsole.ResetStats();
        renderer.Render(buffer);
        long updateWrites = mockConsole.WriteCount;
        long updateMoves = mockConsole.SetCursorPositionCount;

        _output.WriteLine($"Partial Update (1 char): {updateWrites} writes, {updateMoves} moves");

        // 3. No Update (No change)
        mockConsole.ResetStats();
        renderer.Render(buffer);
        long noUpdateWrites = mockConsole.WriteCount;
        long noUpdateMoves = mockConsole.SetCursorPositionCount;

        _output.WriteLine($"No Update: {noUpdateWrites} writes, {noUpdateMoves} moves");
    }
    */
}
