using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class ScalaLanguage : ILanguage
{
    public string Id => "scala";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var java = new JavaLanguage().GetGrammar();
        var grammar = Grammar.Extend(java, new Grammar());

        grammar["triple-quoted-string"] = new List<Pattern>
        {
            new Pattern(@"""""""[\s\S]*?""""""", greedy: true, alias: "string")
        };
        grammar["string"] = new List<Pattern>
        {
            new Pattern(@"(""|')(?:\\.|(?!\1)[^\\\r\n])*\1", greedy: true)
        };
        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"<-|=>|\b(?:abstract|case|catch|class|def|derives|do|else|enum|extends|extension|final|finally|for|forSome|given|if|implicit|import|infix|inline|lazy|match|new|null|object|opaque|open|override|package|private|protected|return|sealed|self|super|this|throw|trait|transparent|try|type|using|val|var|while|with|yield)\b")
        };
        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"\b0x(?:[\da-f]*\.)?[\da-f]+|(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:e\d+)?[dfl]?", regexOptions: "i")
        };
        grammar["builtin"] = new List<Pattern>
        {
            new Pattern(@"\b(?:Any|AnyRef|AnyVal|Boolean|Byte|Char|Double|Float|Int|Long|Nothing|Short|String|Unit)\b")
        };
        grammar["symbol"] = new List<Pattern> { new Pattern(@"'[^\d\s\\]\w*") };

        var interpolationExpressionInside = new Grammar();
        interpolationExpressionInside.Add("punctuation", new Pattern(@"^\$\{?|\}$"));
        interpolationExpressionInside.Add("expression", new Pattern(@"[\s\S]+", inside: grammar));

        var stringInterpolationInside = new Grammar();
        stringInterpolationInside.Add("id", new Pattern(@"^\w+", greedy: true, alias: "function"));
        stringInterpolationInside.Add("escape", new Pattern(@"\\\$""|\$[$""]", greedy: true, alias: "symbol"));
        stringInterpolationInside.Add("interpolation", new Pattern(@"\$(?:\w+|\{(?:[^{}]|\{[^{}]*\})*\})", greedy: true, inside: interpolationExpressionInside));
        stringInterpolationInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.InsertBefore("triple-quoted-string", new Grammar
        {
            { "string-interpolation", new List<Pattern>
                {
                    new Pattern(@"\b[a-z]\w*(?:""""""(?:[^$]|\$(?:[^{]|\{(?:[^{}]|\{[^{}]*\})*\}))*?""""""|""(?:[^$""\r\n]|\$(?:[^{]|\{(?:[^{}]|\{[^{}]*\})*\}))*"")", regexOptions: "i", greedy: true, inside: stringInterpolationInside)
                }
            }
        });

        grammar.Remove("doc-comment");
        grammar.Remove("class-name");
        grammar.Remove("function");
        grammar.Remove("constant");

        return grammar;
    }
}
