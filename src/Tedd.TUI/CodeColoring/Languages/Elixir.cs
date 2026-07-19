using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class ElixirLanguage : ILanguage
{
    public string Id => "elixir";
    public string[] Aliases => ["ex", "exs"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var interpolationInside = new Grammar();
        interpolationInside.Add("delimiter", new Pattern(@"^#\{|\}$", alias: "punctuation"));

        var stringInside = new Grammar();
        stringInside.Add("interpolation", new Pattern(@"#\{[^}]+\}", inside: interpolationInside));

        var docInside = new Grammar();
        docInside.Add("attribute", new Pattern(@"^@\w+"));
        docInside.Add("string", new Pattern(@"['""][\s\S]+"));

        grammar.Add("doc", new Pattern(@"@(?:doc|moduledoc)\s+(?:(""""""|''')[\s\S]*?\1|(""|')(?:\\(?:\r\n|[\s\S])|(?!\2)[^\\\r\n])*\2)", inside: docInside));
        grammar.Add("comment", new Pattern(@"#.*", greedy: true));
        grammar.Add("regex", new Pattern(@"~[rR](?:(""""""|''')(?:\\[\s\S]|(?!\1)[^\\])+\1|([\/|""'])(?:\\.|(?!\2)[^\\\r\n])+\2|\((?:\\.|[^\\)\r\n])+\)|\[(?:\\.|[^\\\]\r\n])+\]|\{(?:\\.|[^\\}\r\n])+\}|<(?:\\.|[^\\>\r\n])+>)[uismxfr]*", greedy: true));
        grammar.Add("string", new List<Pattern>
        {
            new Pattern(@"~[cCsSwW](?:(""""""|''')(?:\\[\s\S]|(?!\1)[^\\])+\1|([\/|""'])(?:\\.|(?!\2)[^\\\r\n])+\2|\((?:\\.|[^\\)\r\n])+\)|\[(?:\\.|[^\\\]\r\n])+\]|\{(?:\\.|#\{[^}]+\}|#(?!\{)|[^#\\}\r\n])+\}|<(?:\\.|[^\\>\r\n])+>)[csa]?", greedy: true, inside: stringInside),
            new Pattern(@"(""""""|''')[\s\S]*?\1", greedy: true, inside: stringInside),
            new Pattern(@"(""|')(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1", greedy: true, inside: stringInside)
        });
        grammar.Add("atom", new Pattern(@"(^|[^:]):\w+", lookbehind: true, alias: "symbol"));
        grammar.Add("module", new Pattern(@"\b[A-Z]\w*\b", alias: "class-name"));
        grammar.Add("attr-name", new Pattern(@"\b\w+\??:(?!:)"));
        grammar.Add("argument", new Pattern(@"(^|[^&])&\d+", lookbehind: true, alias: "variable"));
        grammar.Add("attribute", new Pattern(@"@\w+", alias: "variable"));
        grammar.Add("function", new Pattern(@"\b[_a-zA-Z]\w*[?!]?(?:(?=\s*(?:\.\s*)?\()|(?=\/\d))"));
        grammar.Add("number", new Pattern(@"\b(?:0[box][a-f\d_]+|\d[\d_]*)(?:\.[\d_]+)?(?:e[+-]?[\d_]+)?\b", regexOptions: "i"));
        grammar.Add("keyword", new Pattern(@"\b(?:after|alias|and|case|catch|cond|def(?:callback|delegate|exception|impl|macro|module|n|np|p|protocol|struct)?|do|else|end|fn|for|if|import|not|or|quote|raise|require|rescue|try|unless|unquote|use|when)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|nil|true)\b"));
        grammar.Add("operator", new List<Pattern>
        {
            new Pattern(@"\bin\b|&&?|\|[|>]?|\\\\|::|\.\.\.?|\+\+?|-[->]?|<[-=>]|>=|!==?|\B!|=(?:==?|[>~])?|[*\/^]"),
            new Pattern(@"([^<])<(?!<)", lookbehind: true),
            new Pattern(@"([^>])>(?!>)", lookbehind: true)
        });
        grammar.Add("punctuation", new Pattern(@"<<|>>|[.,%\[\]{}()]"));

        // Prism's $rest: 'elixir' inside string interpolations.
        foreach (var kvp in grammar)
        {
            if (!interpolationInside.ContainsKey(kvp.Key))
            {
                interpolationInside[kvp.Key] = kvp.Value;
            }
        }

        return grammar;
    }
}
