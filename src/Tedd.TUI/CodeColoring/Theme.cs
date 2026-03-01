using System;
using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring;

public class Theme
{
    public ConsoleColor DefaultForeground { get; set; } = ConsoleColor.White;
    public ConsoleColor DefaultBackground { get; set; } = ConsoleColor.Black;

    public static Theme Default { get; set; } = new Theme();

    public Dictionary<string, ConsoleColor> TokenColors { get; } = [];

    public Theme()
    {
        // Default mappings based on PrismJS Default Theme (adapted for ConsoleColor)

        // Comments, blocks
        TokenColors["comment"] = ConsoleColor.DarkGray; // slategray
        TokenColors["prolog"] = ConsoleColor.DarkGray;
        TokenColors["doctype"] = ConsoleColor.DarkGray;
        TokenColors["cdata"] = ConsoleColor.DarkGray;
        TokenColors["punctuation"] = ConsoleColor.DarkGray; // #999

        // Namespace
        TokenColors["namespace"] = ConsoleColor.DarkGray; // opacity .7

        // Properties, Tags, Numbers, Booleans, Constants, Symbols, Deleted
        TokenColors["property"] = ConsoleColor.Magenta; // #905
        TokenColors["tag"] = ConsoleColor.Magenta;
        TokenColors["boolean"] = ConsoleColor.Magenta;
        TokenColors["number"] = ConsoleColor.Magenta;
        TokenColors["constant"] = ConsoleColor.Magenta;
        TokenColors["symbol"] = ConsoleColor.Magenta;
        TokenColors["deleted"] = ConsoleColor.Red; // #905 but deleted usually red
        TokenColors["deleted-sign"] = ConsoleColor.Red;
        TokenColors["deleted-arrow"] = ConsoleColor.Red;

        // Selectors, Attr Names, Strings, Chars, Builtins, Inserted
        TokenColors["selector"] = ConsoleColor.DarkGreen; // #690 (Olive) -> DarkGreen/DarkYellow?
        TokenColors["attr-name"] = ConsoleColor.DarkGreen;
        TokenColors["string"] = ConsoleColor.DarkGreen; // Usually strings are green or yellow.
        TokenColors["char"] = ConsoleColor.DarkGreen;
        TokenColors["builtin"] = ConsoleColor.DarkGreen;
        TokenColors["inserted"] = ConsoleColor.Green; // #690 but inserted usually green
        TokenColors["inserted-sign"] = ConsoleColor.Green;
        TokenColors["inserted-arrow"] = ConsoleColor.Green;

        // Operators, Entities, URLs
        TokenColors["operator"] = ConsoleColor.DarkYellow; // #9a6e3a (Brown)
        TokenColors["entity"] = ConsoleColor.DarkYellow;
        TokenColors["url"] = ConsoleColor.DarkYellow;

        // At-rules, Attr Values, Keywords
        TokenColors["atrule"] = ConsoleColor.Cyan; // #07a (Blue) -> Cyan for readability on black
        TokenColors["attr-value"] = ConsoleColor.Cyan;
        TokenColors["keyword"] = ConsoleColor.Cyan;

        // Functions, Class Names
        TokenColors["function"] = ConsoleColor.Red; // #DD4A68 (Pink) -> Red? Or Magenta?
        TokenColors["class-name"] = ConsoleColor.Red;

        // Regex, Important, Variables
        TokenColors["regex"] = ConsoleColor.Yellow; // #e90 (Orange)
        TokenColors["important"] = ConsoleColor.Yellow;
        TokenColors["variable"] = ConsoleColor.Yellow;

        // Styles
        TokenColors["bold"] = ConsoleColor.White;
        TokenColors["italic"] = ConsoleColor.Gray;

        // Diff specific
        TokenColors["coord"] = ConsoleColor.DarkBlue;
        TokenColors["diff"] = ConsoleColor.DarkGray; // Bold?
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
