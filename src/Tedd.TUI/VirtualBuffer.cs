using System;
using System.Collections.Generic;

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

public class VirtualBuffer
{
    private readonly Cell[,] _buffer;
    public int Width { get; }
    public int Height { get; }

    private Stack<Rect> _clipStack = new Stack<Rect>();

    public VirtualBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _buffer = new Cell[height, width];
        Clear();
    }

    public void PushClip(Rect clip)
    {
        if (_clipStack.Count > 0)
        {
            var current = _clipStack.Peek();
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

    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.Pop();
        }
    }

    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _buffer[y, x] = new Cell(' ', ConsoleColor.White, ConsoleColor.Black);
            }
        }
    }

    public void SetPixel(int x, int y, char c, ConsoleColor fg, ConsoleColor bg)
    {
        if (_clipStack.Count > 0)
        {
            var clip = _clipStack.Peek();
            if (x < clip.X || x >= clip.X + clip.Width || y < clip.Y || y >= clip.Y + clip.Height)
            {
                return;
            }
        }

        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            _buffer[y, x] = new Cell(c, fg, bg);
        }
    }

    public Cell GetPixel(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return _buffer[y, x];
        }
        return new Cell(' ', ConsoleColor.White, ConsoleColor.Black);
    }
}
