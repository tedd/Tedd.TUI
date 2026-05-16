using System;
using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring;

public class Theme
{
    public TuiColor DefaultForeground { get; set; } = TuiColor.White;
    public TuiColor DefaultBackground { get; set; } = TuiColor.Black;

    public static Theme Default { get; set; } = new Theme();

    public Dictionary<string, TuiColor> TokenColors { get; } = [];

    public Theme()
    {
        // Default mappings based on PrismJS Default Theme (adapted for TuiColor parity
        // with the legacy 16-color palette).

        // Comments, blocks
        TokenColors["comment"] = TuiColor.DarkGray; // slategray
        TokenColors["prolog"] = TuiColor.DarkGray;
        TokenColors["doctype"] = TuiColor.DarkGray;
        TokenColors["cdata"] = TuiColor.DarkGray;
        TokenColors["punctuation"] = TuiColor.DarkGray; // #999

        // Namespace
        TokenColors["namespace"] = TuiColor.DarkGray; // opacity .7

        // Properties, Tags, Numbers, Booleans, Constants, Symbols, Deleted
        TokenColors["property"] = TuiColor.Magenta; // #905
        TokenColors["tag"] = TuiColor.Magenta;
        TokenColors["boolean"] = TuiColor.Magenta;
        TokenColors["number"] = TuiColor.Magenta;
        TokenColors["constant"] = TuiColor.Magenta;
        TokenColors["symbol"] = TuiColor.Magenta;
        TokenColors["deleted"] = TuiColor.Red; // #905 but deleted usually red
        TokenColors["deleted-sign"] = TuiColor.Red;
        TokenColors["deleted-arrow"] = TuiColor.Red;

        // Selectors, Attr Names, Strings, Chars, Builtins, Inserted
        TokenColors["selector"] = TuiColor.DarkGreen; // #690 (Olive)
        TokenColors["attr-name"] = TuiColor.DarkGreen;
        TokenColors["string"] = TuiColor.DarkGreen;
        TokenColors["char"] = TuiColor.DarkGreen;
        TokenColors["builtin"] = TuiColor.DarkGreen;
        TokenColors["inserted"] = TuiColor.Green;
        TokenColors["inserted-sign"] = TuiColor.Green;
        TokenColors["inserted-arrow"] = TuiColor.Green;

        // Operators, Entities, URLs
        TokenColors["operator"] = TuiColor.DarkYellow;
        TokenColors["entity"] = TuiColor.DarkYellow;
        TokenColors["url"] = TuiColor.DarkYellow;

        // At-rules, Attr Values, Keywords
        TokenColors["atrule"] = TuiColor.Cyan;
        TokenColors["attr-value"] = TuiColor.Cyan;
        TokenColors["keyword"] = TuiColor.Cyan;

        // Functions, Class Names
        TokenColors["function"] = TuiColor.Red;
        TokenColors["class-name"] = TuiColor.Red;

        // Regex, Important, Variables
        TokenColors["regex"] = TuiColor.Yellow;
        TokenColors["important"] = TuiColor.Yellow;
        TokenColors["variable"] = TuiColor.Yellow;

        // Styles
        TokenColors["bold"] = TuiColor.White;
        TokenColors["italic"] = TuiColor.Gray;

        // Diff specific
        TokenColors["coord"] = TuiColor.DarkBlue;
        TokenColors["diff"] = TuiColor.DarkGray;
    }

    public TuiColor GetColor(string tokenType)
    {
        if (TokenColors.TryGetValue(tokenType, out var color))
        {
            return color;
        }
        return DefaultForeground;
    }
}
