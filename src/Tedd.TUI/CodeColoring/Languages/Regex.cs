using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class RegexLanguage : ILanguage
{
    public string Id => "regex";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var specialEscape = new Pattern(@"\\[\\(){}[\]^$+*?|.]", alias: "escape");
        var escape = new Pattern(@"\\(?:x[\da-fA-F]{2}|u[\da-fA-F]{4}|u\{[\da-fA-F]+\}|0[0-7]{0,2}|[123][0-7]{2}|c[a-zA-Z]|.)");
        var charSet = new Pattern(@"\.|\\[wsd]|\\p\{[^{}]+\}", regexOptions: "i", alias: "class-name");
        var charSetWithoutDot = new Pattern(@"\\[wsd]|\\p\{[^{}]+\}", regexOptions: "i", alias: "class-name");

        var rangeChar = @"(?:[^\\\\-]|" + escape.Regex.ToString() + ")";
        var range = new Pattern(rangeChar + "-" + rangeChar);

        var groupName = new Pattern(@"(<|')[^<>']+(?=[>']$)", lookbehind: true, alias: "variable");

        var charClassInside = new Grammar();
        charClassInside.Add("char-class-negation", new Pattern(@"(^\[)\^", lookbehind: true, alias: "operator"));
        charClassInside.Add("char-class-punctuation", new Pattern(@"^\[|\]$", alias: "punctuation"));
        charClassInside.Add("range", new Pattern(range.Regex, inside: new Grammar
        {
            { "escape", new List<Pattern> { escape } },
            { "range-punctuation", new List<Pattern> { new Pattern(@"-", alias: "operator") } }
        }));
        charClassInside.Add("special-escape", specialEscape);
        charClassInside.Add("char-set", charSetWithoutDot);
        charClassInside.Add("escape", escape);

        grammar.Add("char-class", new Pattern(@"((?:^|[^\\])(?:\\\\)*)\[(?:[^\\\]]|\\[\s\S])*\]", lookbehind: true, inside: charClassInside));
        grammar.Add("special-escape", specialEscape);
        grammar.Add("char-set", charSet);
        grammar.Add("backreference", new List<Pattern>
        {
            new Pattern(@"\\(?![123][0-7]{2})[1-9]", alias: "keyword"),
            new Pattern(@"\\k<[^<>']+>", alias: "keyword", inside: new Grammar { { "group-name", new List<Pattern> { groupName } } })
        });
        grammar.Add("anchor", new Pattern(@"[$^]|\\[ABbGZz]", alias: "function"));
        grammar.Add("escape", escape);
        grammar.Add("group", new List<Pattern>
        {
            new Pattern(@"\((?:\?(?:<[^<>']+>|'[^<>']+'|[>:]|<?[=!]|[idmnsuxU]+(?:-[idmnsuxU]+)?:?))?", alias: "punctuation", inside: new Grammar { { "group-name", new List<Pattern> { groupName } } }),
            new Pattern(@"\)", alias: "punctuation")
        });
        grammar.Add("quantifier", new Pattern(@"(?:[+*?]|\{\d+(?:,\d*)?\})[?+]?", alias: "number"));
        grammar.Add("alternation", new Pattern(@"\|", alias: "keyword"));

        return grammar;
    }
}
