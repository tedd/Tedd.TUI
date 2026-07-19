using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class ErlangLanguage : ILanguage
{
    public string Id => "erlang";
    public string[] Aliases => ["erl"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"%.+"));
        grammar.Add("string", new Pattern(@"""(?:\\.|[^\\""\r\n])*""", greedy: true));
        grammar.Add("quoted-function", new Pattern(@"'(?:\\.|[^\\'\r\n])+'(?=\()", alias: "function"));
        grammar.Add("quoted-atom", new Pattern(@"'(?:\\.|[^\\'\r\n])+'", alias: "atom"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("keyword", new Pattern(@"\b(?:after|begin|case|catch|end|fun|if|of|receive|try|when)\b"));
        grammar.Add("number", new List<Pattern>
        {
            new Pattern(@"\$\\?."),
            new Pattern(@"\b\d+#[a-z0-9]+", regexOptions: "i"),
            new Pattern(@"(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:e[+-]?\d+)?", regexOptions: "i")
        });
        grammar.Add("function", new Pattern(@"\b[a-z][\w@]*(?=\()"));
        // Look-behind prevents wrong highlighting of atoms containing "@"
        grammar.Add("variable", new Pattern(@"(^|[^@])(?:\b|\?)[A-Z_][\w@]*", lookbehind: true));
        grammar.Add("operator", new List<Pattern>
        {
            new Pattern(@"[=\/<>:]=|=[:\/]=|\+\+?|--?|[=*\/!]|\b(?:and|andalso|band|bnot|bor|bsl|bsr|bxor|div|not|or|orelse|rem|xor)\b"),
            new Pattern(@"(^|[^<])<(?!<)", lookbehind: true),
            new Pattern(@"(^|[^>])>(?!>)", lookbehind: true)
        });
        grammar.Add("atom", new Pattern(@"\b[a-z][\w@]*"));
        grammar.Add("punctuation", new Pattern(@"[()[\]{}:;,.#|]|<<|>>"));
        return grammar;
    }
}
