using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class GraphQLLanguage : ILanguage
{
    public string Id => "graphql";
    public string[] Aliases => ["gql"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"#.*"));
        grammar.Add("description", new Pattern(@"(?:""""""(?:[^""]|(?!"""""")"")*""""""|""(?:\\.|[^\\""\r\n])*"")(?=\s*[a-z_])", regexOptions: "i", greedy: true, alias: "string"));
        grammar.Add("string", new Pattern(@"""""""(?:[^""]|(?!"""""")"")*""""""|""(?:\\.|[^\\""\r\n])*""", greedy: true));
        grammar.Add("number", new Pattern(@"(?:\B-|\b)\d+(?:\.\d+)?(?:e[+-]?\d+)?\b", regexOptions: "i"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("variable", new Pattern(@"\$[a-z_]\w*", regexOptions: "i"));
        grammar.Add("directive", new Pattern(@"@[a-z_]\w*", regexOptions: "i", alias: "function"));
        grammar.Add("attr-name", new Pattern(@"\b[a-z_]\w*(?=\s*(?:\((?:[^()""]|""(?:\\.|[^\\""\r\n])*"")*\))?:)", regexOptions: "i", greedy: true));
        grammar.Add("atom-input", new Pattern(@"\b[A-Z]\w*Input\b", alias: "class-name"));
        grammar.Add("scalar", new Pattern(@"\b(?:Boolean|Float|ID|Int|String)\b"));
        grammar.Add("constant", new Pattern(@"\b[A-Z][A-Z_\d]*\b"));
        grammar.Add("class-name", new Pattern(@"(\b(?:enum|implements|interface|on|scalar|type|union)\s+|&\s*|:\s*|\[)[A-Z_]\w*", lookbehind: true));
        grammar.Add("fragment", new Pattern(@"(\bfragment\s+|\.{3}\s*(?!on\b))[a-zA-Z_]\w*", lookbehind: true, alias: "function"));
        grammar.Add("definition-mutation", new Pattern(@"(\bmutation\s+)[a-zA-Z_]\w*", lookbehind: true, alias: "function"));
        grammar.Add("definition-query", new Pattern(@"(\bquery\s+)[a-zA-Z_]\w*", lookbehind: true, alias: "function"));
        grammar.Add("keyword", new Pattern(@"\b(?:directive|enum|extend|fragment|implements|input|interface|mutation|on|query|repeatable|scalar|schema|subscription|type|union)\b"));
        grammar.Add("operator", new Pattern(@"[!=|&]|\.{3}"));
        grammar.Add("property-query", new Pattern(@"\w+(?=\s*\()"));
        grammar.Add("object", new Pattern(@"\w+(?=\s*\{)"));
        grammar.Add("punctuation", new Pattern(@"[!(){}\[\]:=,]"));
        grammar.Add("property", new Pattern(@"\w+"));
        return grammar;
    }
}
