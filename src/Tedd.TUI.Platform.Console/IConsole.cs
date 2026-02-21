using System;
using System.Text;

namespace Tedd.TUI.Platform.Console;

public interface IConsole
{
    int WindowWidth { get; }
    int WindowHeight { get; }
    int BufferWidth { get; }
    int BufferHeight { get; }
    bool CursorVisible { set; }
    Encoding OutputEncoding { set; }

    void SetCursorPosition(int left, int top);
    void Write(string value);
    void Write(char value);

    ConsoleColor ForegroundColor { set; }
    ConsoleColor BackgroundColor { set; }
    void ResetColor();
}
