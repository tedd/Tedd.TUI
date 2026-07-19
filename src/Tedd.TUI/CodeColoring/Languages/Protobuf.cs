using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class ProtobufLanguage : ILanguage
{
    public string Id => "protobuf";
    public string[] Aliases => ["proto"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        string builtinTypes = @"\b(?:bool|bytes|double|s?fixed(?:32|64)|float|[su]?int(?:32|64)|string)\b";

        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(@"(\b(?:enum|extend|message|service)\s+)[A-Za-z_]\w*(?=\s*\{)", lookbehind: true),
            new Pattern(@"(\b(?:rpc\s+\w+|returns)\s*\(\s*(?:stream\s+)?)\.?[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?=\s*\))", lookbehind: true)
        };
        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:enum|extend|extensions|import|message|oneof|option|optional|package|public|repeated|required|reserved|returns|rpc(?=\s+\w)|service|stream|syntax|to)\b(?!\s*=\s*\d)")
        };
        grammar["function"] = new List<Pattern> { new Pattern(@"\b[a-z_]\w*(?=\s*\()", regexOptions: "i") };

        var mapInside = new Grammar();
        mapInside.Add("punctuation", new Pattern(@"[<>.,]"));
        mapInside.Add("builtin", new Pattern(builtinTypes));

        var positionalClassNameInside = new Grammar();
        positionalClassNameInside.Add("punctuation", new Pattern(@"\."));

        grammar.InsertBefore("operator", new Grammar
        {
            { "map", new List<Pattern>
                {
                    new Pattern(@"\bmap<\s*[\w.]+\s*,\s*[\w.]+\s*>(?=\s+[a-z_]\w*\s*[=;])", regexOptions: "i", alias: "class-name", inside: mapInside)
                }
            },
            { "builtin", new List<Pattern> { new Pattern(builtinTypes) } },
            { "positional-class-name", new List<Pattern>
                {
                    new Pattern(@"(?:\b|\B\.)[a-z_]\w*(?:\.[a-z_]\w*)*(?=\s+[a-z_]\w*\s*[=;])", regexOptions: "i", alias: "class-name", inside: positionalClassNameInside)
                }
            },
            { "annotation", new List<Pattern> { new Pattern(@"(\[\s*)[a-z_]\w*(?=\s*=)", regexOptions: "i", lookbehind: true) } }
        });

        return grammar;
    }
}
