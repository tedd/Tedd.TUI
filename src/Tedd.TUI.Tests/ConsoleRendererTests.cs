using Xunit;
using Xunit.Abstractions;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;
using System;
using System.Text;

namespace Tedd.TUI.Tests;

public class ConsoleRendererTests
{
    private readonly ITestOutputHelper _output;

    public ConsoleRendererTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Render_Optimizes_Updates()
    {
        var mockConsole = new MockConsole();
        mockConsole.WindowWidth = 80;
        mockConsole.WindowHeight = 25;
        mockConsole.BufferWidth = 80;
        mockConsole.BufferHeight = 25;

        var renderer = new ConsoleRenderer(mockConsole);
        var buffer = new VirtualBuffer(80, 25);

        // Fill buffer
        for (int y = 0; y < 25; y++)
        {
            buffer.DrawString(0, y, new string('.', 80), ConsoleColor.Gray, ConsoleColor.Black);
        }

        // 1. Initial Render (Full Redraw)
        // Note: Render(buffer) triggers full redraw on first run or resize
        mockConsole.ResetStats();
        renderer.Render(buffer);
        long initialWrites = mockConsole.WriteCount;
        _output.WriteLine($"Initial writes: {initialWrites}");

        Assert.True(initialWrites > 0);

        // 2. No Update
        mockConsole.ResetStats();
        renderer.Render(buffer);
        long noUpdateWrites = mockConsole.WriteCount;
        _output.WriteLine($"No update writes: {noUpdateWrites}");

        Assert.Equal(0, noUpdateWrites); // Expect exactly 0 writes

        // 3. Small Update (1 char)
        buffer.SetPixel(10, 10, 'X', ConsoleColor.Red, ConsoleColor.Black);

        mockConsole.ResetStats();
        renderer.Render(buffer);
        long updateWrites = mockConsole.WriteCount;
        _output.WriteLine($"Update writes: {updateWrites}");

        Assert.True(updateWrites < initialWrites);
        Assert.Equal(1, updateWrites); // 1 chunk written
    }
}
