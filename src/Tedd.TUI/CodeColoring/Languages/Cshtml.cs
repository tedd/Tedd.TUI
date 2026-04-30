using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class CshtmlLanguage : ILanguage
{
    public string Id => "cshtml";
    public string[] Aliases => [ "razor" ];

    public Grammar GetGrammar()
    {
        var markup = new MarkupLanguage().GetGrammar();
        var csharp = new CSharpLanguage().GetGrammar();

        // Helper regex patterns
        string commentLike = @"\/(?![/*])|\/\/.*[\r\n]|\/\*[^*]*(?:\*(?!\/)[^*]*)*\*\/";
        string stringLike = @"@(?!"""")|""(?:[^\r\n\\""]|\\.)*""|@""(?:[^\\""]|""""|\\[\s\S])*""(?!"")|'(?:(?:[^\r\n'\\]|\\.|\\[Uux][\da-fA-F]{1,8})'|(?=[^\\](?!')))";

        string nested(string pattern, int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                pattern = pattern.Replace("<self>", "(?:" + pattern + ")");
            }
            return pattern
                .Replace("<self>", "[^\\s\\S]")
                .Replace("<str>", "(?:" + stringLike + ")")
                .Replace("<comment>", "(?:" + commentLike + ")");
        }

        string round = nested(@"\((?:[^()""'@/]|<str>|<comment>|<self>)*\)", 2);
        string square = nested(@"\[(?:[^\[\]""'@/]|<str>|<comment>|<self>)*\]", 1);
        string curly = nested(@"\{(?:[^{}""'@/]|<str>|<comment>|<self>)*\}", 2);
        string angle = nested(@"<(?:[^<>""'@/]|<comment>|<self>)*>", 1);

        string inlineCs = @"@(?!"""")" +
            @"(?:await\b\s*)?" +
            @"(?:(?!" + @"await\b)\w+\b|" + round + @")" +
            @"(?:[?!]?\.\w+\b|(?:" + angle + @")?" + round + @"|" + square + @")*" +
            @"(?![?!\.(\[]|<(?!\/))";

        string tagAttrInlineCs = @"@(?![\w()])|" + inlineCs;
        string tagAttrValue = @"(?:" +
            @"""[^""@]*""|'[^'@]*'|[^\s'""@>=]+(?=[\s>])" +
            @"|" +
            @"[""'][^""'@]*" + "(?:(?:" + tagAttrInlineCs + @")[^""'@]*)+[""']" +
            @")";

        string tagAttrs = @"(?:\s(?:\s*[^\s>\/=]+(?:\s*=\s*<tagAttrValue>|(?=[\s/>])))+)?".Replace("<tagAttrValue>", tagAttrValue);
        string tagContent = @"(?!\d)[^\s>\/=$<%]+" + tagAttrs + @"\s*\/?>";

        string tagRegion = @"\B@?" +
            @"(?:" +
            @"<([a-zA-Z][\w:]*)" + tagAttrs + @"\s*>" +
            @"(?:" +
            @"(" +
                @"[^<]" +
                @"|" +
                @"<\/?(?!\1\b)" + tagContent +
                @"|" +
                nested(@"<\1" + tagAttrs + @"\s*>" + @"(?:" + @"(" + @"[^<]" + @"|" + @"<\/?(?!\1\b)" + tagContent + @"|" + @"<self>" + @")" + @")*" + @"<\/\1\s*>", 2) +
            @")" +
            @")*" +
            @"<\/\1\s*>" +
            @"|" +
            @"<" + tagContent +
            @")";

        var csharpWithHtml = new CSharpLanguage().GetGrammar();
        csharpWithHtml.InsertBefore("string", new Grammar
        {
            { "html", new List<Pattern> { new Pattern(tagRegion, greedy: true, inside: markup) } }
        });

        var cs = new Grammar();
        cs.Add("csharp", new Pattern(@"[\s\S]+", alias: "language-csharp", inside: csharpWithHtml));

        var inlineValue = new Grammar();
        inlineValue.Add("value", new Pattern(Replace(@"(^|[^@])<<0>>", inlineCs), lookbehind: true, greedy: true, alias: "variable", inside: new Grammar
        {
            { "keyword", new List<Pattern> { new Pattern(@"^@") } },
            { "csharp", new List<Pattern> { new Pattern(@"[\s\S]+", alias: "language-csharp", inside: csharpWithHtml) } }
        }));

        var grammar = Grammar.Extend(markup, new Grammar());

        if (grammar.ContainsKey("tag"))
        {
            grammar["tag"][0].Regex = new System.Text.RegularExpressions.Regex(@"<\/?(?!\d)[^\s>\/=$<%]+" + tagContent);
            if (grammar["tag"][0].Inside != null && grammar["tag"][0].Inside.ContainsKey("attr-value"))
            {
                grammar["tag"][0].Inside["attr-value"][0].Regex = new System.Text.RegularExpressions.Regex(@"=\s*" + tagAttrValue);

                grammar["tag"][0].Inside["attr-value"][0].Inside.InsertBefore("punctuation", new Grammar
                {
                    { "value", inlineValue["value"] }
                });
            }
        }

        grammar.InsertBefore("prolog", new Grammar
        {
            { "razor-comment", new List<Pattern> { new Pattern(@"@\*[\s\S]*?\*@", greedy: true, alias: "comment") } },
            { "block", new List<Pattern>
                {
                    new Pattern(Replace(@"(^|[^@])@(?:<<0>>|<<1>>|<<2>>|<<3>>|<<4>>|<<5>>|<<6>>)",
                        curly,
                        @"(?:code|functions)\s*" + curly,
                        @"(?:for|foreach|lock|switch|using|while)\s*" + round + @"\s*" + curly,
                        @"do\s*" + curly + @"\s*while\s*" + round + @"(?:\s*;)?",
                        @"try\s*" + curly + @"\s*catch\s*" + round + @"\s*" + curly + @"\s*finally\s*" + curly,
                        @"if\s*" + round + @"\s*" + curly + @"(?:" + @"\s*else" + @"(?:" + @"\s+if\s*" + round + @")?" + @"\s*" + curly + @")*",
                        @"helper\s+\w+\s*" + round + @"\s*" + curly
                    ), lookbehind: true, greedy: true, inside: new Grammar
                    {
                        { "keyword", new List<Pattern> { new Pattern(@"^@\w*") } },
                        { "csharp", new List<Pattern> { new Pattern(@"[\s\S]+", alias: "language-csharp", inside: csharpWithHtml) } }
                    })
                }
            },
            { "directive", new List<Pattern>
                {
                    new Pattern(@"^([ \t]*)@(?:addTagHelper|attribute|implements|inherits|inject|layout|model|namespace|page|preservewhitespace|removeTagHelper|section|tagHelperPrefix|using)(?=\s).*", regexOptions: "m", lookbehind: true, greedy: true, inside: new Grammar
                    {
                        { "keyword", new List<Pattern> { new Pattern(@"^@\w+") } },
                        { "csharp", new List<Pattern> { new Pattern(@"[\s\S]+", alias: "language-csharp", inside: csharpWithHtml) } }
                    })
                }
            },
            { "value", inlineValue["value"] },
            { "delegate-operator", new List<Pattern> { new Pattern(@"(^|[^@])@(?=<)", lookbehind: true, alias: "operator") } }
        });

        if (csharpWithHtml.ContainsKey("html"))
        {
            csharpWithHtml["html"][0].Inside = grammar;
        }

        return grammar;
    }
}
