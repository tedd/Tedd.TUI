using System;

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

    public VirtualBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _buffer = new Cell[height, width];
        Clear();
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
