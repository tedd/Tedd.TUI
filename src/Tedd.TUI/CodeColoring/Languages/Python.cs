using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class PythonLanguage : ILanguage
{
    public string Id => "python";
    public string[] Aliases => new[] { "py" };

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"(^|[^\\])#.*", lookbehind: true, greedy: true));

        var interpolationInside = new Grammar();
        interpolationInside.Add("format-spec", new Pattern(@"(:)[^:(){}]+(?=\}$)", lookbehind: true));
        interpolationInside.Add("conversion-option", new Pattern(@"![sra](?=[:}]$)", alias: "punctuation"));
        // rest: null (will be recursive python)

        var stringInterpolationInside = new Grammar();
        stringInterpolationInside.Add("interpolation", new Pattern(@"((?:^|[^{])(?:\{\{)*)\{(?!\{)(?:[^{}]|\{(?!\{)(?:[^{}]|\{(?!\{)(?:[^{}])+\})+\})+\}", lookbehind: true, inside: interpolationInside));
        stringInterpolationInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.Add("string-interpolation", new Pattern(@"(?:f|fr|rf)(?:(""""""|''')[\s\S]*?\1|(""|')(?:\\.|(?!\2)[^\\\r\n])*\2)", regexOptions: "i", greedy: true, inside: stringInterpolationInside));

        grammar.Add("triple-quoted-string", new Pattern(@"(?:[rub]|br|rb)?(""""""|''')[\s\S]*?\1", regexOptions: "i", greedy: true, alias: "string"));
        grammar.Add("string", new Pattern(@"(?:[rub]|br|rb)?(""|')(?:\\.|(?!\1)[^\\\r\n])*\1", regexOptions: "i", greedy: true));
        grammar.Add("function", new Pattern(@"((?:^|\s)def[ \t]+)[a-zA-Z_]\w*(?=\s*\()", lookbehind: true));
        grammar.Add("class-name", new Pattern(@"(\bclass\s+)\w+", regexOptions: "i", lookbehind: true));
        grammar.Add("decorator", new Pattern(@"(^[\t ]*)@\w+(?:\.\w+)*", regexOptions: "m", lookbehind: true, alias: "annotation punctuation", inside: new Grammar { { "punctuation", new List<Pattern> { new Pattern(@"\.") } } }));
        grammar.Add("keyword", new Pattern(@"\b(?:_(?=\s*:)|and|as|assert|async|await|break|case|class|continue|def|del|elif|else|except|exec|finally|for|from|global|if|import|in|is|lambda|match|nonlocal|not|or|pass|print|raise|return|try|while|with|yield)\b"));
        grammar.Add("builtin", new Pattern(@"\b(?:__import__|abs|all|any|apply|ascii|basestring|bin|bool|buffer|bytearray|bytes|callable|chr|classmethod|cmp|coerce|compile|complex|delattr|dict|dir|divmod|enumerate|eval|execfile|file|filter|float|format|frozenset|getattr|globals|hasattr|hash|help|hex|id|input|int|intern|isinstance|issubclass|iter|len|list|locals|long|map|max|memoryview|min|next|object|oct|open|ord|pow|property|range|raw_input|reduce|reload|repr|reversed|round|set|setattr|slice|sorted|staticmethod|str|sum|super|tuple|type|unichr|unicode|vars|xrange|zip)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:False|None|True)\b"));
        grammar.Add("number", new Pattern(@"\b0(?:b(?:_?[01])+|o(?:_?[0-7])+|x(?:_?[a-f0-9])+)\b|(?:\b\d+(?:_\d+)*(?:\.(?:\d+(?:_\d+)*)?)?|\B\.\d+(?:_\d+)*)(?:e[+-]?\d+(?:_\d+)*)?j?(?!\w)", regexOptions: "i"));
        grammar.Add("operator", new Pattern(@"[-+%=]=?|!=|:=|\*\*?=?|\/\/?=?|<[<=>]?|>[=>]?|[&|^~]"));
        grammar.Add("punctuation", new Pattern(@"[{}[\];(),.:]"));

        // Recursive assignment for rest
        // We can't assign 'grammar' directly to 'interpolationInside' because it copies.
        // We can append grammar patterns to interpolationInside.
        foreach(var kvp in grammar)
        {
             if (!interpolationInside.ContainsKey(kvp.Key))
                interpolationInside[kvp.Key] = kvp.Value;
        }

        return grammar;
    }
}
