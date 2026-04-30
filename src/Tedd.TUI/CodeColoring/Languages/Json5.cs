using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class Json5Language : ILanguage
{
    public string Id => "json5";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var json = new JsonLanguage().GetGrammar();
        var grammar = Grammar.Extend(json, new Grammar());

        string stringPattern = @"(""|')(?:\\(?:\r\n?|\n|.)|(?!\1)[^\\\r\n])*\1";

        grammar["property"] = new List<Pattern>
        {
            new Pattern(stringPattern + "(?=\\s*:)", greedy: true),
            new Pattern(@"(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?=\s*:)", alias: "unquoted")
        };

        grammar["string"] = new List<Pattern> { new Pattern(stringPattern, greedy: true) };
        grammar["number"] = new List<Pattern> { new Pattern(@"[+-]?\b(?:NaN|Infinity|0x[a-fA-F\d]+)\b|[+-]?(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:[eE][+-]?\d+\b)?") };

        return grammar;
    }
}
