using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class JsonLanguage : ILanguage
{
    public string Id => "json";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        grammar.Add("property", new Pattern(@"(^|[^\\])""(?:\\.|[^\\""\r\n])*""(?=\s*:)", lookbehind: true, greedy: true));
        grammar.Add("string", new Pattern(@"(^|[^\\])""(?:\\.|[^\\""\r\n])*""(?!\s*:)", lookbehind: true, greedy: true));
        grammar.Add("comment", new Pattern(@"\/\/.*|\/\*[\s\S]*?(?:\*\/|$)", greedy: true));
        grammar.Add("number", new Pattern(@"-?\b\d+(?:\.\d+)?(?:e[+-]?\d+)?\b", regexOptions: "i"));
        grammar.Add("punctuation", new Pattern(@"[{}[\],]"));
        grammar.Add("operator", new Pattern(@":"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("null", new Pattern(@"\bnull\b", alias: "keyword"));

        return grammar;
    }
}
