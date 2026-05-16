using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Tedd.TUI;

/// <summary>
/// A single character cell in the rendering grid. Stores the glyph plus a 32-bit RGBA
/// foreground and background <see cref="TuiColor"/>. Backwards-compatible
/// <see cref="ConsoleColor"/> overloads convert implicitly via <see cref="TuiColor"/>.
/// </summary>
public struct Cell : IEquatable<Cell>
{
    public char Character;
    public TuiColor Foreground;
    public TuiColor Background;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cell(char character, TuiColor foreground, TuiColor background)
    {
        Character = character;
        Foreground = foreground;
        Background = background;
    }

    /// <summary>
    /// Convenience constructor preserving the original <see cref="ConsoleColor"/>
    /// signature so callers that haven't migrated yet keep compiling.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cell(char character, ConsoleColor foreground, ConsoleColor background)
        : this(character, TuiColor.FromConsole(foreground), TuiColor.FromConsole(background))
    {
    }

    /// <summary>
    /// Returns true when both cells are visually identical. Compares the glyph and the
    /// packed color words directly rather than enum-by-enum so the diff loop costs the
    /// same as the old <see cref="ConsoleColor"/> path.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Cell other) =>
        Character == other.Character &&
        Foreground.Packed == other.Foreground.Packed &&
        Background.Packed == other.Background.Packed;

    public override bool Equals(object? obj) => obj is Cell c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(Character, Foreground.Packed, Background.Packed);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Cell a, Cell b) => a.Equals(b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
}

// Intent: Optimize VirtualBuffer using linear arrays and Span for better runtime performance
// Why:
// - Removing multidimensional arrays eliminates consecutive bounds checking overhead.
// - Flattening array layout maps efficiently to memory for predictable access.
// - Inlining hot paths like SetPixel/GetPixel removes call frame overhead during rendering.
// - Caching the current clip rectangle avoids Stack<T>.TryPeek overhead on every pixel set.
// Constraints/Invariants:
// - Buffer bounds logic `y * Width + x` must strictly constrain array indices correctly.
// - _currentClip must always represent the top of the clip stack, or the full buffer if stack is empty.
// Failure modes:
// - UI tearing or IndexOutOfRangeException if x/y bound validations fail.
// Verification:
// - Verify screen draw remains fully correct visibly while performance profiler shows less overhead in TUI layout phase.
public class VirtualBuffer
{
    private readonly Cell[] _buffer;
    public int Width { get; }
    public int Height { get; }

    // Direct access to buffer for optimized rendering
    public ReadOnlySpan<Cell> Cells => _buffer;

    /// <summary>
    /// Optional bitmap-graphic overlay channel. When non-null, the surface hosting this buffer
    /// supports compositing bitmaps over the character grid; graphics-aware controls (e.g.
    /// <see cref="Tedd.TUI.Markdown.Image"/>) append <see cref="GraphicPlacement"/> entries here
    /// during render, and the surface renderer draws them after the text cells. When null the
    /// surface is text-only and controls fall back to character-based rendering.
    /// </summary>
    public IList<GraphicPlacement>? Graphics { get; set; }

    private Stack<Rect> _clipStack = new Stack<Rect>();
    private Rect _currentClip;
    private bool _isClipped;

