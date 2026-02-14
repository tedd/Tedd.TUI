using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class AspNetLanguage : ILanguage
{
    public string Id => "aspnet";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var markup = new MarkupLanguage().GetGrammar();
        var csharp = new CSharpLanguage().GetGrammar();

        var pageDirectiveInside = new Grammar();
        pageDirectiveInside.Add("page-directive", new Pattern(@"<%\s*@\s*(?:Assembly|Control|Implements|Import|Master(?:Type)?|OutputCache|Page|PreviousPageType|Reference|Register)?|%>", regexOptions: "i", alias: "tag"));

        if (markup.ContainsKey("tag") && markup["tag"][0].Inside != null)
        {
             foreach(var kvp in markup["tag"][0].Inside!)
             {
                 if (!pageDirectiveInside.ContainsKey(kvp.Key))
                    pageDirectiveInside[kvp.Key] = kvp.Value;
             }
        }

        var directiveInside = new Grammar();
        directiveInside.Add("directive", new Pattern(@"<%\s*?[$=%#:]{0,2}|%>", alias: "tag"));
        foreach(var kvp in csharp)
        {
             if (!directiveInside.ContainsKey(kvp.Key))
                directiveInside[kvp.Key] = kvp.Value;
        }

        var grammar = Grammar.Extend(markup, new Grammar
        {
            { "page-directive", new List<Pattern> { new Pattern(@"<%\s*@.*%>", alias: "tag", inside: pageDirectiveInside) } },
            { "directive", new List<Pattern> { new Pattern(@"<%.*%>", alias: "tag", inside: directiveInside) } }
        });

        if (grammar.ContainsKey("tag"))
        {
            grammar["tag"][0].Regex = new System.Text.RegularExpressions.Regex(@"<(?!%)\/?[^\s>\/]+(?:\s+[^\s>\/=]+(?:=(?:(""|')(?:\\[\s\S]|(?!\1)[^\\])*\1|[^\s'"">=]+))?)*\s*\/?>");
        }

        if (grammar.ContainsKey("tag"))
        {
             var tagToken = grammar["tag"][0];
             if (tagToken.Inside != null && tagToken.Inside.TryGetValue("attr-value", out var attrValues))
             {
                 var attrValueInside = attrValues[0].Inside;
                 if (attrValueInside != null)
                 {
                     attrValueInside.InsertBefore("punctuation", new Grammar
                     {
                         { "directive", grammar["directive"] }
                     });
                 }
             }
        }

        grammar.InsertBefore("comment", new Grammar
        {
            { "asp-comment", new List<Pattern> { new Pattern(@"<%--[\s\S]*?--%>", alias: "asp comment") } }
        });

        grammar.InsertBefore("tag", new Grammar
        {
            { "asp-script", new List<Pattern> { new Pattern(@"(<script(?=.*runat=['""]?server\b)[^>]*>)[\s\S]*?(?=<\/script>)", regexOptions: "i", lookbehind: true, alias: "asp script", inside: csharp) } }
        });

        return grammar;
    }
}
