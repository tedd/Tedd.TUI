using System.Collections.Generic;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.CodeColoring.Languages;

public class NasmLanguage : ILanguage
{
    public string Id => "nasm";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@";.*$", regexOptions: "m"));
        grammar.Add("string", new Pattern(@"([""'`])(?:\\.|(?!\1)[^\\\r\n])*\1"));
        grammar.Add("label", new Pattern(@"(^\s*)[A-Za-z._?$][\w.?$@~#]*:", regexOptions: "m", lookbehind: true, alias: "function"));
        grammar.Add("keyword", new List<Pattern>
        {
            new Pattern(@"\[?BITS (?:16|32|64)\]?"),
            new Pattern(@"(^\s*)section\s*[a-z.]+:?", regexOptions: "im", lookbehind: true),
            new Pattern(@"(?:extern|global)[^;\r\n]*", regexOptions: "i"),
            new Pattern(@"(?:CPU|DEFAULT|FLOAT).*$", regexOptions: "m")
        });
        grammar.Add("register", new Pattern(@"\b(?:st\d|[xyz]mm\d\d?|[cdt]r\d|r\d\d?[bwd]?|[er]?[abcd]x|[abcd][hl]|[er]?(?:bp|di|si|sp)|[cdefgs]s)\b", regexOptions: "i", alias: "variable"));
        grammar.Add("number", new Pattern(@"(?:\b|(?=\$))(?:0[hx](?:\.[\da-f]+|[\da-f]+(?:\.[\da-f]+)?)(?:p[+-]?\d+)?|\d[\da-f]+[hx]|\$\d[\da-f]*|0[oq][0-7]+|[0-7]+[oq]|0[by][01]+|[01]+[by]|0[dt]\d+|(?:\d+(?:\.\d+)?|\.\d+)(?:\.?e[+-]?\d+)?[dt]?)\b", regexOptions: "i"));
        grammar.Add("operator", new Pattern(@"[\[\]*+\-\/%<>=&|$!]"));
        return grammar;
    }
}
