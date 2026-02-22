using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

public class SystemConsoleWrapper : IConsole
{
    public int WindowWidth => System.Console.WindowWidth;
    public int WindowHeight => System.Console.WindowHeight;
    public int BufferWidth => System.Console.BufferWidth;
    public int BufferHeight => System.Console.BufferHeight;

    public bool CursorVisible
    {
        set => System.Console.CursorVisible = value;
    }

    public Encoding OutputEncoding
    {
        set => System.Console.OutputEncoding = value;
    }

    public void SetCursorPosition(int left, int top) => System.Console.SetCursorPosition(left, top);

    public void Write(string value) => System.Console.Write(value);

    public void Write(char value) => System.Console.Write(value);

    public ConsoleColor ForegroundColor
    {
        set => System.Console.ForegroundColor = value;
    }

    public ConsoleColor BackgroundColor
    {
        set => System.Console.BackgroundColor = value;
    }

    public void ResetColor() => System.Console.ResetColor();
}
