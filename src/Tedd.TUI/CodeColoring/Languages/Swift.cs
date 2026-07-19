using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class SwiftLanguage : ILanguage
{
    public string Id => "swift";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        // Nested comments are supported up to 2 levels
        grammar.Add("comment", new Pattern(@"(^|[^\\:])(?:\/\/.*|\/\*(?:[^/*]|\/(?!\*)|\*(?!\/)|\/\*(?:[^*]|\*(?!\/))*\*\/)*\*\/)", lookbehind: true, greedy: true));

        var stringInside = new Grammar();
        stringInside.Add("interpolation", new Pattern(@"(\\\()(?:[^()]|\([^()]*\))*(?=\))", lookbehind: true, inside: grammar));
        stringInside.Add("interpolation-punctuation", new Pattern(@"^\)|\\\($", alias: "punctuation"));
        stringInside.Add("punctuation", new Pattern(@"\\(?=[\r\n])"));
        stringInside.Add("string", new Pattern(@"[\s\S]+"));

        var rawStringInside = new Grammar();
        rawStringInside.Add("interpolation", new Pattern(@"(\\#+\()(?:[^()]|\([^()]*\))*(?=\))", lookbehind: true, inside: grammar));
        rawStringInside.Add("interpolation-punctuation", new Pattern(@"^\)|\\#+\($", alias: "punctuation"));
        rawStringInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.Add("string-literal", new List<Pattern>
        {
            // https://docs.swift.org/swift-book/LanguageGuide/StringsAndCharacters.html
            new Pattern(@"(^|[^""#])(?:""(?:\\(?:\((?:[^()]|\([^()]*\))*\)|\r\n|[^(])|[^\\\r\n""])*""|""""""(?:\\(?:\((?:[^()]|\([^()]*\))*\)|[^(])|[^\\""]|""(?!""""))*"""""")(?![""#])",
                lookbehind: true, greedy: true, alias: "string", inside: stringInside),
            new Pattern(@"(^|[^""#])(#+)(?:""(?:\\(?:#+\((?:[^()]|\([^()]*\))*\)|\r\n|[^#])|[^\\\r\n])*?""|""""""(?:\\(?:#+\((?:[^()]|\([^()]*\))*\)|[^#])|[^\\])*?"""""")\2",
                lookbehind: true, greedy: true, alias: "string", inside: rawStringInside)
        });

        var directiveInside = new Grammar();
        directiveInside.Add("directive-name", new Pattern(@"^#\w+"));
        directiveInside.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        directiveInside.Add("number", new Pattern(@"\b\d+(?:\.\d+)*\b"));
        directiveInside.Add("operator", new Pattern(@"!|&&|\|\||[<>]=?"));
        directiveInside.Add("punctuation", new Pattern(@"[(),]"));

        // directives with conditions
        grammar.Add("directive", new Pattern(@"#(?:(?:elseif|if)\b(?:[ \t]*(?:![ \t]*)?(?:\b\w+\b(?:[ \t]*\((?:[^()]|\([^()]*\))*\))?|\((?:[^()]|\([^()]*\))*\))(?:[ \t]*(?:&&|\|\|))?)+|(?:else|endif)\b)",
            alias: "property", inside: directiveInside));

        grammar.Add("literal", new Pattern(@"#(?:colorLiteral|column|dsohandle|file(?:ID|Literal|Path)?|function|imageLiteral|line)\b", alias: "constant"));
        grammar.Add("other-directive", new Pattern(@"#\w+\b", alias: "property"));
        grammar.Add("attribute", new Pattern(@"@\w+", alias: "atrule"));
        grammar.Add("function-definition", new Pattern(@"(\bfunc\s+)\w+", lookbehind: true, alias: "function"));

        // https://docs.swift.org/swift-book/LanguageGuide/ControlFlow.html#ID141
        grammar.Add("label", new Pattern(@"\b(break|continue)\s+\w+|\b[a-zA-Z_]\w*(?=\s*:\s*(?:for|repeat|while)\b)", lookbehind: true, alias: "important"));

        grammar.Add("keyword", new Pattern(@"\b(?:Any|Protocol|Self|Type|actor|as|assignment|associatedtype|associativity|async|await|break|case|catch|class|continue|convenience|default|defer|deinit|didSet|do|dynamic|else|enum|extension|fallthrough|fileprivate|final|for|func|get|guard|higherThan|if|import|in|indirect|infix|init|inout|internal|is|isolated|lazy|left|let|lowerThan|mutating|none|nonisolated|nonmutating|open|operator|optional|override|postfix|precedencegroup|prefix|private|protocol|public|repeat|required|rethrows|return|right|safe|self|set|some|static|struct|subscript|super|switch|throw|throws|try|typealias|unowned|unsafe|var|weak|where|while|willSet)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("nil", new Pattern(@"\bnil\b", alias: "constant"));
        grammar.Add("short-argument", new Pattern(@"\$\d+\b"));
        grammar.Add("omit", new Pattern(@"\b_\b", alias: "keyword"));
        grammar.Add("number", new Pattern(@"\b(?:[\d_]+(?:\.[\de_]+)?|0x[a-f0-9_]+(?:\.[a-f0-9p_]+)?|0b[01_]+|0o[0-7_]+)\b", regexOptions: "i"));

        // A class name must start with an upper-case letter and be either 1 letter long or contain a lower-case letter.
        grammar.Add("class-name", new Pattern(@"\b[A-Z](?:[A-Z_\d]*[a-z]\w*)?\b"));
        grammar.Add("function", new Pattern(@"\b[a-z_]\w*(?=\s*\()", regexOptions: "i"));
        grammar.Add("constant", new Pattern(@"\b(?:[A-Z_]{2,}|k[A-Z][A-Za-z_]+)\b"));

        // Operators are generic in Swift; this only supports ASCII operators.
        grammar.Add("operator", new Pattern(@"[-+*/%=!<>&|^~?]+|\.[.\-+*/%=!<>&|^~?]+"));
        grammar.Add("punctuation", new Pattern(@"[{}[\]();,.:\\]"));

        return grammar;
    }
}
