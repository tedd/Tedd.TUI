using System;
using System.Text;

namespace Tedd.TUI.Platform.Console;

public class ConsoleRenderer : IRenderer
{
    private readonly IConsole _console;
    private int _width;
    private int _height;

    private Cell[,]? _backBuffer;
    private int _backBufferWidth;
    private int _backBufferHeight;

    public ConsoleRenderer(IConsole? console = null)
    {
        _console = console ?? new SystemConsoleWrapper();
        _width = _console.WindowWidth;
        _height = _console.WindowHeight;
        _console.CursorVisible = false;
        _console.OutputEncoding = Encoding.UTF8;
    }

    public void Render(VirtualBuffer buffer)
    {
        int bufH = Math.Min(buffer.Height, Math.Min(_console.WindowHeight, _console.BufferHeight));
        int bufW = Math.Min(buffer.Width, Math.Min(_console.WindowWidth, _console.BufferWidth));

        // Check if backbuffer needs resize or initialization
        if (_backBuffer == null || _backBufferWidth != bufW || _backBufferHeight != bufH)
        {
            _backBuffer = new Cell[bufH, bufW];
            _backBufferWidth = bufW;
            _backBufferHeight = bufH;

            // Initialize with a value that is unlikely to match any real cell to force redraw
            // default(Cell) has Character = '\0', which usually differs from ' ' or other content.
        }

        // We track the state of the console to minimize API calls
        int cursorX = -1;
        int cursorY = -1;

        // Initialize with invalid color to force set on first write
        ConsoleColor lastFg = (ConsoleColor)(-1);
        ConsoleColor lastBg = (ConsoleColor)(-1);

        try
        {
            // Optional: Hide cursor during render if not already hidden?
            // Constructor sets it, but maybe it changed.
            // _console.CursorVisible = false;
        }
        catch { }

        for (int y = 0; y < bufH; y++)
        {
            // Runtime check for resizing
            if (y >= _console.BufferHeight) break;

            for (int x = 0; x < bufW; x++)
            {
                var cell = buffer.GetPixel(x, y);
                ref var backCell = ref _backBuffer[y, x];

                // Diffing: Only write if changed
                if (cell.Character != backCell.Character ||
                    cell.Foreground != backCell.Foreground ||
                    cell.Background != backCell.Background)
                {
                    // Move cursor if needed
                    if (x != cursorX || y != cursorY)
                    {
                        try
                        {
                            _console.SetCursorPosition(x, y);
                            cursorX = x;
                            cursorY = y;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // If we can't move to this position, we can't write this cell.
                            continue;
                        }
                    }

                    // Update colors if needed
                    if (cell.Foreground != lastFg)
                    {
                        _console.ForegroundColor = cell.Foreground;
                        lastFg = cell.Foreground;
                    }
                    if (cell.Background != lastBg)
                    {
                        _console.BackgroundColor = cell.Background;
                        lastBg = cell.Background;
                    }

                    // Write character
                    _console.Write(cell.Character);

                    // Update state
                    backCell = cell;
                    cursorX++; // Cursor moves 1 step to the right
                }
            }
        }

        // Reset colors at end
        _console.ResetColor();
    }
}
