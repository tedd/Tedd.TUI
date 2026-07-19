using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class JuliaLanguage : ILanguage
{
    public string Id => "julia";
    public string[] Aliases => ["jl"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        // support one level of nested comments
        grammar.Add("comment", new Pattern(@"(^|[^\\])(?:#=(?:[^#=]|=(?!#)|#(?!=)|#=(?:[^#=]|=(?!#)|#(?!=))*=#)*=#|#.*)", lookbehind: true));
        grammar.Add("regex", new Pattern(@"r""(?:\\.|[^""\\\r\n])*""[imsx]{0,4}", greedy: true));
        grammar.Add("string", new Pattern(@"""""""[\s\S]+?""""""|(?:\b\w+)?""(?:\\.|[^""\\\r\n])*""|`(?:[^\\`\r\n]|\\.)*`", greedy: true));
        grammar.Add("char", new Pattern(@"(^|[^\w'])'(?:\\[^\r\n][^'\r\n]*|[^\\\r\n])'", lookbehind: true, greedy: true));
        grammar.Add("keyword", new Pattern(@"\b(?:abstract|baremodule|begin|bitstype|break|catch|ccall|const|continue|do|else|elseif|end|export|finally|for|function|global|if|immutable|import|importall|in|let|local|macro|module|print|println|quote|return|struct|try|type|typealias|using|while)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("number", new Pattern(@"(?:\b(?=\d)|\B(?=\.))(?:0[box])?(?:[\da-f]+(?:_[\da-f]+)*(?:\.(?:\d+(?:_\d+)*)?)?|\.\d+(?:_\d+)*)(?:[efp][+-]?\d+(?:_\d+)*)?j?", regexOptions: "i"));
        grammar.Add("operator", new Pattern(@"&&|\|\||[-+*^%÷⊻&$\\]=?|\/[\/=]?|!=?=?|\|[=>]?|<(?:<=?|[=:|])?|>(?:=|>>?=?)?|==?=?|[~≠≤≥'√∛]"));
        grammar.Add("punctuation", new Pattern(@"::?|[{}[\]();,.?]"));
        grammar.Add("constant", new Pattern(@"\b(?:(?:Inf|NaN)(?:16|32|64)?|im|pi)\b|[πℯ]"));
        return grammar;
    }
}
