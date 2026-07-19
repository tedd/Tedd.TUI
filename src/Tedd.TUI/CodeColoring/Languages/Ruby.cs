using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class RubyLanguage : ILanguage
{
    public string Id => "ruby";
    public string[] Aliases => ["rb"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        var interpolationInside = new Grammar();
        var interpolation = new Pattern(@"((?:^|[^\\])(?:\\{2})*)#\{(?:[^{}]|\{[^{}]*\})*\}", lookbehind: true, inside: interpolationInside);
        interpolationInside.Add("content", new Pattern(@"^(#\{)[\s\S]+(?=\}$)", lookbehind: true, inside: grammar));
        interpolationInside.Add("delimiter", new Pattern(@"^#\{|\}$", alias: "punctuation"));

        string percentExpression = "(?:" + string.Join("|",
            @"([^a-zA-Z0-9\s{(\[<=])(?:(?!\1)[^\\]|\\[\s\S])*\1",
            @"\((?:[^()\\]|\\[\s\S]|\((?:[^()\\]|\\[\s\S])*\))*\)",
            @"\{(?:[^{}\\]|\\[\s\S]|\{(?:[^{}\\]|\\[\s\S])*\})*\}",
            @"\[(?:[^\[\]\\]|\\[\s\S]|\[(?:[^\[\]\\]|\\[\s\S])*\])*\]",
            @"<(?:[^<>\\]|\\[\s\S]|<(?:[^<>\\]|\\[\s\S])*>)*>") + ")";

        string symbolName = @"(?:""(?:\\.|[^""\\\r\n])*""|(?:\b[a-zA-Z_]\w*|[^\s\0-\x7F]+)[?!]?|\$.)";

        grammar["comment"] = new List<Pattern>
        {
            new Pattern(@"#.*|^=begin\s[\s\S]*?^=end", regexOptions: "m", greedy: true)
        };

        var classNameInside = new Grammar();
        classNameInside.Add("punctuation", new Pattern(@"[.\\]"));
        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(@"(\b(?:class|module)\s+|\bcatch\s+\()[\w.\\]+|\b[A-Z_]\w*(?=\s*\.\s*new\b)", lookbehind: true, inside: classNameInside)
        };

        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:BEGIN|END|alias|and|begin|break|case|class|def|define_method|defined|do|each|else|elsif|end|ensure|extend|for|if|in|include|module|new|next|nil|not|or|prepend|private|protected|public|raise|redo|require|rescue|retry|return|self|super|then|throw|undef|unless|until|when|while|yield)\b")
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"\.{2,3}|&\.|===|<?=>|[!=]?~|(?:&&|\|\||<<|>>|\*\*|[+\-*/%<>!^&|=])=?|[?:]")
        };
        grammar["punctuation"] = new List<Pattern> { new Pattern(@"[(){}[\].,;]") };

        grammar.InsertBefore("operator", new Grammar
        {
            { "double-colon", new List<Pattern> { new Pattern(@"::", alias: "punctuation") } }
        });

        var regexLiteralInside = new Grammar();
        regexLiteralInside.Add("interpolation", interpolation);
        regexLiteralInside.Add("regex", new Pattern(@"[\s\S]+"));

        var methodDefinitionInside = new Grammar();
        methodDefinitionInside.Add("function", new Pattern(@"\b\w+$"));
        methodDefinitionInside.Add("keyword", new Pattern(@"^self\b"));
        methodDefinitionInside.Add("class-name", new Pattern(@"^\w+"));
        methodDefinitionInside.Add("punctuation", new Pattern(@"\."));

        grammar.InsertBefore("keyword", new Grammar
        {
            { "regex-literal", new List<Pattern>
                {
                    new Pattern(@"%r" + percentExpression + @"[egimnosux]{0,6}", greedy: true, inside: regexLiteralInside),
                    new Pattern(@"(^|[^/])\/(?!\/)(?:\[[^\r\n\]]+\]|\\.|[^[/\\\r\n])+\/[egimnosux]{0,6}(?=\s*(?:$|[\r\n,.;})#]))", lookbehind: true, greedy: true, inside: regexLiteralInside)
                }
            },
            { "variable", new List<Pattern> { new Pattern(@"[@$]+[a-zA-Z_]\w*(?:[?!]|\b)") } },
            { "symbol", new List<Pattern>
                {
                    new Pattern(@"(^|[^:]):" + symbolName, lookbehind: true, greedy: true),
                    new Pattern(@"([\r\n{(,][ \t]*)" + symbolName + @"(?=:(?!:))", lookbehind: true, greedy: true)
                }
            },
            { "method-definition", new List<Pattern>
                {
                    new Pattern(@"(\bdef\s+)\w+(?:\s*\.\s*\w+)?", lookbehind: true, inside: methodDefinitionInside)
                }
            }
        });

        var stringLiteralInside = new Grammar();
        stringLiteralInside.Add("interpolation", interpolation);
        stringLiteralInside.Add("string", new Pattern(@"[\s\S]+"));

        var heredocDelimiterInside = new Grammar();
        heredocDelimiterInside.Add("symbol", new Pattern(@"\b\w+"));
        heredocDelimiterInside.Add("punctuation", new Pattern(@"^<<[-~]?"));

        var heredocInside = new Grammar();
        heredocInside.Add("delimiter", new Pattern(@"^<<[-~]?[a-z_]\w*|\b[a-z_]\w*$", regexOptions: "i", inside: heredocDelimiterInside));
        heredocInside.Add("interpolation", interpolation);
        heredocInside.Add("string", new Pattern(@"[\s\S]+"));

        var heredocQuotedDelimiterInside = new Grammar();
        heredocQuotedDelimiterInside.Add("symbol", new Pattern(@"\b\w+"));
        heredocQuotedDelimiterInside.Add("punctuation", new Pattern(@"^<<[-~]?'|'$"));

        var heredocQuotedInside = new Grammar();
        heredocQuotedInside.Add("delimiter", new Pattern(@"^<<[-~]?'[a-z_]\w*'|\b[a-z_]\w*$", regexOptions: "i", inside: heredocQuotedDelimiterInside));
        heredocQuotedInside.Add("string", new Pattern(@"[\s\S]+"));

        var commandLiteralInside = new Grammar();
        commandLiteralInside.Add("interpolation", interpolation);
        commandLiteralInside.Add("command", new Pattern(@"[\s\S]+", alias: "string"));

        grammar.InsertBefore("string", new Grammar
        {
            { "string-literal", new List<Pattern>
                {
                    new Pattern(@"%[qQiIwWs]?" + percentExpression, greedy: true, inside: stringLiteralInside),
                    new Pattern(@"(""|')(?:#\{[^}]+\}|#(?!\{)|\\(?:\r\n|[\s\S])|(?!\1)[^\\#\r\n])*\1", greedy: true, inside: stringLiteralInside),
                    new Pattern(@"<<[-~]?([a-z_]\w*)[\r\n](?:.*[\r\n])*?[\t ]*\1", regexOptions: "i", greedy: true, alias: "heredoc-string", inside: heredocInside),
                    new Pattern(@"<<[-~]?'([a-z_]\w*)'[\r\n](?:.*[\r\n])*?[\t ]*\1", regexOptions: "i", greedy: true, alias: "heredoc-string", inside: heredocQuotedInside)
                }
            },
            { "command-literal", new List<Pattern>
                {
                    new Pattern(@"%x" + percentExpression, greedy: true, inside: commandLiteralInside),
                    new Pattern(@"`(?:#\{[^}]+\}|#(?!\{)|\\(?:\r\n|[\s\S])|[^\\`#\r\n])*`", greedy: true, inside: commandLiteralInside)
                }
            }
        });

        grammar.InsertBefore("number", new Grammar
        {
            { "builtin", new List<Pattern>
                {
                    new Pattern(@"\b(?:Array|Bignum|Binding|Class|Continuation|Dir|Exception|FalseClass|File|Fixnum|Float|Hash|IO|Integer|MatchData|Method|Module|NilClass|Numeric|Object|Proc|Range|Regexp|Stat|String|Struct|Symbol|TMS|Thread|ThreadGroup|Time|TrueClass)\b")
                }
            },
            { "constant", new List<Pattern> { new Pattern(@"\b[A-Z][A-Z0-9_]*(?:[?!]|\b)") } }
        });

        grammar.Remove("function");
        grammar.Remove("string");

        return grammar;
    }
}
