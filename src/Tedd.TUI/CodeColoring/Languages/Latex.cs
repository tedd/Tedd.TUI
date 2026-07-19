using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class LatexLanguage : ILanguage
{
    public string Id => "latex";
    public string[] Aliases => ["tex", "context"];

    public Grammar GetGrammar()
    {
        string funcPattern = @"\\(?:[^a-z()[\]]|[a-z*]+)";

        var insideEqu = new Grammar();
        insideEqu.Add("equation-command", new Pattern(funcPattern, regexOptions: "i", alias: "regex"));

        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"%.*"));
        // the verbatim environment prints whitespace to the document
        grammar.Add("cdata", new Pattern(@"(\\begin\{((?:lstlisting|verbatim)\*?)\})[\s\S]*?(?=\\end\{\2\})", lookbehind: true));
        // equations can be between $$ $$ or $ $ or \( \) or \[ \]
        grammar.Add("equation", new List<Pattern>
        {
            new Pattern(@"\$\$(?:\\[\s\S]|[^\\$])+\$\$|\$(?:\\[\s\S]|[^\\$])+\$|\\\([\s\S]*?\\\)|\\\[[\s\S]*?\\\]", inside: insideEqu, alias: "string"),
            new Pattern(@"(\\begin\{((?:align|eqnarray|equation|gather|math|multline)\*?)\})[\s\S]*?(?=\\end\{\2\})", lookbehind: true, inside: insideEqu, alias: "string")
        });
        // arguments which are keywords or references are highlighted as keywords
        grammar.Add("keyword", new Pattern(@"(\\(?:begin|cite|documentclass|end|label|ref|usepackage)(?:\[[^\]]+\])?\{)[^}]+(?=\})", lookbehind: true));
        grammar.Add("url", new Pattern(@"(\\url\{)[^}]+(?=\})", lookbehind: true));
        // section or chapter headlines stand out more
        grammar.Add("headline", new Pattern(@"(\\(?:chapter|frametitle|paragraph|part|section|subparagraph|subsection|subsubparagraph|subsubsection|subsubsubparagraph)\*?(?:\[[^\]]+\])?\{)[^}]+(?=\})", lookbehind: true, alias: "class-name"));
        grammar.Add("function", new Pattern(funcPattern, regexOptions: "i", alias: "selector"));
        grammar.Add("punctuation", new Pattern(@"[[\]{}&]"));
        return grammar;
    }
}
