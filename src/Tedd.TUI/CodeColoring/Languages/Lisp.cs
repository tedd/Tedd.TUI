using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class LispLanguage : ILanguage
{
    public string Id => "lisp";
    public string[] Aliases => ["emacs", "elisp", "emacs-lisp"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        // Symbol name; & and : are excluded as they are usually used for special purposes
        string symbol = @"(?!\d)[-+*/~!@$%^=<>{}\w]+";
        // symbol starting with & used in function arguments
        string marker = "&" + symbol;
        // Open parenthesis for look-behind
        string par = @"(\()";
        string endpar = @"(?=\))";
        // End the pattern with look-ahead space
        string space = @"(?=\s)";
        string nestedPar = @"(?:[^()]|\((?:[^()]|\((?:[^()]|\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\))*\))*\))*";
        string forms = @"\S+(?:\s+\S+)*";

        // Function arguments: markers, varforms, and plain arguments; the full
        // lisp grammar is appended afterwards (Prism's $rest).
        var argInside = new Grammar();
        argInside.Add("lisp-marker", new Pattern(marker));
        argInside.Add("varform", new Pattern(@"\(" + symbol + @"\s+(?=\S)" + nestedPar + @"\)", inside: grammar));
        argInside.Add("argument", new Pattern(@"(^|[\s(])" + symbol, lookbehind: true, alias: "variable"));

        var arglistInside = new Grammar();
        arglistInside.Add("rest-vars", new Pattern(@"&(?:body|rest)\s+" + forms, inside: argInside));
        arglistInside.Add("other-marker-vars", new Pattern(@"&(?:aux|optional)\s+" + forms, inside: argInside));
        arglistInside.Add("keys", new Pattern(@"&key\s+" + forms + @"(?:\s+&allow-other-keys)?", inside: argInside));
        arglistInside.Add("argument", new Pattern(symbol, alias: "variable"));
        arglistInside.Add("punctuation", new Pattern(@"[()]"));

        var arglist = new Pattern(par + nestedPar + endpar, lookbehind: true, inside: arglistInside);

        // arglist with one level of sublists (for defun/defmacro argument lists)
        var argumentsInside = new Grammar();
        foreach (var kvp in arglistInside)
        {
            argumentsInside[kvp.Key] = kvp.Value;
        }
        argumentsInside["sublist"] = new List<Pattern> { arglist };
        var argumentsPattern = new Pattern(par + nestedPar + endpar, lookbehind: true, inside: argumentsInside);

        // Three or four semicolons are considered a heading.
        grammar.Add("heading", new Pattern(@";;;.*", alias: "comment"));
        grammar.Add("comment", new Pattern(@";.*"));

        var stringInside = new Grammar();
        stringInside.Add("argument", new Pattern(@"[-A-Z]+(?=[.,\s])"));
        stringInside.Add("symbol", new Pattern("`" + symbol + "'"));
        grammar.Add("string", new Pattern(@"""(?:[^""\\]|\\.)*""", greedy: true, inside: stringInside));

        grammar.Add("quoted-symbol", new Pattern("#?'" + symbol, alias: "symbol"));
        grammar.Add("lisp-property", new Pattern(":" + symbol, alias: "property"));
        grammar.Add("splice", new Pattern(",@?" + symbol, alias: "symbol"));

        grammar.Add("keyword", new List<Pattern>
        {
            new Pattern(par + @"(?:and|(?:cl-)?letf|cl-loop|cond|cons|error|if|(?:lexical-)?let\*?|message|not|null|or|provide|require|setq|unless|use-package|when|while)" + space, lookbehind: true),
            new Pattern(par + @"(?:append|by|collect|concat|do|finally|for|in|return)" + space, lookbehind: true)
        });

        grammar.Add("declare", new Pattern(par + @"(?:declare)(?=[\s\)])", lookbehind: true, alias: "keyword"));
        grammar.Add("interactive", new Pattern(par + @"(?:interactive)(?=[\s\)])", lookbehind: true, alias: "keyword"));
        grammar.Add("boolean", new Pattern(@"([\s([])(?:nil|t)(?=[\s)])", lookbehind: true));
        grammar.Add("number", new Pattern(@"([\s([])(?:[-+]?\d+(?:\.\d*)?)(?=[\s)])", lookbehind: true));

        var defvarInside = new Grammar();
        defvarInside.Add("keyword", new Pattern(@"^def[a-z]+"));
        defvarInside.Add("variable", new Pattern(symbol));
        grammar.Add("defvar", new Pattern(par + @"def(?:const|custom|group|var)\s+" + symbol, lookbehind: true, inside: defvarInside));

        var defunInside = new Grammar();
        defunInside.Add("keyword", new Pattern(@"^(?:cl-)?def\S+"));
        defunInside.Add("arguments", argumentsPattern);
        defunInside.Add("function", new Pattern(@"(^\s)" + symbol, lookbehind: true));
        defunInside.Add("punctuation", new Pattern(@"[()]"));
        grammar.Add("defun", new Pattern(par + @"(?:cl-)?(?:defmacro|defun\*?)\s+" + symbol + @"\s+\(" + nestedPar + @"\)", lookbehind: true, greedy: true, inside: defunInside));

        var lambdaInside = new Grammar();
        lambdaInside.Add("keyword", new Pattern(@"^lambda"));
        lambdaInside.Add("arguments", arglist);
        lambdaInside.Add("punctuation", new Pattern(@"[()]"));
        grammar.Add("lambda", new Pattern(par + @"lambda\s+\(\s*(?:&?" + symbol + @"(?:\s+&?" + symbol + @")*\s*)?\)", lookbehind: true, greedy: true, inside: lambdaInside));

        grammar.Add("car", new Pattern(par + symbol, lookbehind: true));
        grammar.Add("punctuation", new List<Pattern>
        {
            new Pattern(@"(?:['`,]?\(|[)\[\]])"),
            new Pattern(@"(\s)\.(?=\s)", lookbehind: true)
        });

        // Prism's $rest: 'lisp' inside argument lists.
        foreach (var kvp in grammar)
        {
            if (!argInside.ContainsKey(kvp.Key))
            {
                argInside[kvp.Key] = kvp.Value;
            }
        }

        return grammar;
    }
}
