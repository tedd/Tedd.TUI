using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

// Standalone PHP grammar (no HTML embedding); Prism uses this same grammar
// directly whenever the source contains no markup.
public class PhpLanguage : ILanguage
{
    public string Id => "php";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        string comment = @"\/\*[\s\S]*?\*\/|\/\/.*|#(?!\[).*";
        string number = @"\b0b[01]+(?:_[01]+)*\b|\b0o[0-7]+(?:_[0-7]+)*\b|\b0x[\da-f]+(?:_[\da-f]+)*\b|(?:\b\d+(?:_\d+)*\.?(?:\d+(?:_\d+)*)?|\B\.\d+)(?:e[+-]?\d+)?";
        string operatorPattern = @"<?=>|\?\?=?|\.{3}|\??->|[!=]=?=?|::|\*\*=?|--|\+\+|&&|\|\||<<|>>|[?~]|[/^|%*&<>.+-]=?";
        string punctuation = @"[{}\[\](),:;]";

        var backslashPunctuation = new Grammar();
        backslashPunctuation.Add("punctuation", new Pattern(@"\\"));

        var constant = new List<Pattern>
        {
            new Pattern(@"\b(?:false|true)\b", regexOptions: "i", alias: "boolean"),
            new Pattern(@"(::\s*)\b[a-z_]\w*\b(?!\s*\()", regexOptions: "i", greedy: true, lookbehind: true),
            new Pattern(@"(\b(?:case|const)\s+)\b[a-z_]\w*(?=\s*[;=])", regexOptions: "i", greedy: true, lookbehind: true),
            new Pattern(@"\b(?:null)\b", regexOptions: "i"),
            new Pattern(@"\b[A-Z_][A-Z0-9_]*\b(?!\s*\()")
        };

        grammar.Add("delimiter", new Pattern(@"\?>$|^<\?(?:php(?=\s)|=)?", regexOptions: "i", alias: "important"));
        grammar.Add("doc-comment", new Pattern(@"\/\*\*(?!\/)[\s\S]*?\*\/", greedy: true, alias: "comment"));
        grammar.Add("comment", new Pattern(comment));
        grammar.Add("variable", new Pattern(@"\$+(?:\w+\b|(?=\{))"));
        grammar.Add("package", new Pattern(@"(namespace\s+|use\s+(?:function\s+)?)(?:\\?\b[a-z_]\w*)+\b(?!\\)", regexOptions: "i", lookbehind: true, inside: backslashPunctuation));
        grammar.Add("class-name-definition", new Pattern(@"(\b(?:class|enum|interface|trait)\s+)\b[a-z_]\w*(?!\\)\b", regexOptions: "i", lookbehind: true, alias: "class-name"));
        grammar.Add("function-definition", new Pattern(@"(\bfunction\s+)[a-z_]\w*(?=\s*\()", regexOptions: "i", lookbehind: true, alias: "function"));

        grammar.Add("keyword", new List<Pattern>
        {
            new Pattern(@"(\(\s*)\b(?:array|bool|boolean|float|int|integer|object|string)\b(?=\s*\))", regexOptions: "i", alias: "type-casting", greedy: true, lookbehind: true),
            new Pattern(@"([(,?]\s*)\b(?:array(?!\s*\()|bool|callable|(?:false|null)(?=\s*\|)|float|int|iterable|mixed|object|self|static|string)\b(?=\s*\$)", regexOptions: "i", alias: "type-hint", greedy: true, lookbehind: true),
            new Pattern(@"(\)\s*:\s*(?:\?\s*)?)\b(?:array(?!\s*\()|bool|callable|(?:false|null)(?=\s*\|)|float|int|iterable|mixed|never|object|self|static|string|void)\b", regexOptions: "i", alias: "return-type", greedy: true, lookbehind: true),
            new Pattern(@"\b(?:array(?!\s*\()|bool|float|int|iterable|mixed|object|string|void)\b", regexOptions: "i", alias: "type-declaration", greedy: true),
            new Pattern(@"(\|\s*)(?:false|null)\b|\b(?:false|null)(?=\s*\|)", regexOptions: "i", alias: "type-declaration", greedy: true, lookbehind: true),
            new Pattern(@"\b(?:parent|self|static)(?=\s*::)", regexOptions: "i", alias: "static-context", greedy: true),
            new Pattern(@"(\byield\s+)from\b", regexOptions: "i", lookbehind: true),
            new Pattern(@"\bclass\b", regexOptions: "i"),
            new Pattern(@"((?:^|[^\s>:]|(?:^|[^-])>|(?:^|[^:]):)\s*)\b(?:abstract|and|array|as|break|callable|case|catch|clone|const|continue|declare|default|die|do|echo|else|elseif|empty|enddeclare|endfor|endforeach|endif|endswitch|endwhile|enum|eval|exit|extends|final|finally|fn|for|foreach|function|global|goto|if|implements|include|include_once|instanceof|insteadof|interface|isset|list|match|namespace|never|new|or|parent|print|private|protected|public|readonly|require|require_once|return|self|static|switch|throw|trait|try|unset|use|var|while|xor|yield|__halt_compiler)\b", regexOptions: "i", lookbehind: true)
        });

