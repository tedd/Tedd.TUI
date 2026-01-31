using System;
using System.Text;

namespace Tedd.TUI.Platform.Console;

public class ConsoleRenderer : IRenderer
{
    private int _width;
    private int _height;

    public ConsoleRenderer()
    {
        _width = System.Console.WindowWidth;
        _height = System.Console.WindowHeight;
        System.Console.CursorVisible = false;
        System.Console.OutputEncoding = Encoding.UTF8;
    }

    public void Render(VirtualBuffer buffer)
    {
        // Simple optimization: only draw if buffer changed?
        // For now, full redraw or line-by-line.
        // To avoid flicker, we should buffer writes, but Console.Write is buffered usually.

        // We will just draw character by character for now, optimizing state changes.

        int lastFg = -1;
        int lastBg = -1;

        int bufH = Math.Min(buffer.Height, Math.Min(System.Console.WindowHeight, System.Console.BufferHeight));
        int bufW = Math.Min(buffer.Width, Math.Min(System.Console.WindowWidth, System.Console.BufferWidth));

        System.Console.SetCursorPosition(0, 0);

        var sb = new StringBuilder();

        for (int y = 0; y < bufH; y++)
        {
            // Runtime check: BufferHeight might have changed since loop started (e.g. async resize)
            if (y >= System.Console.BufferHeight) break;

            try
            {
                System.Console.SetCursorPosition(0, y);
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
                        System.Console.Write(sb.ToString());
                        sb.Clear();
                    }

                    if ((int)cell.Foreground != lastFg)
                    {
                        System.Console.ForegroundColor = cell.Foreground;
                        lastFg = (int)cell.Foreground;
                    }
                    if ((int)cell.Background != lastBg)
                    {
                        System.Console.BackgroundColor = cell.Background;
                        lastBg = (int)cell.Background;
                    }
                }

                sb.Append(cell.Character);
            }

            // Flush end of line
            if (sb.Length > 0)
            {
                System.Console.Write(sb.ToString());
                sb.Clear();
            }
        }

        System.Console.ResetColor();
    }
}
