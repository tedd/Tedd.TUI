using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class JavaScriptLanguage : ILanguage
{
    public string Id => "javascript";
    public string[] Aliases => ["js"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        var classNameInside = new Grammar();
        classNameInside.Add("punctuation", new Pattern(@"[.\\]"));

        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(@"(\b(?:class|extends|implements|instanceof|interface|new)\s+)[\w.\\]+", lookbehind: true, inside: classNameInside),
            new Pattern(@"(^|[^$\w\xA0-\uFFFF])(?!\s)[_$A-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?=\.(?:constructor|prototype))", lookbehind: true)
        };

        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"(^|[^.]|\.\.\.\s*)\busing(?=\s+(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*\s*(?:=(?!=)|\bof\b))", lookbehind: true),
            new Pattern(@"(^|[^.]|\.\.\.\s*)\b(?:as|assert(?=\s*\{)|export|from(?=\s*(?:['""]|$))|import)\b", lookbehind: true, alias: "module"),
            new Pattern(@"((?:^|\})\s*)catch\b", lookbehind: true, alias: "control-flow"),
            new Pattern(@"(^|[^.]|\.\.\.\s*)\b(?:await|break|case|continue|default|do|else|finally(?=\s*(?:\{|$))|for|if|return|switch|throw|try|while|yield)\b", lookbehind: true, alias: "control-flow"),
            new Pattern(@"(^|[^.]|\.\.\.\s*)\b(?:async(?=\s*(?:function\b|\(|[$\w\xA0-\uFFFF]|$))|class|const|debugger|delete|enum|extends|function|(?:get|set)(?=\s*(?:[#\[$\w\xA0-\uFFFF]|$))|implements|in|instanceof|interface|let|new|null|of|package|private|protected|public|static|super|this|typeof|undefined|var|void|with)\b", lookbehind: true)
        };

        // Allow for all non-ASCII characters (see Prism javascript.js)
        grammar["function"] = new List<Pattern>
        {
            new Pattern(@"#?(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?=\s*(?:\.\s*(?:apply|bind|call)\s*)?\()")
        };

        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"(^|[^\w$])(?:NaN|Infinity|0[bB][01]+(?:_[01]+)*n?|0[oO][0-7]+(?:_[0-7]+)*n?|0[xX][\dA-Fa-f]+(?:_[\dA-Fa-f]+)*n?|\d+(?:_\d+)*n|(?:\d+(?:_\d+)*(?:\.(?:\d+(?:_\d+)*)?)?|\.\d+(?:_\d+)*)(?:[Ee][+-]?\d+(?:_\d+)*)?)(?![\w$])", lookbehind: true)
        };

        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"--|\+\+|\*\*=?|=>|&&=?|\|\|=?|[!=]==|<<=?|>>>?=?|[-+*/%&|^!=<>]=?|\.{3}|\?\?=?|\?\.?|[~:]")
        };

        grammar.InsertBefore("comment", new Grammar
        {
            { "doc-comment", new List<Pattern>
                {
                    new Pattern(@"\/\*\*(?!\/)[\s\S]*?(?:\*\/|$)", greedy: true, alias: "comment")
                }
            }
        });

        var regexInside = new Grammar();
        regexInside.Add("regex-source", new Pattern(@"^(\/)[\s\S]+(?=\/[a-z]*$)", lookbehind: true, alias: "language-regex", inside: new RegexLanguage().GetGrammar()));
        regexInside.Add("regex-delimiter", new Pattern(@"^\/|\/$"));
        regexInside.Add("regex-flags", new Pattern(@"^[a-z]+$"));

        grammar.InsertBefore("keyword", new Grammar
        {
            { "regex", new List<Pattern>
                {
                    new Pattern(@"((?:^|[^$\w\xA0-\uFFFF.""'\])\s]|\b(?:return|yield))\s*)\/(?:(?:\[(?:[^\]\\\r\n]|\\.)*\]|\\.|[^/\\\[\r\n])+\/[dgimyus]{0,7}|(?:\[(?:[^[\]\\\r\n]|\\.|\[(?:[^[\]\\\r\n]|\\.|\[(?:[^[\]\\\r\n]|\\.)*\])*\])*\]|\\.|[^/\\\[\r\n])+\/[dgimyus]{0,7}v[dgimyus]{0,7})(?=(?:\s|\/\*(?:[^*]|\*(?!\/))*\*\/)*(?:$|[\r\n,.;:})\]]|\/\/))",
                        lookbehind: true, greedy: true, inside: regexInside)
                }
            },
            { "function-variable", new List<Pattern>
                {
                    new Pattern(@"#?(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?=\s*[=:]\s*(?:async\s*)?(?:\bfunction\b|(?:\((?:[^()]|\([^()]*\))*\)|(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*)\s*=>))", alias: "function")
                }
            },
            { "parameter", new List<Pattern>
                {
                    new Pattern(@"(function(?:\s+(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*)?\s*\(\s*)(?!\s)(?:[^()\s]|\s+(?![\s)])|\([^()]*\))+(?=\s*\))", lookbehind: true, inside: grammar),
                    new Pattern(@"(^|[^$\w\xA0-\uFFFF])(?!\s)[_$a-z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?=\s*=>)", regexOptions: "i", lookbehind: true, inside: grammar),
                    new Pattern(@"(\(\s*)(?!\s)(?:[^()\s]|\s+(?![\s)])|\([^()]*\))+(?=\s*\)\s*=>)", lookbehind: true, inside: grammar),
                    new Pattern(@"((?:\b|\s|^)(?!(?:as|async|await|break|case|catch|class|const|continue|debugger|default|delete|do|else|enum|export|extends|finally|for|from|function|get|if|implements|import|in|instanceof|interface|let|new|null|of|package|private|protected|public|return|set|static|super|switch|this|throw|try|typeof|undefined|var|void|while|with|yield)(?![$\w\xA0-\uFFFF]))(?:(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*\s*)\(\s*|\]\s*\(\s*)(?!\s)(?:[^()\s]|\s+(?![\s)])|\([^()]*\))+(?=\s*\)\s*\{)", lookbehind: true, inside: grammar)
                }
            },
            { "constant", new List<Pattern> { new Pattern(@"\b[A-Z](?:[A-Z_]|\dx?)*\b") } }
        });

        // Template string with ${...} interpolation; the interpolation body is
        // highlighted with the full JavaScript grammar (Prism's $rest), which is
        // appended to interpolationInside after this grammar is fully built.
        var interpolationInside = new Grammar();
        interpolationInside.Add("interpolation-punctuation", new Pattern(@"^\$\{|\}$", alias: "punctuation"));

        var templateInside = new Grammar();
        templateInside.Add("template-punctuation", new Pattern(@"^`|`$", alias: "string"));
        templateInside.Add("interpolation", new Pattern(@"((?:^|[^\\])(?:\\{2})*)\$\{(?:[^{}]|\{(?:[^{}]|\{[^{}]*\})*\})+\}", lookbehind: true, inside: interpolationInside));
        templateInside.Add("string", new Pattern(@"[\s\S]+"));

        grammar.InsertBefore("string", new Grammar
        {
            { "hashbang", new List<Pattern> { new Pattern(@"^#!.*", greedy: true, alias: "comment") } },
            { "template-string", new List<Pattern>
                {
                    new Pattern(@"`(?:\\[\s\S]|\$\{(?:[^{}]|\{(?:[^{}]|\{[^{}]*\})*\})+\}|[^\\`$]|\$(?!\{))*`", greedy: true, inside: templateInside)
                }
            },
            { "string-property", new List<Pattern>
                {
                    new Pattern(@"((?:^|[,{])[ \t]*)([""'])(?:\\(?:\r\n|[\s\S])|(?!\2)[^\\\r\n])*\2(?=\s*:)", regexOptions: "m", lookbehind: true, greedy: true, alias: "property")
                }
            }
        });

        grammar.InsertBefore("operator", new Grammar
        {
            { "literal-property", new List<Pattern>
                {
                    new Pattern(@"((?:^|[,{])[ \t]*)(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?=\s*:)", regexOptions: "m", lookbehind: true, alias: "property")
                }
            }
        });

        // Prism's $rest: 'javascript' inside template interpolations.
        foreach (var kvp in grammar)
        {
            if (!interpolationInside.ContainsKey(kvp.Key))
            {
                interpolationInside[kvp.Key] = kvp.Value;
            }
        }

        return grammar;
    }
}
