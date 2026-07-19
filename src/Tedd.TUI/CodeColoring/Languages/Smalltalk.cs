using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class SmalltalkLanguage : ILanguage
{
    public string Id => "smalltalk";
    public string[] Aliases => ["st"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"""(?:""""|[^""])*""", greedy: true));
        grammar.Add("char", new Pattern(@"\$.", greedy: true));
        grammar.Add("string", new Pattern(@"'(?:''|[^'])*'", greedy: true));
        grammar.Add("symbol", new Pattern(@"#[\da-z]+|#(?:-|([+\/\\*~<>=@%|&?!])\1?)|#(?=\()", regexOptions: "i"));

        var blockArgumentsInside = new Grammar();
        blockArgumentsInside.Add("variable", new Pattern(@":[\da-z]+", regexOptions: "i"));
        blockArgumentsInside.Add("punctuation", new Pattern(@"\|"));
        grammar.Add("block-arguments", new Pattern(@"(\[\s*):[^\[|]*\|", lookbehind: true, inside: blockArgumentsInside));

        var temporaryVariablesInside = new Grammar();
        temporaryVariablesInside.Add("variable", new Pattern(@"[\da-z]+", regexOptions: "i"));
        temporaryVariablesInside.Add("punctuation", new Pattern(@"\|"));
        grammar.Add("temporary-variables", new Pattern(@"\|[^|]+\|", inside: temporaryVariablesInside));

        grammar.Add("keyword", new Pattern(@"\b(?:new|nil|self|super)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("number", new List<Pattern>
        {
            new Pattern(@"\d+r-?[\dA-Z]+(?:\.[\dA-Z]+)?(?:e-?\d+)?"),
            new Pattern(@"\b\d+(?:\.\d+)?(?:e-?\d+)?")
        });
        grammar.Add("operator", new Pattern(@"[<=]=?|:=|~[~=]|\/\/?|\\\\|>[>=]?|[!^+\-*&|,@]"));
        grammar.Add("punctuation", new Pattern(@"[.;:?\[\](){}]"));
        return grammar;
    }
}
