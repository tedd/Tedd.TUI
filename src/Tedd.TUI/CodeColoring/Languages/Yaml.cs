using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class YamlLanguage : ILanguage
{
    public string Id => "yaml";
    public string[] Aliases => ["yml"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        grammar.Add("scalar", new Pattern(@"([\-:]\s*(?:![^\s]+)?[ \t]*[|>])[ \t]*(?:((?:\r\n|\r|\n)+)[ \t]+)(?:(?:[^\r\n]+(?:\2[ \t]+[^\r\n]+)*)?)", lookbehind: true, alias: "string"));

        grammar.Add("comment", new Pattern(@"#.*"));

        grammar.Add("key", new Pattern(@"(\s*(?:^|[:\-?])[ \t]*)(?:(?:\w|_)+|(?:'(?:\\.|[^\\'\r\n])*'|""(?:\\.|[^\\""\r\n])*"")(?=\s*:))", lookbehind: true, alias: "atrule"));

        grammar.Add("directive", new Pattern(@"(^[ \t]*)%.+", regexOptions: "m", lookbehind: true, alias: "important"));

        grammar.Add("datetime", new Pattern(@"([:\-,?]\s*|\s)(?:\d{4}-\d\d?-\d\d?(?:[tT]|[ \t]+)\d\d?:\d\d:\d\d(?:\.\d*)?(?:[ \t]*(?:Z|[-+]\d\d?(?::\d\d)?))?|\d{4}-\d{2}-\d{2}|\d\d?:\d\d(?::\d\d(?:\.\d*)?)?)(?=[ \t]*(?:$|,|]|}))", lookbehind: true, alias: "number"));

        grammar.Add("boolean", new Pattern(@"([:\-,?]\s*|\s)(?:true|false|yes|no)(?=[ \t]*(?:$|,|]|}))", regexOptions: "i", lookbehind: true, alias: "important"));

        grammar.Add("null", new Pattern(@"([:\-,?]\s*|\s)(?:null|~)(?=[ \t]*(?:$|,|]|}))", regexOptions: "i", lookbehind: true, alias: "important"));

        grammar.Add("string", new Pattern(@"([:\-,?]\s*|\s)(?:'(?:\\.|[^\\'\r\n])*'|""(?:\\.|[^\\""\r\n])*"")(?=[ \t]*(?:$|,|]|}))", lookbehind: true, greedy: true));

        grammar.Add("number", new Pattern(@"([:\-,?]\s*|\s)[+-]?(?:0x[\da-f]+|0o[0-7]+|(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?|\.inf|\.nan)(?=[ \t]*(?:$|,|]|}))", regexOptions: "i", lookbehind: true));

        grammar.Add("tag", new Pattern(@"![^\s]+", alias: "important"));
        grammar.Add("important", new Pattern(@"[&*][\w]+"));
        grammar.Add("punctuation", new Pattern(@"---|[:[\]{}\-,|>?]"));

        return grammar;
    }
}