        grammar.Add("argument-name", new Pattern(@"([(,]\s*)\b[a-z_]\w*(?=\s*:(?!:))", regexOptions: "i", lookbehind: true));

        grammar.Add("class-name", new List<Pattern>
        {
            new Pattern(@"(\b(?:extends|implements|instanceof|new(?!\s+self|\s+static))\s+|\bcatch\s*\()\b[a-z_]\w*(?!\\)\b", regexOptions: "i", greedy: true, lookbehind: true),
            new Pattern(@"(\|\s*)\b[a-z_]\w*(?!\\)\b", regexOptions: "i", greedy: true, lookbehind: true),
            new Pattern(@"\b[a-z_]\w*(?!\\)\b(?=\s*\|)", regexOptions: "i", greedy: true),
            new Pattern(@"(\|\s*)(?:\\?\b[a-z_]\w*)+\b", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, lookbehind: true, inside: backslashPunctuation),
            new Pattern(@"(?:\\?\b[a-z_]\w*)+\b(?=\s*\|)", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, inside: backslashPunctuation),
            new Pattern(@"(\b(?:extends|implements|instanceof|new(?!\s+self\b|\s+static\b))\s+|\bcatch\s*\()(?:\\?\b[a-z_]\w*)+\b(?!\\)", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, lookbehind: true, inside: backslashPunctuation),
            new Pattern(@"\b[a-z_]\w*(?=\s*\$)", regexOptions: "i", alias: "type-declaration", greedy: true),
            new Pattern(@"(?:\\?\b[a-z_]\w*)+(?=\s*\$)", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, inside: backslashPunctuation),
            new Pattern(@"\b[a-z_]\w*(?=\s*::)", regexOptions: "i", alias: "static-context", greedy: true),
            new Pattern(@"(?:\\?\b[a-z_]\w*)+(?=\s*::)", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, inside: backslashPunctuation),
            new Pattern(@"([(,?]\s*)[a-z_]\w*(?=\s*\$)", regexOptions: "i", alias: "type-hint", greedy: true, lookbehind: true),
            new Pattern(@"([(,?]\s*)(?:\\?\b[a-z_]\w*)+(?=\s*\$)", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, lookbehind: true, inside: backslashPunctuation),
            new Pattern(@"(\)\s*:\s*(?:\?\s*)?)\b[a-z_]\w*(?!\\)\b", regexOptions: "i", alias: "return-type", greedy: true, lookbehind: true),
            new Pattern(@"(\)\s*:\s*(?:\?\s*)?)(?:\\?\b[a-z_]\w*)+\b(?!\\)", regexOptions: "i", alias: "class-name-fully-qualified", greedy: true, lookbehind: true, inside: backslashPunctuation)
        });

        grammar.Add("constant", constant);
        grammar.Add("function", new Pattern(@"(^|[^\\\w])\\?[a-z_](?:[\w\\]*\w)?(?=\s*\()", regexOptions: "i", lookbehind: true, inside: backslashPunctuation));
        grammar.Add("property", new Pattern(@"(->\s*)\w+", lookbehind: true));
        grammar.Add("number", new Pattern(number, regexOptions: "i"));
        grammar.Add("operator", new Pattern(operatorPattern));
        grammar.Add("punctuation", new Pattern(punctuation));

