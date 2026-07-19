using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class TypeScriptLanguage : ILanguage
{
    public string Id => "typescript";
    public string[] Aliases => ["ts"];

    public Grammar GetGrammar()
    {
        var js = new JavaScriptLanguage().GetGrammar();
        var grammar = Grammar.Extend(js, new Grammar());

        // A version of the grammar specifically for highlighting types; filled after
        // the main grammar's overrides so it mirrors it minus class-name.
        var typeInside = new Grammar();

        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(@"(\b(?:class|extends|implements|instanceof|interface|new|type)\s+)(?!keyof\b)(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*(?:\s*<(?:[^<>]|<(?:[^<>]|<[^<>]*>)*>)*>)?",
                lookbehind: true, greedy: true, inside: typeInside)
        };

        grammar["builtin"] = new List<Pattern>
        {
            new Pattern(@"\b(?:Array|Function|Promise|any|boolean|console|never|number|string|symbol|unknown)\b")
        };

        // The keywords TypeScript adds to JavaScript
        var keywords = new List<Pattern>(grammar["keyword"])
        {
            new Pattern(@"\b(?:abstract|declare|is|keyof|out|readonly|require|satisfies)\b"),
            // keywords that have to be followed by an identifier
            new Pattern(@"\b(?:asserts|infer|interface|module|namespace|type)\b(?=\s*(?:[{_$a-zA-Z\xA0-\uFFFF]|$))"),
            // This is for `import type *, {}`
            new Pattern(@"\btype\b(?=\s*(?:[\{*]|$))")
        };
        grammar["keyword"] = keywords;

        // doesn't work with TS because TS is too complex
        grammar.Remove("parameter");
        grammar.Remove("literal-property");

        foreach (var kvp in grammar)
        {
            if (kvp.Key != "class-name")
            {
                typeInside[kvp.Key] = kvp.Value;
            }
        }

        var decoratorInside = new Grammar();
        decoratorInside.Add("at", new Pattern(@"^@", alias: "operator"));
        decoratorInside.Add("function", new Pattern(@"^[\s\S]+"));

        var genericFunctionInside = new Grammar();
        genericFunctionInside.Add("function", new Pattern(@"^#?(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*"));
        genericFunctionInside.Add("generic", new Pattern(@"<[\s\S]+", alias: "class-name", inside: typeInside));

        grammar.InsertBefore("function", new Grammar
        {
            { "decorator", new List<Pattern> { new Pattern(@"@[$\w\xA0-\uFFFF]+", inside: decoratorInside) } },
            { "generic-function", new List<Pattern>
                {
                    // e.g. foo<T extends "bar" | "baz">( ...
                    new Pattern(@"#?(?!\s)[_$a-zA-Z\xA0-\uFFFF](?:(?!\s)[$\w\xA0-\uFFFF])*\s*<(?:[^<>]|<(?:[^<>]|<[^<>]*>)*>)*>(?=\s*\()", greedy: true, inside: genericFunctionInside)
                }
            }
        });

        return grammar;
    }
}
