namespace Tedd.TUI;

public interface IRenderer
{
    void Render(VirtualBuffer buffer);
}

public class ConsoleRenderer : IRenderer
{
    private Cell[,]? _previousBuffer;
    private int _prevWidth;
    private int _prevHeight;

    public void Render(VirtualBuffer buffer)
    {
        Console.CursorVisible = false;

        // Check if buffer size changed or not initialized
        if (_previousBuffer == null || buffer.Width != _prevWidth || buffer.Height != _prevHeight)
        {
            Console.Clear();
            _prevWidth = buffer.Width;
            _prevHeight = buffer.Height;
            _previousBuffer = new Cell[_prevHeight, _prevWidth];
        }

        int width = buffer.Width;
        int height = buffer.Height;

        // Track cursor position to minimize jumps
        int expectedCursorX = -1;
        int expectedCursorY = -1;

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                var cell = buffer.GetPixel(x, y);
                var prev = _previousBuffer![y, x];

                // Skip if unchanged
                if (AreSame(cell, prev))
                {
                    x++;
                    continue;
                }

                // Move cursor if not already there
                if (x != expectedCursorX || y != expectedCursorY)
                {
                    Console.SetCursorPosition(x, y);
                }

                // Start batch with current colors
                Console.ForegroundColor = cell.Foreground;
                Console.BackgroundColor = cell.Background;

                var batchFg = cell.Foreground;
                var batchBg = cell.Background;

                // Write run of characters
                while (x < width)
                {
                    var current = buffer.GetPixel(x, y);

                    // Stop batch if color changes or we reached content that matches previous frame
                    if (current.Foreground != batchFg || current.Background != batchBg)
                        break;

                    if (AreSame(current, _previousBuffer[y, x]))
                        break;

                    Console.Write(current.Character);
                    _previousBuffer[y, x] = current;
                    x++;
                }

                // Update expected cursor position after write
                expectedCursorX = x;
                expectedCursorY = y;
            }
        }

        Console.ResetColor();
    }

    private bool AreSame(Cell a, Cell b)
    {
        return a.Character == b.Character &&
               a.Foreground == b.Foreground &&
               a.Background == b.Background;
    }
}
