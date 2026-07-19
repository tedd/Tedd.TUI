using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class AdaLanguage : ILanguage
{
    public string Id => "ada";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"--.*"));
        grammar.Add("string", new Pattern(@"""(?:""""|[^""\r\f\n])*"""));
        grammar.Add("number", new List<Pattern>
        {
            new Pattern(@"\b\d(?:_?\d)*#[\dA-F](?:_?[\dA-F])*(?:\.[\dA-F](?:_?[\dA-F])*)?#(?:E[+-]?\d(?:_?\d)*)?", regexOptions: "i"),
            new Pattern(@"\b\d(?:_?\d)*(?:\.\d(?:_?\d)*)?(?:E[+-]?\d(?:_?\d)*)?\b", regexOptions: "i")
        });
        grammar.Add("attribute", new Pattern(@"\b'\w+", alias: "attr-name"));
        grammar.Add("keyword", new Pattern(@"\b(?:abort|abs|abstract|accept|access|aliased|all|and|array|at|begin|body|case|constant|declare|delay|delta|digits|do|else|elsif|end|entry|exception|exit|for|function|generic|goto|if|in|interface|is|limited|loop|mod|new|not|null|of|or|others|out|overriding|package|pragma|private|procedure|protected|raise|range|record|rem|renames|requeue|return|reverse|select|separate|some|subtype|synchronized|tagged|task|terminate|then|type|until|use|when|while|with|xor)\b", regexOptions: "i"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b", regexOptions: "i"));
        grammar.Add("operator", new Pattern(@"<[=>]?|>=?|=>?|:=|\/=?|\*\*?|[&+-]"));
        grammar.Add("punctuation", new Pattern(@"\.\.?|[,;():]"));
        grammar.Add("char", new Pattern(@"'.'"));
        grammar.Add("variable", new Pattern(@"\b[a-z](?:\w)*\b", regexOptions: "i"));
        return grammar;
    }
}
