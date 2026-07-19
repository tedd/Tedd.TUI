using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class NimLanguage : ILanguage
{
    public string Id => "nim";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"#.*", greedy: true));
        // Double-quoted strings can be prefixed by an identifier (generalized raw string literals)
        grammar.Add("string", new Pattern(@"(?:\b(?!\d)(?:\w|\\x[89a-fA-F][0-9a-fA-F])+)?(?:""""""[\s\S]*?""""""(?!"")|""(?:\\[\s\S]|""""|[^""\\])*"")", greedy: true));
        // Character literals are handled specifically to prevent issues with numeric type suffixes
        grammar.Add("char", new Pattern(@"'(?:\\(?:\d+|x[\da-fA-F]{0,2}|.)|[^'])'", greedy: true));

        var functionInside = new Grammar();
        functionInside.Add("operator", new Pattern(@"\*$"));
        grammar.Add("function", new Pattern(@"(?:(?!\d)(?:\w|\\x[89a-fA-F][0-9a-fA-F])+|`[^`\r\n]+`)\*?(?:\[[^\]]+\])?(?=\s*\()", greedy: true, inside: functionInside));

        // We don't want to highlight operators (and anything really) inside backticks
        var identifierInside = new Grammar();
        identifierInside.Add("punctuation", new Pattern(@"`"));
        grammar.Add("identifier", new Pattern(@"`[^`\r\n]+`", greedy: true, inside: identifierInside));

        // The negative look ahead prevents wrong highlighting of the .. operator
        grammar.Add("number", new Pattern(@"\b(?:0[xXoObB][\da-fA-F_]+|\d[\d_]*(?:(?!\.\.)\.[\d_]*)?(?:[eE][+-]?\d[\d_]*)?)(?:'?[iuf]\d*)?"));
        grammar.Add("keyword", new Pattern(@"\b(?:addr|as|asm|atomic|bind|block|break|case|cast|concept|const|continue|converter|defer|discard|distinct|do|elif|else|end|enum|except|export|finally|for|from|func|generic|if|import|include|interface|iterator|let|macro|method|mixin|nil|object|out|proc|ptr|raise|ref|return|static|template|try|tuple|type|using|var|when|while|with|without|yield)\b"));
        grammar.Add("operator", new Pattern(@"(^|[({\[](?=\.\.)|(?![({\[]\.).)(?:(?:[=+\-*\/<>@$~&%|!?^:\\]|\.\.|\.(?![)}\]]))+|\b(?:and|div|in|is|isnot|mod|not|notin|of|or|shl|shr|xor)\b)", regexOptions: "m", lookbehind: true));
        grammar.Add("punctuation", new Pattern(@"[({\[]\.|\.[)}\]]|[`(){}\[\],:]"));
        return grammar;
    }
}