        var stringInterpolation = new Pattern(@"\{\$(?:\{(?:\{[^{}]+\}|[^{}]+)\}|[^{}])+\}|(^|[^\\{])\$+(?:\w+(?:\[[^\r\n\[\]]+\]|->\w+)?)", lookbehind: true, inside: grammar);

        var nowdocDelimiterInside = new Grammar();
        nowdocDelimiterInside.Add("punctuation", new Pattern(@"^<<<'?|[';]$"));
        var nowdocInside = new Grammar();
        nowdocInside.Add("delimiter", new Pattern(@"^<<<'[^']+'|[a-z_]\w*;$", regexOptions: "i", alias: "symbol", inside: nowdocDelimiterInside));

        var heredocDelimiterInside = new Grammar();
        heredocDelimiterInside.Add("punctuation", new Pattern(@"^<<<""?|["";]$"));
        var heredocInside = new Grammar();
        heredocInside.Add("delimiter", new Pattern(@"^<<<(?:""[^""]+""|[a-z_]\w*)|[a-z_]\w*;$", regexOptions: "i", alias: "symbol", inside: heredocDelimiterInside));
        heredocInside.Add("interpolation", stringInterpolation);

        var doubleQuotedInside = new Grammar();
        doubleQuotedInside.Add("interpolation", stringInterpolation);

        var stringPatterns = new List<Pattern>
        {
            new Pattern(@"<<<'([^']+)'[\r\n](?:.*[\r\n])*?\1;", alias: "nowdoc-string", greedy: true, inside: nowdocInside),
            new Pattern(@"<<<(?:""([^""]+)""[\r\n](?:.*[\r\n])*?\1;|([a-z_]\w*)[\r\n](?:.*[\r\n])*?\2;)", regexOptions: "i", alias: "heredoc-string", greedy: true, inside: heredocInside),
            new Pattern(@"`(?:\\[\s\S]|[^\\`])*`", alias: "backtick-quoted-string", greedy: true),
            new Pattern(@"'(?:\\[\s\S]|[^\\'])*'", alias: "single-quoted-string", greedy: true),
            new Pattern(@"""(?:\\[\s\S]|[^\\""])*""", alias: "double-quoted-string", greedy: true, inside: doubleQuotedInside)
        };

        var attributeContentInside = new Grammar();
        attributeContentInside.Add("comment", new Pattern(comment));
        attributeContentInside.Add("string", stringPatterns);
        attributeContentInside.Add("attribute-class-name", new List<Pattern>
        {
            new Pattern(@"([^:]|^)\b[a-z_]\w*(?!\\)\b", regexOptions: "i", alias: "class-name", greedy: true, lookbehind: true),
            new Pattern(@"([^:]|^)(?:\\?\b[a-z_]\w*)+", regexOptions: "i", alias: "class-name", greedy: true, lookbehind: true, inside: backslashPunctuation)
        });
        attributeContentInside.Add("constant", constant);
        attributeContentInside.Add("number", new Pattern(number, regexOptions: "i"));
        attributeContentInside.Add("operator", new Pattern(operatorPattern));
        attributeContentInside.Add("punctuation", new Pattern(punctuation));

        var attributeInside = new Grammar();
        attributeInside.Add("attribute-content", new Pattern(@"^(#\[)[\s\S]+(?=\]$)", lookbehind: true, inside: attributeContentInside));
        attributeInside.Add("delimiter", new Pattern(@"^#\[|\]$", alias: "punctuation"));

        grammar.InsertBefore("variable", new Grammar
        {
            { "string", stringPatterns },
            { "attribute", new List<Pattern>
                {
                    new Pattern(@"#\[(?:[^""'\/#]|\/(?![*/])|\/\/.*$|#(?!\[).*$|\/\*(?:[^*]|\*(?!\/))*\*\/|""(?:\\[\s\S]|[^\\""])*""|'(?:\\[\s\S]|[^\\'])*')+\](?=\s*[a-z$#])", regexOptions: "im", greedy: true, inside: attributeInside)
                }
            }
        });

        return grammar;
    }
}
