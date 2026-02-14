using System;
using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring;

public class Theme
{
    public ConsoleColor DefaultForeground { get; set; } = ConsoleColor.White;
    public ConsoleColor DefaultBackground { get; set; } = ConsoleColor.Black;

    public Dictionary<string, ConsoleColor> TokenColors { get; } = new Dictionary<string, ConsoleColor>();

    public Theme()
    {
        // Default mappings (Dark theme inspired)
        TokenColors["comment"] = ConsoleColor.DarkGreen;
        TokenColors["string"] = ConsoleColor.DarkYellow;
        TokenColors["class-name"] = ConsoleColor.Cyan;
        TokenColors["keyword"] = ConsoleColor.Blue;
        TokenColors["boolean"] = ConsoleColor.DarkCyan;
        TokenColors["function"] = ConsoleColor.Yellow;
        TokenColors["number"] = ConsoleColor.Magenta;
        TokenColors["operator"] = ConsoleColor.White;
        TokenColors["punctuation"] = ConsoleColor.DarkGray;
        TokenColors["variable"] = ConsoleColor.DarkCyan;
        TokenColors["constant"] = ConsoleColor.DarkMagenta;
        TokenColors["symbol"] = ConsoleColor.DarkRed;
        TokenColors["builtin"] = ConsoleColor.Cyan;
        TokenColors["property"] = ConsoleColor.Cyan;
        TokenColors["regex"] = ConsoleColor.DarkYellow;
        TokenColors["important"] = ConsoleColor.Red;
        TokenColors["attr-name"] = ConsoleColor.Cyan;
        TokenColors["attr-value"] = ConsoleColor.DarkYellow;
        TokenColors["namespace"] = ConsoleColor.Cyan;
        TokenColors["prolog"] = ConsoleColor.DarkGray;
        TokenColors["doctype"] = ConsoleColor.DarkGray;
        TokenColors["cdata"] = ConsoleColor.DarkGray;
        TokenColors["tag"] = ConsoleColor.Blue;
        TokenColors["entity"] = ConsoleColor.DarkRed;
        TokenColors["url"] = ConsoleColor.Blue;
        TokenColors["bold"] = ConsoleColor.White;
        TokenColors["italic"] = ConsoleColor.White;
        TokenColors["inserted"] = ConsoleColor.Green;
        TokenColors["deleted"] = ConsoleColor.Red;
    }

    public ConsoleColor GetColor(string tokenType)
    {
        if (TokenColors.TryGetValue(tokenType, out var color))
        {
            return color;
        }
        return DefaultForeground;
    }
}
