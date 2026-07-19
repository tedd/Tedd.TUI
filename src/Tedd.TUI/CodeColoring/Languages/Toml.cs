using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class TomlLanguage : ILanguage
{
    public string Id => "toml";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        string key = @"(?:[\w-]+|'[^'\n\r]*'|""(?:\\.|[^\\""\r\n])*"")";
        string dottedKey = key + @"(?:\s*\.\s*" + key + ")*";

        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"#.*", greedy: true));

        var tableInside = new Grammar();
        tableInside.Add("table-name", new Pattern(@"(^\[\[?\s*)" + dottedKey, lookbehind: true, alias: "variable"));
        tableInside.Add("punctuation", new Pattern(@"\[|\]"));

        // keep entire table header (including brackets) under one parent token
        grammar.Add("table", new Pattern(@"(^[\t ]*)(?:\[\[\s*" + dottedKey + @"\s*\]\]|\[\s*" + dottedKey + @"\s*\])(?!\])", regexOptions: "m", lookbehind: true, greedy: true, inside: tableInside));

        grammar.Add("key", new Pattern(@"(^[\t ]*|[{,]\s*)" + dottedKey + @"(?=\s*=)", regexOptions: "m", lookbehind: true, greedy: true, alias: "property"));
        grammar.Add("string", new Pattern(@"""""""(?:\\[\s\S]|[^\\])*?""""""|'''[\s\S]*?'''|'[^'\n\r]*'|""(?:\\.|[^\\""\r\n])*""", greedy: true));
        grammar.Add("date", new List<Pattern>
        {
            // Offset Date-Time, Local Date-Time, Local Date
            new Pattern(@"\b\d{4}-\d{2}-\d{2}(?:[T\s]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?)?\b", regexOptions: "i", alias: "number"),
            // Local Time
            new Pattern(@"\b\d{2}:\d{2}:\d{2}(?:\.\d+)?\b", alias: "number")
        });
        grammar.Add("number", new Pattern(@"(?:\b0(?:x[\da-zA-Z]+(?:_[\da-zA-Z]+)*|o[0-7]+(?:_[0-7]+)*|b[10]+(?:_[10]+)*))\b|[-+]?\b\d+(?:_\d+)*(?:\.\d+(?:_\d+)*)?(?:[eE][+-]?\d+(?:_\d+)*)?\b|[-+]?\b(?:inf|nan)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("punctuation", new Pattern(@"[.,=[\]{}]"));
        return grammar;
    }
}
