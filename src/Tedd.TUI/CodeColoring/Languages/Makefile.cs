using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class MakefileLanguage : ILanguage
{
    public string Id => "makefile";
    public string[] Aliases => ["make"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"(^|[^\\])#(?:\\(?:\r\n|[\s\S])|[^\\\r\n])*", lookbehind: true));
        grammar.Add("string", new Pattern(@"([""'])(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1", greedy: true));
        grammar.Add("builtin-target", new Pattern(@"\.[A-Z][^:#=\s]+(?=\s*:(?!=))", alias: "builtin"));

        var targetInside = new Grammar();
        targetInside.Add("variable", new Pattern(@"\$+(?:(?!\$)[^(){}:#=\s]+|(?=[({]))"));
        grammar.Add("target", new Pattern(@"^(?:[^:=\s]|[ \t]+(?![\s:]))+(?=\s*:(?!=))", regexOptions: "m", alias: "symbol", inside: targetInside));

        grammar.Add("variable", new Pattern(@"\$+(?:(?!\$)[^(){}:#=\s]+|\([@*%<^+?][DF]\)|(?=[({]))"));
        // Directives
        grammar.Add("keyword", new Pattern(@"-include\b|\b(?:define|else|endef|endif|export|ifn?def|ifn?eq|include|override|private|sinclude|undefine|unexport|vpath)\b"));
        grammar.Add("function", new Pattern(@"(\()(?:abspath|addsuffix|and|basename|call|dir|error|eval|file|filter(?:-out)?|findstring|firstword|flavor|foreach|guile|if|info|join|lastword|load|notdir|or|origin|patsubst|realpath|shell|sort|strip|subst|suffix|value|warning|wildcard|word(?:list|s)?)(?=[ \t])", lookbehind: true));
        grammar.Add("operator", new Pattern(@"(?:::|[?:+!])?=|[|@]"));
        grammar.Add("punctuation", new Pattern(@"[:;(){}]"));
        return grammar;
    }
}
