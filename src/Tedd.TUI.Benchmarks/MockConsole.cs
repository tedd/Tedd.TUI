using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

public class MockConsole : IConsole
{
    public int WindowWidth { get; set; } = 80;
    public int WindowHeight { get; set; } = 25;
    public int BufferWidth { get; set; } = 80;
    public int BufferHeight { get; set; } = 25;
    public bool CursorVisible { get; set; }
    public Encoding OutputEncoding { get; set; } = Encoding.UTF8;

    public long WriteCount { get; private set; }
    public long SetCursorPositionCount { get; private set; }
    public long ColorChangeCount { get; private set; }

    public void SetCursorPosition(int left, int top)
    {
        SetCursorPositionCount++;
    }

    public void Write(string value)
    {
        WriteCount++;
    }

    public void Write(char value)
    {
        WriteCount++;
    }

    private ConsoleColor _foregroundColor;
    public ConsoleColor ForegroundColor
    {
        set
        {
            if (_foregroundColor != value)
            {
                _foregroundColor = value;
                ColorChangeCount++;
            }
        }
    }

    private ConsoleColor _backgroundColor;
    public ConsoleColor BackgroundColor
    {
        set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                ColorChangeCount++;
            }
        }
    }

    public void ResetColor()
    {
        // Treat as color change
        ColorChangeCount++;
    }

    public void ResetStats()
    {
        WriteCount = 0;
        SetCursorPositionCount = 0;
        ColorChangeCount = 0;
    }
}
