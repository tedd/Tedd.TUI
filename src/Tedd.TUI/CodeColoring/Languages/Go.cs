using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class GoLanguage : ILanguage
{
    public string Id => "go";
    public string[] Aliases => ["golang"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        grammar["string"] = new List<Pattern>
        {
            new Pattern(@"(^|[^\\])""(?:\\.|[^""\\\r\n])*""|`[^`]*`", lookbehind: true, greedy: true)
        };
        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:break|case|chan|const|continue|default|defer|else|fallthrough|for|func|go(?:to)?|if|import|interface|map|package|range|return|select|struct|switch|type|var)\b")
        };
        grammar["boolean"] = new List<Pattern> { new Pattern(@"\b(?:_|false|iota|nil|true)\b") };
        grammar["number"] = new List<Pattern>
        {
            // binary and octal integers
            new Pattern(@"\b0(?:b[01_]+|o[0-7_]+)i?\b", regexOptions: "i"),
            // hexadecimal integers and floats
            new Pattern(@"\b0x(?:[a-f\d_]+(?:\.[a-f\d_]*)?|\.[a-f\d_]+)(?:p[+-]?\d+(?:_\d+)*)?i?(?!\w)", regexOptions: "i"),
            // decimal integers and floats
            new Pattern(@"(?:\b\d[\d_]*(?:\.[\d_]*)?|\B\.\d[\d_]*)(?:e[+-]?[\d_]+)?i?(?!\w)", regexOptions: "i")
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"[*\/%^!=]=?|\+[=+]?|-[=-]?|\|[=|]?|&(?:=|&|\^=?)?|>(?:>=?|=)?|<(?:<=?|=|-)?|:=|\.\.\.")
        };
        grammar["builtin"] = new List<Pattern>
        {
            new Pattern(@"\b(?:append|bool|byte|cap|close|complex|complex(?:64|128)|copy|delete|error|float(?:32|64)|imag|u?int(?:8|16|32|64)?|len|make|new|panic|print(?:ln)?|real|recover|rune|string|uintptr)\b")
        };

        grammar.InsertBefore("string", new Grammar
        {
            { "char", new List<Pattern> { new Pattern(@"'(?:\\.|[^'\\\r\n]){0,10}'", greedy: true) } }
        });

        grammar.Remove("class-name");

        return grammar;
    }
}
