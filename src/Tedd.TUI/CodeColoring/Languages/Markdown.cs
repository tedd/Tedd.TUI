using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class MarkdownLanguage : ILanguage
{
    public string Id => "markdown";
    public string[] Aliases => [ "md"  ];

    public Grammar GetGrammar()
    {
        var markup = new MarkupLanguage().GetGrammar();
        var grammar = Grammar.Extend(markup, new Grammar());

        string inner = @"(?:\\.|[^\\\n\r]|(?:\n|\r\n?)(?![\r\n]))";

        string createInline(string pattern)
        {
            pattern = pattern.Replace("<inner>", inner);
            return @"((?:^|[^\\])(?:\\{2})*)(?:" + pattern + ")";
        }

        string tableCell = @"(?:\\.|``(?:[^`\r\n]|`(?!`))+``|`[^`\r\n]+`|[^\\|\r\n`])+";
        string tableRow = @"\|?__(?:\|__)+\|?(?:(?:\n|\r\n?)|(?![\s\S]))".Replace("__", tableCell);
        string tableLine = @"\|?[ \t]*:?-{3,}:?[ \t]*(?:\|[ \t]*:?-{3,}:?[ \t]*)+\|?(?:\n|\r\n?)";

        grammar.InsertBefore("prolog", new Grammar
        {
            { "front-matter-block", new List<Pattern>
                {
                    new Pattern(@"(^(?:\s*[\r\n])?)---(?!.)[\s\S]*?[\r\n]---(?!.)", lookbehind: true, greedy: true, inside: new Grammar
                    {
                        { "punctuation", new List<Pattern> { new Pattern(@"^---|---$") } },
                        { "front-matter", new List<Pattern> { new Pattern(@"\S+(?:\s+\S+)*", alias: "language-yaml", inside: new YamlLanguage().GetGrammar()) } }
                    })
                }
            },
            { "blockquote", new List<Pattern> { new Pattern(@"^>(?:[\t ]*>)*", regexOptions: "m", alias: "punctuation") } },
            { "table", new List<Pattern>
                {
                    new Pattern(@"^" + tableRow + tableLine + "(?:" + tableRow + ")*", regexOptions: "m", inside: new Grammar
                    {
                        { "table-data-rows", new List<Pattern> { new Pattern(@"^(" + tableRow + tableLine + ")(?:" + tableRow + ")*$", lookbehind: true, inside: new Grammar
                            {
                                { "table-data", new List<Pattern> { new Pattern(tableCell, inside: grammar) } }, // Recursive ref to markdown
                                { "punctuation", new List<Pattern> { new Pattern(@"\|") } }
                            })
                        } },
                        { "table-line", new List<Pattern> { new Pattern(@"^(" + tableRow + ")" + tableLine + "$", lookbehind: true, inside: new Grammar { { "punctuation", new List<Pattern> { new Pattern(@"\||:?-{3,}:?") } } }) } },
                        { "table-header-row", new List<Pattern> { new Pattern(@"^" + tableRow + "$", inside: new Grammar
                            {
                                { "table-header", new List<Pattern> { new Pattern(tableCell, alias: "important", inside: grammar) } },
                                { "punctuation", new List<Pattern> { new Pattern(@"\|") } }
                            })
                        } }
                    })
                }
            },
            { "code", new List<Pattern>
                {
                    new Pattern(@"((?:^|\n)[ \t]*\n|(?:^|\r\n?)[ \t]*\r\n?)(?: {4}|\t).+(?:(?:\n|\r\n?)(?: {4}|\t).+)*", lookbehind: true, alias: "keyword"),
                    new Pattern(@"^```[\s\S]*?^```$", regexOptions: "m", greedy: true, inside: new Grammar
                    {
                        { "code-block", new List<Pattern> { new Pattern(@"^(```.*(?:\n|\r\n?))[\s\S]+?(?=(?:\n|\r\n?)^```$)", regexOptions: "m", lookbehind: true) } },
                        { "code-language", new List<Pattern> { new Pattern(@"^(```).+", lookbehind: true) } },
                        { "punctuation", new List<Pattern> { new Pattern(@"```") } }
                    })
                }
            },
            { "title", new List<Pattern>
                {
                    new Pattern(@"\S.*(?:\n|\r\n?)(?:==+|--+)(?=[ \t]*$)", regexOptions: "m", alias: "important", inside: new Grammar { { "punctuation", new List<Pattern> { new Pattern(@"==+$|--+$") } } }),
                    new Pattern(@"(^\s*)#.+", regexOptions: "m", lookbehind: true, alias: "important", inside: new Grammar { { "punctuation", new List<Pattern> { new Pattern(@"^#+|#+$") } } })
                }
            },
            { "hr", new List<Pattern> { new Pattern(@"(^\s*)([*-])(?:[\t ]*\2){2,}(?=\s*$)", regexOptions: "m", lookbehind: true, alias: "punctuation") } },
            { "list", new List<Pattern> { new Pattern(@"(^\s*)(?:[*+-]|\d+\.)(?=[\t ].)", regexOptions: "m", lookbehind: true, alias: "punctuation") } },
            { "url-reference", new List<Pattern> { new Pattern(@"!?\[[^\]]+\]:[\t ]+(?:\S+|<(?:\\.|[^>\\])+>)(?:[\t ]+(?:""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|\((?:\\.|[^)\\])*\)))?", alias: "url", inside: new Grammar
                {
                    { "variable", new List<Pattern> { new Pattern(@"^(!?\[)[^\]]+", lookbehind: true) } },
                    { "string", new List<Pattern> { new Pattern(@"(?:""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|\((?:\\.|[^)\\])*\))$") } },
                    { "punctuation", new List<Pattern> { new Pattern(@"^[\[\]!:]|[<>]") } }
                })
            } },
            { "bold", new List<Pattern> { new Pattern(createInline(@"\b__(?:(?!_)<inner>|_(?:(?!_)<inner>)+_)+__\b|\*\*(?:(?!\*)<inner>|\*(?:(?!\*)<inner>)+\*)+\*\*"), lookbehind: true, greedy: true, inside: new Grammar
                {
                    { "content", new List<Pattern> { new Pattern(@"(^..)[\s\S]+(?=..$)", lookbehind: true, inside: new Grammar()) } }, // see below
                    { "punctuation", new List<Pattern> { new Pattern(@"\*\*|__") } }
                })
            } },
            { "italic", new List<Pattern> { new Pattern(createInline(@"\b_(?:(?!_)<inner>|__(?:(?!_)<inner>)+__)+_\b|\*(?:(?!\*)<inner>|\*\*(?:(?!\*)<inner>)+\*\*)+\*"), lookbehind: true, greedy: true, inside: new Grammar
                {
                    { "content", new List<Pattern> { new Pattern(@"(^.)[\s\S]+(?=.$)", lookbehind: true, inside: new Grammar()) } }, // see below
                    { "punctuation", new List<Pattern> { new Pattern(@"[*_]") } }
                })
            } },
            { "strike", new List<Pattern> { new Pattern(createInline(@"(~~?)(?:(?!~)<inner>)+\2"), lookbehind: true, greedy: true, inside: new Grammar
                {
                    { "content", new List<Pattern> { new Pattern(@"(^~~?)[\s\S]+(?=\1$)", lookbehind: true, inside: new Grammar()) } }, // see below
                    { "punctuation", new List<Pattern> { new Pattern(@"~~?") } }
                })
            } },
            { "code-snippet", new List<Pattern> { new Pattern(@"(^|[^\\`])(?:``[^`\r\n]+(?:`[^`\r\n]+)*``(?!`)|`[^`\r\n]+`(?!`))", lookbehind: true, greedy: true, alias: "code keyword") } },
            { "url", new List<Pattern> { new Pattern(createInline(@"!?\[(?:(?!\])<inner>)+\](?:\([^\s)]+(?:[\t ]+""(?:\\.|[^""\\])*"")?\)|[ \t]?\[(?:(?!\])<inner>)+\])"), lookbehind: true, greedy: true, inside: new Grammar
                {
                    { "operator", new List<Pattern> { new Pattern(@"^!") } },
                    { "content", new List<Pattern> { new Pattern(@"(^\[)[^\]]+(?=\])", lookbehind: true, inside: new Grammar()) } }, // see below
                    { "variable", new List<Pattern> { new Pattern(@"(^\][ \t]?\[)[^\]]+(?=\]$)", lookbehind: true) } },
                    { "url", new List<Pattern> { new Pattern(@"(^\]\()[^\s)]+", lookbehind: true) } },
                    { "string", new List<Pattern> { new Pattern(@"(^[ \t]+)""(?:\\.|[^""\\])*""(?=\)$)", lookbehind: true) } }
                })
            } }
        });

        // Resolve recursive insides
        var boldInside = grammar["bold"][0].Inside!["content"][0].Inside!;
        var italicInside = grammar["italic"][0].Inside!["content"][0].Inside!;
        var strikeInside = grammar["strike"][0].Inside!["content"][0].Inside!;
        var urlInside = grammar["url"][0].Inside!["content"][0].Inside!;

        // Add 'url', 'bold', 'italic', 'strike', 'code-snippet' to each other
        var inlineTokens = new Dictionary<string, List<Pattern>>
        {
            { "url", grammar["url"] },
            { "bold", grammar["bold"] },
            { "italic", grammar["italic"] },
            { "strike", grammar["strike"] },
            { "code-snippet", grammar["code-snippet"] }
        };

        foreach (var target in new Grammar[] { boldInside, italicInside, strikeInside, urlInside })
        {
            foreach (var kvp in inlineTokens)
            {
                // Avoid infinite recursion of same token?
                // Prism: "if (token !== inside)"
                // But here 'target' corresponds to 'inside'.
                // If target is 'boldInside', we shouldn't add 'bold' inside it?
                // Prism: "['url', 'bold', 'italic', 'strike'].forEach(function (token) { ... if (token !== inside) ... })"
                // But 'inside' variable in Prism loop is the inner grammar name.
                // For 'bold', we add everything EXCEPT 'bold'.

                // My targets are the grammar instances.
                bool skip = false;
                if (target == boldInside && kvp.Key == "bold") skip = true;
                if (target == italicInside && kvp.Key == "italic") skip = true;
                if (target == strikeInside && kvp.Key == "strike") skip = true;
                if (target == urlInside && kvp.Key == "url") skip = true;

                if (!skip)
                {
                    target.Add(kvp.Key, kvp.Value);
                }
            }
        }

        return grammar;
    }
}
