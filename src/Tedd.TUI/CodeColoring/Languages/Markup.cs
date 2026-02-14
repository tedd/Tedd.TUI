using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public static class Markup
{
    public static Grammar GetGrammar()
    {
        var grammar = new Grammar();

        // Comment
        grammar.Add("comment", new Pattern(@"<!--(?:(?!<!--)[\s\S])*?-->", greedy: true));

        // Prolog
        grammar.Add("prolog", new Pattern(@"<\?[\s\S]+?\?>", greedy: true));

        // Doctype
        var doctypeInside = new Grammar();
        doctypeInside.Add("internal-subset", new Pattern(@"(^[^\[]*\[)[\s\S]+(?=\]>$)", lookbehind: true, greedy: true, inside: grammar)); // Circular reference via 'grammar' variable (captured closure)
        // Wait, 'grammar' is not fully populated yet. But reference is to the object, so it's fine if we use the object.
        // However, 'grammar' is a Dictionary. 'inside' property holds a reference to a Grammar.
        // So passing 'grammar' here works, provided we don't copy it later incorrectly.
        // But wait, `Prism.languages.markup['doctype'].inside['internal-subset'].inside = Prism.languages.markup;`
        // Yes, it refers to the root markup grammar.

        doctypeInside.Add("string", new Pattern(@"""[^""]*""|'[^']*'", greedy: true));
        doctypeInside.Add("punctuation", new Pattern(@"^<!|>$|[[\]]"));
        doctypeInside.Add("doctype-tag", new Pattern(@"^DOCTYPE", regexOptions: "i"));
        doctypeInside.Add("name", new Pattern(@"[^\s<>'""+]+"));

        grammar.Add("doctype", new Pattern(@"<!DOCTYPE(?:[^>""'[\]]|""[^""]*""|'[^']*')+(?:\[(?:[^<""'\]]|""[^""]*""|'[^']*'|<(?!!--)|<!--(?:[^-]|-(?!->))*-->)*\]\s*)?>",
            regexOptions: "i", greedy: true, inside: doctypeInside));

        // CDATA
        grammar.Add("cdata", new Pattern(@"<!\[CDATA\[[\s\S]*?\]\]>", regexOptions: "i", greedy: true));

        // Tag
        var tagInside = new Grammar();

        var tagTagInside = new Grammar();
        tagTagInside.Add("punctuation", new Pattern(@"^<\/?"));
        tagTagInside.Add("namespace", new Pattern(@"^[^\s>\/:]+:"));

        tagInside.Add("tag", new Pattern(@"^<\/?(?!\d)[^\s>\/=$<%]+", inside: tagTagInside)); // Simplified from JS slightly?
        // JS: /^<\/?[^\s>\/]+/

        tagInside.Add("special-attr", new List<Pattern>()); // Empty initially

        var attrValueInside = new Grammar();
        attrValueInside.Add("punctuation", new List<Pattern>
        {
            new Pattern(@"^=", alias: "attr-equals"),
            new Pattern(@"^(\s*)[""']|[""']$", lookbehind: true)
        });

        // Entity reference in attr-value
        // We need to define Entity first to refer to it.
        // Or define it later and inject?
        // Prism does: `Prism.languages.markup['tag'].inside['attr-value'].inside['entity'] = Prism.languages.markup['entity'];`
        // We can create the pattern object for Entity and reuse it.

        var entityPatterns = new List<Pattern>
        {
            new Pattern(@"&[\da-z]{1,8};", regexOptions: "i", alias: "named-entity"),
            new Pattern(@"&#x?[\da-f]{1,8};", regexOptions: "i")
        };

        attrValueInside.Add("entity", entityPatterns);

        tagInside.Add("attr-value", new Pattern(@"=\s*(?:""[^""]*""|'[^']*'|[^\s'"">=]+)", inside: attrValueInside));

        tagInside.Add("punctuation", new Pattern(@"\/?>"));

        var attrNameInside = new Grammar();
        attrNameInside.Add("namespace", new Pattern(@"^[^\s>\/:]+:"));

        tagInside.Add("attr-name", new Pattern(@"[^\s>\/]+", inside: attrNameInside));

        grammar.Add("tag", new Pattern(@"<\/?(?!\d)[^\s>\/=$<%]+(?:\s(?:\s*[^\s>\/=]+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s'"">=]+(?=[\s>]))|(?=[\s/>])))+)?\s*\/?>",
            greedy: true, inside: tagInside));

        // Entity
        grammar.Add("entity", entityPatterns);

        return grammar;
    }
}
