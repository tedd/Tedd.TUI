using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class PrologLanguage : ILanguage
{
    public string Id => "prolog";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"\/\*[\s\S]*?\*\/|%.*", greedy: true));
        // Depending on the implementation, strings may allow escaped newlines and quote-escape
        grammar.Add("string", new Pattern(@"([""'])(?:\1\1|\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1(?!\1)", greedy: true));
        grammar.Add("builtin", new Pattern(@"\b(?:fx|fy|xf[xy]?|yfx?)\b"));
        grammar.Add("function", new Pattern(@"\b[a-z]\w*(?:(?=\()|\/\d+)"));
        grammar.Add("number", new Pattern(@"\b\d+(?:\.\d*)?"));
        // Custom operators are allowed
        grammar.Add("operator", new Pattern(@"[:\\=><\-?*@\/;+^|!$.]+|\b(?:is|mod|not|xor)\b"));
        grammar.Add("punctuation", new Pattern(@"[(){}\[\],]"));
        return grammar;
    }
}
