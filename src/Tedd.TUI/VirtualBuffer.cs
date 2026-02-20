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
// Constraints/Invariants:
// - Buffer bounds logic `y * Width + x` must strictly constrain array indices correctly.
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
        if (_clipStack.TryPeek(out var current))
        {
            // Intersect new clip with current clip
            int x = Math.Max(current.X, clip.X);
            int y = Math.Max(current.Y, clip.Y);
            int r = Math.Min(current.X + current.Width, clip.X + clip.Width);
            int b = Math.Min(current.Y + current.Height, clip.Y + clip.Height);
            
            _clipStack.Push(new Rect(x, y, Math.Max(0, r - x), Math.Max(0, b - y)));
        }
        else
        {
            _clipStack.Push(clip);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.Pop();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rect GetClip()
    {
        return _clipStack.TryPeek(out var rect) ? rect : new Rect(0, 0, Width, Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _clipStack.Clear();
        _buffer.AsSpan().Fill(new Cell(' ', ConsoleColor.White, ConsoleColor.Black));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, char c, ConsoleColor fg, ConsoleColor bg)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        if (_clipStack.TryPeek(out var clip))
        {
            if (x < clip.X || x >= clip.X + clip.Width || y < clip.Y || y >= clip.Y + clip.Height)
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