    public VirtualBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _buffer = new Cell[width * height];
        Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushClip(Rect clip)
    {
        if (_clipStack.Count > 0)
        {
            // Intersect new clip with current clip
            int x = Math.Max(_currentClip.X, clip.X);
            int y = Math.Max(_currentClip.Y, clip.Y);
            int r = Math.Min(_currentClip.X + _currentClip.Width, clip.X + clip.Width);
            int b = Math.Min(_currentClip.Y + _currentClip.Height, clip.Y + clip.Height);

            var newClip = new Rect(x, y, Math.Max(0, r - x), Math.Max(0, b - y));
            _clipStack.Push(newClip);
            _currentClip = newClip;
        }
        else
        {
            _clipStack.Push(clip);
            _currentClip = clip;
        }
        _isClipped = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.Pop();
            if (_clipStack.Count > 0)
            {
                _currentClip = _clipStack.Peek();
                _isClipped = true;
            }
            else
            {
                _currentClip = new Rect(0, 0, Width, Height);
                _isClipped = false;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rect GetClip()
    {
        return _currentClip;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _clipStack.Clear();
        _currentClip = new Rect(0, 0, Width, Height);
        _isClipped = false;
        _buffer.AsSpan().Fill(new Cell(' ', TuiColor.White, TuiColor.Black));
    }

    /// <summary>
    /// Clears the buffer to the given background color. Useful for layer buffers that
    /// want a fully transparent default so blank cells don't paint over lower layers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(TuiColor background)
    {
        _clipStack.Clear();
        _currentClip = new Rect(0, 0, Width, Height);
        _isClipped = false;
        _buffer.AsSpan().Fill(new Cell(' ', TuiColor.White, background));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, char c, TuiColor fg, TuiColor bg)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        // Optimization: Check _isClipped first to skip bounds check if no clip is active
        if (_isClipped)
        {
            if (x < _currentClip.X || x >= _currentClip.X + _currentClip.Width || y < _currentClip.Y || y >= _currentClip.Y + _currentClip.Height)
            {
                return;
            }
        }

        _buffer[y * Width + x] = new Cell(c, fg, bg);
    }

    // ConsoleColor compatibility trampoline.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, char c, ConsoleColor fg, ConsoleColor bg) =>
        SetPixel(x, y, c, TuiColor.FromConsole(fg), TuiColor.FromConsole(bg));

    /// <summary>
    /// Alpha-aware pixel write: composes (fg, bg) over the existing cell using
    /// Porter-Duff "over". When either incoming channel is opaque the corresponding
    /// channel is written verbatim; when the glyph is a space the original glyph stays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BlendPixel(int x, int y, char c, TuiColor fg, TuiColor bg)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        if (_isClipped)
        {
            if (x < _currentClip.X || x >= _currentClip.X + _currentClip.Width || y < _currentClip.Y || y >= _currentClip.Y + _currentClip.Height) return;
        }

        int idx = y * Width + x;
        ref Cell dst = ref _buffer[idx];

        TuiColor newBg = bg.IsOpaque ? bg : bg.Blend(dst.Background);
        TuiColor newFg;
        char newChar;

        if (c == ' ' || c == '\0')
        {
            // Pure background paint: keep the existing glyph and tint the foreground.
            newChar = dst.Character;
            newFg = fg.IsTransparent ? dst.Foreground : fg.Blend(dst.Foreground);
        }
        else
        {
            newChar = c;
            // The new glyph is rendered against the freshly composited background.
            newFg = fg.IsOpaque ? fg : fg.Blend(newBg);
        }

        dst.Character = newChar;
        dst.Foreground = newFg;
        dst.Background = newBg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cell GetPixel(int x, int y)
    {
        if ((uint)x < (uint)Width && (uint)y < (uint)Height)
        {
            return _buffer[y * Width + x];
        }
        return new Cell(' ', TuiColor.White, TuiColor.Black);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawString(int x, int y, string text, TuiColor fg, TuiColor bg) =>
        DrawString(x, y, text.AsSpan(), fg, bg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawString(int x, int y, string text, ConsoleColor fg, ConsoleColor bg) =>
        DrawString(x, y, text.AsSpan(), TuiColor.FromConsole(fg), TuiColor.FromConsole(bg));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawString(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg) =>
        DrawString(x, y, text, TuiColor.FromConsole(fg), TuiColor.FromConsole(bg));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawString(int x, int y, ReadOnlySpan<char> text, TuiColor fg, TuiColor bg)
    {
        if ((uint)y >= (uint)Height) return;

        int startX = x;
        int endX = x + text.Length;

        if (_isClipped)
        {
            if (y < _currentClip.Y || y >= _currentClip.Y + _currentClip.Height) return;

            if (startX < _currentClip.X)
            {
                int diff = _currentClip.X - startX;
                if (diff >= text.Length) return;
                text = text.Slice(diff);
                startX = _currentClip.X;
            }

            int clipRight = _currentClip.X + _currentClip.Width;
            if (endX > clipRight)
            {
                int visibleLen = clipRight - startX;
                if (visibleLen <= 0) return;
                if (visibleLen < text.Length)
                    text = text.Slice(0, visibleLen);
            }
        }
        else
        {
            if (startX < 0)
            {
                int diff = -startX;
                if (diff >= text.Length) return;
                text = text.Slice(diff);
                startX = 0;
            }
            if (startX + text.Length > Width)
            {
                int visibleLen = Width - startX;
                if (visibleLen <= 0) return;
                text = text.Slice(0, visibleLen);
            }
        }

        int bufferIdx = y * Width + startX;
        for (int i = 0; i < text.Length; i++)
        {
            _buffer[bufferIdx + i] = new Cell(text[i], fg, bg);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawHLine(int x, int y, int length, char c, TuiColor fg, TuiColor bg)
    {
        if ((uint)y >= (uint)Height) return;

        int startX = x;
        int endX = x + length;

        if (_isClipped)
        {
            if (y < _currentClip.Y || y >= _currentClip.Y + _currentClip.Height) return;
            startX = Math.Max(startX, _currentClip.X);
            endX = Math.Min(endX, _currentClip.X + _currentClip.Width);
        }
        else
        {
            startX = Math.Max(startX, 0);
            endX = Math.Min(endX, Width);
        }

        if (endX <= startX) return;

        int len = endX - startX;
        int bufferIdx = y * Width + startX;

        _buffer.AsSpan(bufferIdx, len).Fill(new Cell(c, fg, bg));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawHLine(int x, int y, int length, char c, ConsoleColor fg, ConsoleColor bg) =>
        DrawHLine(x, y, length, c, TuiColor.FromConsole(fg), TuiColor.FromConsole(bg));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawVLine(int x, int y, int length, char c, TuiColor fg, TuiColor bg)
    {
        if ((uint)x >= (uint)Width) return;

        int startY = y;
        int endY = y + length;

        if (_isClipped)
        {
            if (x < _currentClip.X || x >= _currentClip.X + _currentClip.Width) return;
            startY = Math.Max(startY, _currentClip.Y);
            endY = Math.Min(endY, _currentClip.Y + _currentClip.Height);
        }
        else
        {
            startY = Math.Max(startY, 0);
            endY = Math.Min(endY, Height);
        }

        if (endY <= startY) return;

        var cell = new Cell(c, fg, bg);
        int stride = Width;
        int bufferIdx = startY * stride + x;

        for (int i = startY; i < endY; i++)
        {
            _buffer[bufferIdx] = cell;
            bufferIdx += stride;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawVLine(int x, int y, int length, char c, ConsoleColor fg, ConsoleColor bg) =>
        DrawVLine(x, y, length, c, TuiColor.FromConsole(fg), TuiColor.FromConsole(bg));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillRect(int x, int y, int width, int height, char c, TuiColor fg, TuiColor bg)
    {
        int startX = x;
        int startY = y;
        int endX = x + width;
        int endY = y + height;

        if (_isClipped)
        {
            startX = Math.Max(startX, _currentClip.X);
            startY = Math.Max(startY, _currentClip.Y);
            endX = Math.Min(endX, _currentClip.X + _currentClip.Width);
            endY = Math.Min(endY, _currentClip.Y + _currentClip.Height);
        }
        else
        {
            startX = Math.Max(startX, 0);
            startY = Math.Max(startY, 0);
            endX = Math.Min(endX, Width);
            endY = Math.Min(endY, Height);
        }

        if (endX <= startX || endY <= startY) return;

        int rowWidth = endX - startX;
        var cell = new Cell(c, fg, bg);

        for (int row = startY; row < endY; row++)
        {
            int idx = row * Width + startX;
            _buffer.AsSpan(idx, rowWidth).Fill(cell);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillRect(int x, int y, int width, int height, char c, ConsoleColor fg, ConsoleColor bg) =>
        FillRect(x, y, width, height, c, TuiColor.FromConsole(fg), TuiColor.FromConsole(bg));
}
