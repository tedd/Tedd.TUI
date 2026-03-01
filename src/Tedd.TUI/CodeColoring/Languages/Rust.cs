using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class RustLanguage : ILanguage
{
    public string Id => "rust";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        string multilineComment = @"\/\*(?:[^*/]|\*(?!\/)|\/(?!\*)|<self>)*\*\/";
        // Nested comments (depth 2)
        multilineComment = Nested(multilineComment, 2);

        grammar.Add("comment", new List<Pattern>
        {
            new Pattern(Replace(@"(^|[^\\])<<0>>", multilineComment), lookbehind: true, greedy: true),
            new Pattern(@"(^|[^\\:])\/\/.*", lookbehind: true, greedy: true)
        });

        grammar.Add("string", new Pattern(@"b?""(?:\\[\s\S]|[^\\""])*""|b?r(#*)""(?:[^""]|""(?!\1))*""\1", greedy: true));
        grammar.Add("char", new Pattern(@"b?'(?:\\(?:x[0-7][\da-fA-F]|u\{(?:[\da-fA-F]_*){1,6}\}|.)|[^\\\r\n\t'])'", greedy: true));

        var attributeInside = new Grammar();
        attributeInside.Add("string", grammar["string"]); // Recursive ref? Wait, 'grammar["string"]' is empty/not added yet?
        // Patterns are added sequentially. 'string' was just added.
        // So grammar["string"] works.

        grammar.Add("attribute", new Pattern(@"#!?\[(?:[^\[\]""]|""(?:\\[\s\S]|[^\\""])*"")*\]", greedy: true, alias: "attr-name", inside: attributeInside));

        var closureParamsInside = new Grammar();
        closureParamsInside.Add("closure-punctuation", new Pattern(@"^\||\|$", alias: "punctuation"));
        // rest: recursive rust
        // We will fill this later.

        grammar.Add("closure-params", new Pattern(@"([=(,:]\s*|\bmove\s*)\|[^|]*\||\|[^|]*\|(?=\s*(?:\{|->))", lookbehind: true, greedy: true, inside: closureParamsInside));

        grammar.Add("lifetime-annotation", new Pattern(@"'\w+", alias: "symbol"));
        grammar.Add("fragment-specifier", new Pattern(@"(\$\w+:)[a-z]+", lookbehind: true, alias: "punctuation"));
        grammar.Add("variable", new Pattern(@"\$\w+"));
        grammar.Add("function-definition", new Pattern(@"(\bfn\s+)\w+", lookbehind: true, alias: "function"));
        grammar.Add("type-definition", new Pattern(@"(\b(?:enum|struct|trait|type|union)\s+)\w+", lookbehind: true, alias: "class-name"));

        var moduleInside = new Grammar();
        moduleInside.Add("punctuation", new Pattern(@"::"));

        grammar.Add("module-declaration", new List<Pattern>
        {
            new Pattern(@"(\b(?:crate|mod)\s+)[a-z][a-z_\d]*", lookbehind: true, alias: "namespace"),
            new Pattern(@"(\b(?:crate|self|super)\s*)::\s*[a-z][a-z_\d]*\b(?:\s*::(?:\s*[a-z][a-z_\d]*\s*::)*)?", lookbehind: true, alias: "namespace", inside: moduleInside)
        });

        grammar.Add("keyword", new List<Pattern>
        {
            new Pattern(@"\b(?:Self|abstract|as|async|await|become|box|break|const|continue|crate|do|dyn|else|enum|extern|final|fn|for|if|impl|in|let|loop|macro|match|mod|move|mut|override|priv|pub|ref|return|self|static|struct|super|trait|try|type|typeof|union|unsafe|unsized|use|virtual|where|while|yield)\b"),
            new Pattern(@"\b(?:bool|char|f(?:32|64)|[ui](?:8|16|32|64|128|size)|str)\b")
        });

        grammar.Add("function", new Pattern(@"\b[a-z_]\w*(?=\s*(?:::\s*<|\())"));
        grammar.Add("macro", new Pattern(@"\b\w+!", alias: "property"));
        grammar.Add("constant", new Pattern(@"\b[A-Z_][A-Z_\d]+\b"));
        grammar.Add("class-name", new Pattern(@"\b[A-Z]\w*\b"));

        grammar.Add("namespace", new Pattern(@"(?:\b[a-z][a-z_\d]*\s*::\s*)*\b[a-z][a-z_\d]*\s*::(?!\s*<)", inside: moduleInside));

        grammar.Add("number", new Pattern(@"\b(?:0x[\dA-Fa-f](?:_?[\dA-Fa-f])*|0o[0-7](?:_?[0-7])*|0b[01](?:_?[01])*|(?:(?:\d(?:_?\d)*)?\.)?\d(?:_?\d)*(?:[Ee][+-]?\d+)?)(?:_?(?:f32|f64|[iu](?:8|16|32|64|size)?))?\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("punctuation", new Pattern(@"->|\.\.=|\.{1,3}|::|[{}[\];(),:]"));
        grammar.Add("operator", new Pattern(@"[-+*\/%!^]=?|=[=>]?|&[&=]?|\|[|=]?|<<?=?|>>?=?|[@?]"));

        // Fill recursive references
        foreach (var kvp in grammar)
        {
            if (!closureParamsInside.ContainsKey(kvp.Key))
                closureParamsInside[kvp.Key] = kvp.Value;
        }

        return grammar;
    }
}
