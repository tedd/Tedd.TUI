using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

public class ConsoleRenderer : IRenderer
{
    private readonly IConsole _console;
    private int _width;
    private int _height;

    // Double buffering state
    private Cell[]? _backBuffer;
    private int _backBufferWidth;
    private int _backBufferHeight;

    // Cursor tracking
    private int _consoleCursorX = -1;
    private int _consoleCursorY = -1;

    // Console color state tracking to minimize API calls
    private int _consoleCurrentFg = -1;
    private int _consoleCurrentBg = -1;

    public ConsoleRenderer() : this(new SystemConsoleWrapper())
    {
    }

    public ConsoleRenderer(IConsole console)
    {
        _console = console;
        _width = _console.WindowWidth;
        _height = _console.WindowHeight;
        _console.CursorVisible = false;
        _console.OutputEncoding = Encoding.UTF8;
    }

    /// <summary>
    /// Renders the virtual buffer to the console using double-buffering optimization.
    /// Time Complexity: O(W * H) - Iterates over every cell in the buffer.
    /// Space Complexity: O(W * H) - Maintains a backbuffer of the same size.
    /// </summary>
    public void Render(VirtualBuffer buffer)
    {
        // Check buffer dimensions
        int bufW = Math.Min(buffer.Width, Math.Min(_console.WindowWidth, _console.BufferWidth));
        int bufH = Math.Min(buffer.Height, Math.Min(_console.WindowHeight, _console.BufferHeight));

        // Reallocate backbuffer if needed
        if (_backBuffer == null || _backBufferWidth != bufW || _backBufferHeight != bufH)
        {
            _backBufferWidth = bufW;
            _backBufferHeight = bufH;
            _backBuffer = new Cell[bufW * bufH];
            // Initialize with invalid cells to force full redraw
            // Using a color that is unlikely to match default (e.g. -1 cast)
            Array.Fill(_backBuffer, new Cell('\0', (ConsoleColor)(-1), (ConsoleColor)(-1)));

            // Invalidate cursor tracking on resize/reset
            _consoleCursorX = -1;
            _consoleCursorY = -1;
        }

        // Reset color state tracking at start of frame as we don't know external state
        // (or we reset it at end of last frame)
        _consoleCurrentFg = -1;
        _consoleCurrentBg = -1;

        int lastFg = -1;
        int lastBg = -1;

        // Start of the pending write chunk
        int pendingX = -1;
        int pendingY = -1;

        var sb = new StringBuilder();

        for (int y = 0; y < bufH; y++)
        {
            for (int x = 0; x < bufW; x++)
            {
                var newCell = buffer.GetPixel(x, y);
                int idx = y * bufW + x; // Backbuffer index (row-major)

                // Skip if unchanged
                if (IsSame(newCell, _backBuffer[idx]))
                {
                    // If we were accumulating a chunk, flush it now because we hit a gap (unchanged cell)
                    if (sb.Length > 0)
                    {
                        FlushBuffer(sb, pendingX, pendingY, lastFg, lastBg);
                    }
                    continue;
                }

                // Update backbuffer
                _backBuffer[idx] = newCell;

                bool colorChanged = (int)newCell.Foreground != lastFg || (int)newCell.Background != lastBg;

                if (sb.Length > 0)
                {
                     // We have a pending chunk.
                     // Since we iterate sequentially, continuity is guaranteed (current x is prev x + 1).
                     // We just need to check color.
                     if (colorChanged)
                     {
                         // Flush current chunk
                         FlushBuffer(sb, pendingX, pendingY, lastFg, lastBg);

                         // Start new chunk
                         pendingX = x;
                         pendingY = y;
                         lastFg = (int)newCell.Foreground;
                         lastBg = (int)newCell.Background;
                         sb.Append(newCell.Character);
                     }
                     else
                     {
                         // Append to current chunk
                         sb.Append(newCell.Character);
                     }
                }
                else
                {
                    // Start new pending chunk
                    pendingX = x;
                    pendingY = y;
                    lastFg = (int)newCell.Foreground;
                    lastBg = (int)newCell.Background;
                    sb.Append(newCell.Character);
                }
            }

            // End of line flush
            if (sb.Length > 0)
            {
                FlushBuffer(sb, pendingX, pendingY, lastFg, lastBg);
            }
        }

        _console.ResetColor();
    }

    private void FlushBuffer(StringBuilder sb, int startX, int startY, int fg, int bg)
    {
        if (sb.Length == 0) return;

        // Optimization: Only move cursor if not already there
        if (_consoleCursorX != startX || _consoleCursorY != startY)
        {
             try
            {
                _console.SetCursorPosition(startX, startY);
                _consoleCursorX = startX;
                _consoleCursorY = startY;
            }
            catch (ArgumentOutOfRangeException)
            {
                sb.Clear();
                return;
            }
        }

        // Apply colors only if changed from current console state
        if (fg != -1 && fg != _consoleCurrentFg)
        {
            _console.ForegroundColor = (ConsoleColor)fg;
            _consoleCurrentFg = fg;
        }

        if (bg != -1 && bg != _consoleCurrentBg)
        {
            _console.BackgroundColor = (ConsoleColor)bg;
            _consoleCurrentBg = bg;
        }

        _console.Write(sb.ToString());

        // Update tracked cursor position
        _consoleCursorX += sb.Length;
        // _consoleCursorY stays same assuming no wrap/newline

        sb.Clear();
    }

    private bool IsSame(Cell a, Cell b)
    {
        return a.Character == b.Character && a.Foreground == b.Foreground && a.Background == b.Background;
    }
}
