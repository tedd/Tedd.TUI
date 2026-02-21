using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Tedd.TUI;

public struct Cell
{
    public char Character;
    public ConsoleColor Foreground;
    public ConsoleColor Background;

    public Cell(char character, ConsoleColor foreground, ConsoleColor background)
    {
        Character = character;
        Foreground = foreground;
        Background = background;
    }
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
        _buffer.AsSpan().Fill(new Cell(' ', ConsoleColor.White, ConsoleColor.Black));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, char c, ConsoleColor fg, ConsoleColor bg)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cell GetPixel(int x, int y)
    {
        if ((uint)x < (uint)Width && (uint)y < (uint)Height)
        {
            return _buffer[y * Width + x];
        }
        return new Cell(' ', ConsoleColor.White, ConsoleColor.Black);
    }
}
