using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class GroovyLanguage : ILanguage
{
    public string Id => "groovy";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        var interpolationInside = new Grammar();
        interpolationInside.Add("interpolation-punctuation", new Pattern(@"^\$\{?|\}$", alias: "punctuation"));
        interpolationInside.Add("expression", new Pattern(@"[\s\S]+", inside: grammar));

        var interpolation = new Pattern(@"((?:^|[^\\$])(?:\\{2})*)\$(?:\w+|\{[^{}]*\})", lookbehind: true, inside: interpolationInside);

        grammar["string"] = new List<Pattern>
        {
            new Pattern(@"'''(?:[^\\]|\\[\s\S])*?'''|'(?:\\.|[^\\'\r\n])*'", greedy: true)
        };
        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:abstract|as|assert|boolean|break|byte|case|catch|char|class|const|continue|def|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|in|instanceof|int|interface|long|native|new|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|trait|transient|try|void|volatile|while)\b")
        };
        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"\b(?:0b[01_]+|0x[\da-f_]+(?:\.[\da-f_p\-]+)?|[\d_]+(?:\.[\d_]+)?(?:e[+-]?\d+)?)[glidf]?\b", regexOptions: "i")
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"(^|[^.])(?:~|==?~?|\?[.:]?|\*(?:[.=]|\*=?)?|\.[@&]|\.\.<|\.\.(?!\.)|-[-=>]?|\+[+=]?|!=?|<(?:<=?|=>?)?|>(?:>>?=?|=)?|&[&=]?|\|[|=]?|\/=?|\^=?|%=?)", lookbehind: true)
        };
        grammar["punctuation"] = new List<Pattern> { new Pattern(@"\.+|[{}[\];(),:$]") };

        var interpolationStringInside = new Grammar();
        interpolationStringInside.Add("interpolation", interpolation);
        interpolationStringInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.InsertBefore("string", new Grammar
        {
            { "shebang", new List<Pattern> { new Pattern(@"#!.+", alias: "comment", greedy: true) } },
            { "interpolation-string", new List<Pattern>
                {
                    new Pattern(@"""""""(?:[^\\]|\\[\s\S])*?""""""|([""/])(?:\\.|(?!\1)[^\\\r\n])*\1|\$\/(?:[^/$]|\$(?:[/$]|(?![/$]))|\/(?!\$))*\/\$", greedy: true, inside: interpolationStringInside)
                }
            }
        });

        grammar.InsertBefore("punctuation", new Grammar
        {
            { "spock-block", new List<Pattern> { new Pattern(@"\b(?:and|cleanup|expect|given|setup|then|when|where):") } }
        });

        grammar.InsertBefore("function", new Grammar
        {
            { "annotation", new List<Pattern> { new Pattern(@"(^|[^.])@\w+", lookbehind: true, alias: "punctuation") } }
        });

        return grammar;
    }
}
