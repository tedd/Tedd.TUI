using System.Collections.Generic;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.CodeColoring.Languages;

public class LuaLanguage : ILanguage
{
    public string Id => "lua";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"^#!.+|--(?:\[(=*)\[[\s\S]*?\]\1\]|.*)", regexOptions: "m"));
        grammar.Add("string", new Pattern(@"([""'])(?:(?!\1)[^\\\r\n]|\\z(?:\r\n|\s)|\\(?:\r\n|[^z]))*\1|\[(=*)\[[\s\S]*?\]\2\]", greedy: true));
        grammar.Add("number", new Pattern(@"\b0x[a-f\d]+(?:\.[a-f\d]*)?(?:p[+-]?\d+)?\b|\b\d+(?:\.\B|(?:\.\d*)?(?:e[+-]?\d+)?\b)|\B\.\d+(?:e[+-]?\d+)?\b", regexOptions: "i"));
        grammar.Add("keyword", new Pattern(@"\b(?:and|break|do|else|elseif|end|false|for|function|goto|if|in|local|nil|not|or|repeat|return|then|true|until|while)\b"));
        grammar.Add("function", new Pattern(@"(?!\d)\w+(?=\s*(?:[({]))"));
        grammar.Add("operator", new List<Pattern>
        {
            new Pattern(@"[-+*%^&|#]|\/\/?|<[<=]?|>[>=]?|[=~]=?"),
            new Pattern(@"(^|[^.])\.\.(?!\.)", lookbehind: true)
        });
        grammar.Add("punctuation", new Pattern(@"[\[\](){},;]|\.+|:+"));
        return grammar;
    }
}
