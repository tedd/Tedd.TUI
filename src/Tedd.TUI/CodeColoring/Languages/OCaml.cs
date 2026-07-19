using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class OCamlLanguage : ILanguage
{
    public string Id => "ocaml";
    public string[] Aliases => ["ml"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"\(\*[\s\S]*?\*\)", greedy: true));
        grammar.Add("char", new Pattern(@"'(?:[^\\\r\n']|\\(?:.|[ox]?[0-9a-f]{1,3}))'", regexOptions: "i", greedy: true));
        grammar.Add("string", new List<Pattern>
        {
            new Pattern(@"""(?:\\(?:[\s\S]|\r\n)|[^\\\r\n""])*""", greedy: true),
            new Pattern(@"\{([a-z_]*)\|[\s\S]*?\|\1\}", greedy: true)
        });
        grammar.Add("number", new List<Pattern>
        {
            new Pattern(@"\b(?:0b[01][01_]*|0o[0-7][0-7_]*)\b", regexOptions: "i"),
            new Pattern(@"\b0x[a-f0-9][a-f0-9_]*(?:\.[a-f0-9_]*)?(?:p[+-]?\d[\d_]*)?(?!\w)", regexOptions: "i"),
            new Pattern(@"\b\d[\d_]*(?:\.[\d_]*)?(?:e[+-]?\d[\d_]*)?(?!\w)", regexOptions: "i")
        });
        grammar.Add("directive", new Pattern(@"\B#\w+", alias: "property"));
        grammar.Add("label", new Pattern(@"\B~\w+", alias: "property"));
        grammar.Add("type-variable", new Pattern(@"\B'\w+", alias: "function"));
        grammar.Add("variant", new Pattern(@"`\w+", alias: "symbol"));
        grammar.Add("keyword", new Pattern(@"\b(?:as|assert|begin|class|constraint|do|done|downto|else|end|exception|external|for|fun|function|functor|if|in|include|inherit|initializer|lazy|let|match|method|module|mutable|new|nonrec|object|of|open|private|rec|sig|struct|then|to|try|type|val|value|virtual|when|where|while|with)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("operator-like-punctuation", new Pattern(@"\[[<>|]|[>|]\]|\{<|>\}", alias: "punctuation"));
        // Custom operators are allowed
        grammar.Add("operator", new Pattern(@"\.[.~]|:[=>]|[=<>@^|&+\-*\/$%!?~][!$%&*+\-.\/:<=>?@^|~]*|\b(?:and|asr|land|lor|lsl|lsr|lxor|mod|or)\b"));
        grammar.Add("punctuation", new Pattern(@";;|::|[(){}\[\].,:;#]|\b_\b"));
        return grammar;
    }
}
