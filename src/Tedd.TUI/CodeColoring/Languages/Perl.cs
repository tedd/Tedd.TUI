using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class PerlLanguage : ILanguage
{
    public string Id => "perl";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        string brackets = @"(?:\((?:[^()\\]|\\[\s\S])*\)|\{(?:[^{}\\]|\\[\s\S])*\}|\[(?:[^[\]\\]|\\[\s\S])*\]|<(?:[^<>\\]|\\[\s\S])*>)" ;

        grammar.Add("comment", new List<Pattern>
        {
            new Pattern(@"(^\s*)=\w[\s\S]*?=cut.*$", regexOptions: "m", lookbehind: true, greedy: true),
            new Pattern(@"(^|[^\\$])#.*", lookbehind: true, greedy: true)
        });

        grammar.Add("string", new List<Pattern>
        {
            new Pattern(@"\b(?:q|qq|qw|qx)(?![a-zA-Z0-9])\s*(?:([^a-zA-Z0-9\s{(\[<])(?:(?!\1)[^\\]|\\[\s\S])*\1|([a-zA-Z0-9])(?:(?!\2)[^\\]|\\[\s\S])*\2|" + brackets + ")", greedy: true),
            new Pattern(@"(""|`)(?:(?!\1)[^\\]|\\[\s\S])*\1", greedy: true),
            new Pattern(@"'(?:[^'\\\r\n]|\\.)*'", greedy: true)
        });

        grammar.Add("regex", new List<Pattern>
        {
            new Pattern(@"\b(?:m|qr)(?![a-zA-Z0-9])\s*(?:([^a-zA-Z0-9\s{(\[<])(?:(?!\1)[^\\]|\\[\s\S])*\1|([a-zA-Z0-9])(?:(?!\2)[^\\]|\\[\s\S])*\2|" + brackets + ")[msixpodualngc]*", greedy: true),
            new Pattern(@"(^|[^-])\b(?:s|tr|y)(?![a-zA-Z0-9])\s*(?:([^a-zA-Z0-9\s{(\[<])(?:(?!\2)[^\\]|\\[\s\S])*\2(?:(?!\2)[^\\]|\\[\s\S])*\2|([a-zA-Z0-9])(?:(?!\3)[^\\]|\\[\s\S])*\3(?:(?!\3)[^\\]|\\[\s\S])*\3|" + brackets + @"\s*" + brackets + ")[msixpodualngcer]*", lookbehind: true, greedy: true),
            new Pattern(@"\/(?:[^\/\\\r\n]|\\.)*\/[msixpodualngc]*(?=\s*(?:$|[\r\n,.;})&|\-+*~<>!?^]|(?:and|cmp|eq|ge|gt|le|lt|ne|not|or|x|xor)\b))", greedy: true)
        });

        grammar.Add("variable", new List<Pattern>
        {
            new Pattern(@"[&*$@%]\{\^[A-Z]+\}"),
            new Pattern(@"[&*$@%]\^[A-Z_]"),
            new Pattern(@"[&*$@%]#?(?=\{)"),
            new Pattern(@"[&*$@%]#?(?:(?:::)*'?(?!\d)[\w$]+(?![\w$]))+(?:::)*/"),
            new Pattern(@"[&*$@%]\d+"),
            new Pattern(@"(?!%=)[$@%][!""#$%&'()*+,\-.\/:;<=>?@[\\\]^_`{|}~]")
        });

        grammar.Add("filehandle", new Pattern(@"<(?![<=])\S*?>|\b_\b", alias: "symbol"));
        grammar.Add("v-string", new Pattern(@"v\d+(?:\.\d+)*|\d+(?:\.\d+){2,}", alias: "string"));
        grammar.Add("function", new Pattern(@"(\bsub[ \t]+)\w+", lookbehind: true));
        grammar.Add("keyword", new Pattern(@"\b(?:any|break|continue|default|delete|die|do|else|elsif|eval|for|foreach|given|goto|if|last|local|my|next|our|package|print|redo|require|return|say|state|sub|switch|undef|unless|until|use|when|while)\b"));
        grammar.Add("number", new Pattern(@"\b(?:0x[\dA-Fa-f](?:_?[\dA-Fa-f])*|0b[01](?:_?[01])*|(?:(?:\d(?:_?\d)*)?\.)?\d(?:_?\d)*(?:[Ee][+-]?\d+)?)\b"));
        grammar.Add("operator", new Pattern(@"-[rwxoRWXOezsfdlpSbctugkTBMAC]\b|\+[+=]?|-[-=>]?|\*\*?=?|\/\/?=?|=[=~>]?|~[~=]?|\|\|?=?|&&?=?|<(?:=>?|<=?)?|>>?=?|![~=]?|[%^]=?|\.(?:=|\.\.?)?|[\\?]|\bx(?:=|\b)|\b(?:and|cmp|eq|ge|gt|le|lt|ne|not|or|xor)\b"));
        grammar.Add("punctuation", new Pattern(@"[{}[\];(),:]"));
        return grammar;
    }
}
