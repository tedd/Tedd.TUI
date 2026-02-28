using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    // Rendering optimization
    private char[] _charBuffer = new char[1024];
    private int _charBufferPos = 0;

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

            // Resize char buffer if it's too small for a full row (unlikely but safe)
            if (_charBuffer.Length < bufW)
            {
                _charBuffer = new char[Math.Max(1024, bufW)];
            }
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

        ref Cell bufferRef = ref MemoryMarshal.GetReference(buffer.Cells);
        ref Cell backBufferRef = ref MemoryMarshal.GetArrayDataReference(_backBuffer);

        int bufferWidth = buffer.Width;

        for (int y = 0; y < bufH; y++)
        {
            int rowOffset = y * bufW;
            int sourceRowOffset = y * bufferWidth;

            for (int x = 0; x < bufW; x++)
            {
                int idx = rowOffset + x;

                ref Cell newCell = ref Unsafe.Add(ref bufferRef, sourceRowOffset + x);
                ref Cell backCell = ref Unsafe.Add(ref backBufferRef, idx);

                // Inline IsSame comparison for speed
                if (newCell.Character == backCell.Character &&
                    newCell.Foreground == backCell.Foreground &&
                    newCell.Background == backCell.Background)
                {
                    // If we were accumulating a chunk, flush it now because we hit a gap (unchanged cell)
                    if (_charBufferPos > 0)
                    {
                        FlushBuffer(pendingX, pendingY, lastFg, lastBg);
                    }
                    continue;
                }

                // Update backbuffer
                backCell = newCell;

                int newFg = (int)newCell.Foreground;
                int newBg = (int)newCell.Background;
                bool colorChanged = newFg != lastFg || newBg != lastBg;

                if (_charBufferPos > 0)
                {
                     // We have a pending chunk.
                     if (colorChanged)
                     {
                         // Flush current chunk
                         FlushBuffer(pendingX, pendingY, lastFg, lastBg);

                         // Start new chunk
                         pendingX = x;
                         pendingY = y;
                         lastFg = newFg;
                         lastBg = newBg;

                         AppendChar(newCell.Character);
                     }
                     else
                     {
                         // Append to current chunk
                         AppendChar(newCell.Character);
                     }
                }
                else
                {
                    // Start new pending chunk
                    pendingX = x;
                    pendingY = y;
                    lastFg = newFg;
                    lastBg = newBg;

                    AppendChar(newCell.Character);
                }
            }

            // End of line flush
            if (_charBufferPos > 0)
            {
                FlushBuffer(pendingX, pendingY, lastFg, lastBg);
            }
        }

        _console.ResetColor();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendChar(char c)
    {
        if (_charBufferPos >= _charBuffer.Length)
        {
            var newBuffer = new char[_charBuffer.Length * 2];
            _charBuffer.CopyTo(newBuffer, 0);
            _charBuffer = newBuffer;
        }
        _charBuffer[_charBufferPos++] = c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushBuffer(int startX, int startY, int fg, int bg)
    {
        if (_charBufferPos == 0) return;

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
                _charBufferPos = 0;
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

        _console.Write(new ReadOnlySpan<char>(_charBuffer, 0, _charBufferPos));

        // Update tracked cursor position
        _consoleCursorX += _charBufferPos;
        // _consoleCursorY stays same assuming no wrap/newline

        _charBufferPos = 0;
    }
}
