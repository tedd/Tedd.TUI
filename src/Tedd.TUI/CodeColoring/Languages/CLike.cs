using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class CLikeLanguage : ILanguage
{
    public string Id => "clike";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        // Comment
        grammar.Add("comment", new List<Pattern>
        {
            new Pattern(@"(^|[^\\])\/\*[\s\S]*?(?:\*\/|$)", lookbehind: true, greedy: true),
            new Pattern(@"(^|[^\\:])\/\/.*", lookbehind: true, greedy: true)
        });

        // String
        grammar.Add("string", new Pattern(@"([""'])(?:\\(?:\r\n|[\s\S])|(?!\1)[^\\\r\n])*\1", greedy: true));

        // Class-name
        var classNameInside = new Grammar();
        classNameInside.Add("punctuation", new Pattern(@"[.\\]"));

        grammar.Add("class-name", new Pattern(@"(\b(?:class|extends|implements|instanceof|interface|new|trait)\s+|\bcatch\s+\()[\w.\\]+",
            regexOptions: "i", lookbehind: true, inside: classNameInside));

        // Keyword
        grammar.Add("keyword", new Pattern(@"\b(?:break|catch|continue|do|else|finally|for|function|if|in|instanceof|new|null|return|throw|try|while)\b"));

        // Boolean
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));

        // Function
        grammar.Add("function", new Pattern(@"\b\w+(?=\()"));

        // Number
        grammar.Add("number", new Pattern(@"\b0x[\da-f]+\b|(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:e[+-]?\d+)?", regexOptions: "i"));

        // Operator
        grammar.Add("operator", new Pattern(@"[<>]=?|[!=]=?=?|--?|\+\+?|&&?|\|\|?|[?*/~^%]"));

        // Punctuation
        grammar.Add("punctuation", new Pattern(@"[{}[\];(),.:]"));

        return grammar;
    }
}
