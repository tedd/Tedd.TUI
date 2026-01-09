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

        int bufH = Math.Min(buffer.Height, System.Console.WindowHeight);
        int bufW = Math.Min(buffer.Width, System.Console.WindowWidth);

        System.Console.SetCursorPosition(0, 0);

        var sb = new StringBuilder();

        for (int y = 0; y < bufH; y++)
        {
            // Move cursor to start of line if we are not wrapping perfectly (which we usually aren't guaranteed)
            // Actually, writing a full line moves cursor to next line usually, but lets be safe.
            System.Console.SetCursorPosition(0, y);

            for (int x = 0; x < bufW; x++)
            {
                var cell = buffer.GetPixel(x, y);

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

                System.Console.Write(cell.Character);
            }
        }

        System.Console.ResetColor();
    }
}
