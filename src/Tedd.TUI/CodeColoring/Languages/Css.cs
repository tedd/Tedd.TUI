using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class CssLanguage : ILanguage
{
    public string Id => "css";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        string stringPattern = "(?:\"(?:\\\\(?:\\r\\n|[\\s\\S])|[^\"\\\\\\r\\n])*\"|'(?:\\\\(?:\\r\\n|[\\s\\S])|[^'\\\\\\r\\n])*')";

        grammar.Add("comment", new Pattern(@"\/\*[\s\S]*?\*\/"));

        var atRuleInside = new Grammar();
        atRuleInside.Add("rule", new Pattern(@"^@[\w-]+"));
        atRuleInside.Add("selector-function-argument", new Pattern(@"(\bselector\s*\(\s*(?![\s)]))(?:[^()\s]|\s+(?![\s)])|\((?:[^()]|\([^()]*\))*\))+(?=\s*\))", lookbehind: true, alias: "selector"));
        atRuleInside.Add("keyword", new Pattern(@"(^|[^\w-])(?:and|not|only|or)(?![\w-])", lookbehind: true));

        grammar.Add("atrule", new Pattern(Replace(@"@[\w-](?:[^;{\s""']|\s+(?!\s)|<<0>>)*?(?:;|(?=\s*\{))", stringPattern), inside: atRuleInside));

        var urlInside = new Grammar();
        urlInside.Add("function", new Pattern(@"^url", regexOptions: "i"));
        urlInside.Add("punctuation", new Pattern(@"^\(|\)$"));
        urlInside.Add("string", new Pattern(Replace(@"^<<0>>$", stringPattern), alias: "url"));

        grammar.Add("url", new Pattern(Replace(@"\burl\((?:<<0>>|(?:[^\\\r\n()""']|\\[\s\S])*)\)", stringPattern), regexOptions: "i", greedy: true, inside: urlInside));

        grammar.Add("selector", new Pattern(Replace(@"(^|[{}\s])[^{}\s](?:[^{};""'\s]|\s+(?![\s{])|<<0>>)*(?=\s*\{)", stringPattern), lookbehind: true));

        grammar.Add("string", new Pattern(stringPattern, greedy: true));
        grammar.Add("property", new Pattern(@"(^|[^-\w\xA0-\uFFFF])(?!\s)[-_a-z\xA0-\uFFFF](?:(?!\s)[-\w\xA0-\uFFFF])*(?=\s*:)", regexOptions: "i", lookbehind: true));
        grammar.Add("important", new Pattern(@"!important\b", regexOptions: "i"));
        grammar.Add("function", new Pattern(@"(^|[^-a-z0-9])[-a-z0-9]+(?=\()", regexOptions: "i", lookbehind: true));
        grammar.Add("punctuation", new Pattern(@"[(){};:,]"));

        // Copy grammar to atRuleInside
        foreach (var kvp in grammar)
        {
            if (!atRuleInside.ContainsKey(kvp.Key))
                atRuleInside[kvp.Key] = kvp.Value;
        }

        return grammar;
    }
}
