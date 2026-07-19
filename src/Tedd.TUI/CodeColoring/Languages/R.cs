using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class RLanguage : ILanguage
{
    public string Id => "r";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"#.*"));
        grammar.Add("string", new Pattern(@"(['""])(?:\\.|(?!\1)[^\\\r\n])*\1", greedy: true));
        // Includes user-defined operators and %%, %*%, %/%, %in%, %o%, %x%
        grammar.Add("percent-operator", new Pattern(@"%[^%\s]*%", alias: "operator"));
        grammar.Add("boolean", new Pattern(@"\b(?:FALSE|TRUE)\b"));
        grammar.Add("ellipsis", new Pattern(@"\.\.(?:\.|\d+)"));
        grammar.Add("number", new List<Pattern>
        {
            new Pattern(@"\b(?:Inf|NaN)\b"),
            new Pattern(@"(?:\b0x[\dA-Fa-f]+(?:\.\d*)?|\b\d+(?:\.\d*)?|\B\.\d+)(?:[EePp][+-]?\d+)?[iL]?")
        });
        grammar.Add("keyword", new Pattern(@"\b(?:NA|NA_character_|NA_complex_|NA_integer_|NA_real_|NULL|break|else|for|function|if|in|next|repeat|while)\b"));
        grammar.Add("operator", new Pattern(@"->?>?|<(?:=|<?-)?|[>=!]=?|::?|&&?|\|\|?|[+*\/^$@~]"));
        grammar.Add("punctuation", new Pattern(@"[(){}\[\],;]"));
        return grammar;
    }
}
