using System;
using System.Text;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Benchmarks.Legacy;

public class ConsoleRendererLegacy : IRenderer
{
    private readonly IConsole _console;
    private int _width;
    private int _height;

    public ConsoleRendererLegacy() : this(new SystemConsoleWrapper())
    {
    }

    public ConsoleRendererLegacy(IConsole console)
    {
        _console = console;
        _width = _console.WindowWidth;
        _height = _console.WindowHeight;
        _console.CursorVisible = false;
        _console.OutputEncoding = Encoding.UTF8;
    }

    public void Render(VirtualBuffer buffer)
    {
        // Simple optimization: only draw if buffer changed?
        // For now, full redraw or line-by-line.
        // To avoid flicker, we should buffer writes, but Console.Write is buffered usually.

        // We will just draw character by character for now, optimizing state changes.

        int lastFg = -1;
        int lastBg = -1;

        // Use _console properties
        int bufH = Math.Min(buffer.Height, Math.Min(_console.WindowHeight, _console.BufferHeight));
        int bufW = Math.Min(buffer.Width, Math.Min(_console.WindowWidth, _console.BufferWidth));

        _console.SetCursorPosition(0, 0);

        var sb = new StringBuilder();

        for (int y = 0; y < bufH; y++)
        {
            // Runtime check: BufferHeight might have changed since loop started (e.g. async resize)
            if (y >= _console.BufferHeight) break;

            try
            {
                _console.SetCursorPosition(0, y);
            }
            catch (ArgumentOutOfRangeException)
            {
                // If cursor position is out of bounds, we can't draw this line.
                // Stop rendering this frame to avoid further errors.
                break;
            }

            for (int x = 0; x < bufW; x++)
            {
                var cell = buffer.GetPixel(x, y);

                // Flush buffer if color changes
                if ((int)cell.Foreground != lastFg || (int)cell.Background != lastBg)
                {
                    if (sb.Length > 0)
                    {
                        _console.Write(sb.ToString());
                        sb.Clear();
                    }

                    if ((int)cell.Foreground != lastFg)
                    {
                        _console.ForegroundColor = cell.Foreground;
                        lastFg = (int)cell.Foreground;
                    }
                    if ((int)cell.Background != lastBg)
                    {
                        _console.BackgroundColor = cell.Background;
                        lastBg = (int)cell.Background;
                    }
                }

                sb.Append(cell.Character);
            }

            // Flush end of line
            if (sb.Length > 0)
            {
                _console.Write(sb.ToString());
                sb.Clear();
            }
        }

        _console.ResetColor();
    }
}
