using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class KotlinLanguage : ILanguage
{
    public string Id => "kotlin";
    public string[] Aliases => ["kt", "kts"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        grammar["keyword"] = new List<Pattern>
        {
            // The lookbehind prevents wrong highlighting of e.g. kotlin.properties.get
            new Pattern(@"(^|[^.])\b(?:abstract|actual|annotation|as|break|by|catch|class|companion|const|constructor|continue|crossinline|data|do|dynamic|else|enum|expect|external|final|finally|for|fun|get|if|import|in|infix|init|inline|inner|interface|internal|is|lateinit|noinline|null|object|open|operator|out|override|package|private|protected|public|reified|return|sealed|set|super|suspend|tailrec|this|throw|to|try|typealias|val|var|vararg|when|where|while)\b", lookbehind: true)
        };
        grammar["function"] = new List<Pattern>
        {
            new Pattern(@"(?:`[^\r\n`]+`|\b\w+)(?=\s*\()", greedy: true),
            new Pattern(@"(\.)(?:`[^\r\n`]+`|\w+)(?=\s*\{)", lookbehind: true, greedy: true)
        };
        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"\b(?:0[xX][\da-fA-F]+(?:_[\da-fA-F]+)*|0[bB][01]+(?:_[01]+)*|\d+(?:_\d+)*(?:\.\d+(?:_\d+)*)?(?:[eE][+-]?\d+(?:_\d+)*)?[fFL]?)\b")
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"\+[+=]?|-[-=>]?|==?=?|!(?:!|==?)?|[\/*%<>]=?|[?:]:?|\.\.|&&|\|\||\b(?:and|inv|or|shl|shr|ushr|xor)\b")
        };

        var interpolationInside = new Grammar();
        interpolationInside.Add("interpolation-punctuation", new Pattern(@"^\$\{?|\}$", alias: "punctuation"));
        interpolationInside.Add("expression", new Pattern(@"[\s\S]+", inside: grammar));

        var multilineStringInside = new Grammar();
        multilineStringInside.Add("interpolation", new Pattern(@"\$(?:[a-z_]\w*|\{[^{}]*\})", regexOptions: "i", inside: interpolationInside));
        multilineStringInside.Add("string", new Pattern(@"[\s\S]+"));

        var singlelineStringInside = new Grammar();
        singlelineStringInside.Add("interpolation", new Pattern(@"((?:^|[^\\])(?:\\{2})*)\$(?:[a-z_]\w*|\{[^{}]*\})", regexOptions: "i", lookbehind: true, inside: interpolationInside));
        singlelineStringInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.InsertBefore("string", new Grammar
        {
            { "string-literal", new List<Pattern>
                {
                    new Pattern(@"""""""(?:[^$]|\$(?:(?!\{)|\{[^{}]*\}))*?""""""", alias: "multiline", inside: multilineStringInside),
                    new Pattern(@"""(?:[^""\\\r\n$]|\\.|\$(?:(?!\{)|\{[^{}]*\}))*""", alias: "singleline", inside: singlelineStringInside)
                }
            },
            { "char", new List<Pattern>
                {
                    new Pattern(@"'(?:[^'\\\r\n]|\\(?:.|u[a-fA-F0-9]{0,4}))'", greedy: true)
                }
            }
        });

        grammar.InsertBefore("keyword", new Grammar
        {
            { "annotation", new List<Pattern>
                {
                    new Pattern(@"\B@(?:\w+:)?(?:[A-Z]\w*|\[[^\]]+\])", alias: "builtin")
                }
            }
        });

        grammar.InsertBefore("function", new Grammar
        {
            { "label", new List<Pattern> { new Pattern(@"\b\w+@|@\w+\b", alias: "symbol") } }
        });

        grammar.Remove("class-name");
        grammar.Remove("string");

        return grammar;
    }
}
