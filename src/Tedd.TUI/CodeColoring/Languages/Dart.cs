using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class DartLanguage : ILanguage
{
    public string Id => "dart";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        var keywords = new List<Pattern>
        {
            new Pattern(@"\b(?:async|sync|yield)\*"),
            new Pattern(@"\b(?:abstract|assert|async|await|break|case|catch|class|const|continue|covariant|default|deferred|do|dynamic|else|enum|export|extends|extension|external|factory|final|finally|for|get|hide|if|implements|import|in|interface|library|mixin|new|null|on|operator|part|rethrow|return|set|show|static|super|switch|sync|this|throw|try|typedef|var|void|while|with|yield)\b")
        };

        // Handles named imports, such as http.Client
        string packagePrefix = @"(^|[^\w.])(?:[a-z]\w*\s*\.\s*)*(?:[A-Z]\w*\s*\.\s*)*";

        var namespaceInside = new Grammar();
        namespaceInside.Add("punctuation", new Pattern(@"\."));

        // based on the dart naming conventions
        var classNameInside = new Grammar();
        classNameInside.Add("namespace", new Pattern(@"^[a-z]\w*(?:\s*\.\s*[a-z]\w*)*(?:\s*\.)?", inside: namespaceInside));

        var className = new Pattern(packagePrefix + @"[A-Z](?:[\d_A-Z]*[a-z]\w*)?\b", lookbehind: true, inside: classNameInside);

        grammar["class-name"] = new List<Pattern>
        {
            className,
            // variables and parameters
            new Pattern(packagePrefix + @"[A-Z]\w*(?=\s+\w+\s*[;,=()])", lookbehind: true, inside: classNameInside)
        };

        grammar["keyword"] = keywords;

        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"\bis!|\b(?:as|is)\b|\+\+|--|&&|\|\||<<=?|>>=?|~(?:\/=?)?|[+\-*\/%&^|=!<>]=?|\?")
        };

        var interpolationExpressionInside = new Grammar();
        interpolationExpressionInside.Add("punctuation", new Pattern(@"^\$\{?|\}$"));
        interpolationExpressionInside.Add("expression", new Pattern(@"[\s\S]+", inside: grammar));

        var stringLiteralInside = new Grammar();
        stringLiteralInside.Add("interpolation", new Pattern(@"((?:^|[^\\])(?:\\{2})*)\$(?:\w+|\{(?:[^{}]|\{[^{}]*\})*\})", lookbehind: true, inside: interpolationExpressionInside));
        stringLiteralInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.InsertBefore("string", new Grammar
        {
            { "string-literal", new List<Pattern>
                {
                    new Pattern(@"r?(?:(""""""|''')[\s\S]*?\1|([""'])(?:\\.|(?!\2)[^\\\r\n])*\2(?!\2))", greedy: true, inside: stringLiteralInside)
                }
            }
        });
        grammar.Remove("string");

        var genericsInside = new Grammar();
        genericsInside.Add("class-name", className);
        genericsInside.Add("keyword", keywords);
        genericsInside.Add("punctuation", new Pattern(@"[<>(),.:]"));
        genericsInside.Add("operator", new Pattern(@"[?&|]"));

        grammar.InsertBefore("class-name", new Grammar
        {
            { "metadata", new List<Pattern> { new Pattern(@"@\w+", alias: "function") } },
            { "generics", new List<Pattern>
                {
                    new Pattern(@"<(?:[\w\s,.&?]|<(?:[\w\s,.&?]|<(?:[\w\s,.&?]|<[\w\s,.&?]*>)*>)*>)*>", inside: genericsInside)
                }
            }
        });

        return grammar;
    }
}
