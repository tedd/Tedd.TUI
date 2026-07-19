using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class SchemeLanguage : ILanguage
{
    public string Id => "scheme";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        // Supports ; comments, #; datum comments, and 1 level of nested #| |# comments
        grammar.Add("comment", new Pattern(@";.*|#;\s*(?:\((?:[^()]|\([^()]*\))*\)|\[(?:[^\[\]]|\[[^\[\]]*\])*\])|#\|(?:[^#|]|#(?!\|)|\|(?!#)|#\|(?:[^#|]|#(?!\|)|\|(?!#))*\|#)*\|#"));
        grammar.Add("string", new Pattern(@"""(?:[^""\\]|\\.)*""", greedy: true));
        grammar.Add("symbol", new Pattern(@"'[^()\[\]#'\s]+", greedy: true));
        grammar.Add("char", new Pattern(@"#\\(?:[ux][a-fA-F\d]+\b|[-a-zA-Z]+\b|[\uD800-\uDBFF][\uDC00-\uDFFF]|\S)", greedy: true));
        grammar.Add("lambda-parameter", new List<Pattern>
        {
            new Pattern(@"((?:^|[^'`#])[(\[]lambda\s+)(?:[^|()\[\]'\s]+|\|(?:[^\\|]|\\.)*\|)", lookbehind: true),
            new Pattern(@"((?:^|[^'`#])[(\[]lambda\s+[(\[])[^()\[\]']+", lookbehind: true)
        });
        grammar.Add("keyword", new Pattern(@"((?:^|[^'`#])[(\[])(?:begin|case(?:-lambda)?|cond(?:-expand)?|define(?:-library|-macro|-record-type|-syntax|-values)?|defmacro|delay(?:-force)?|do|else|except|export|guard|if|import|include(?:-ci|-library-declarations)?|lambda|let(?:rec)?(?:-syntax|-values|\*)?|let\*-values|only|parameterize|prefix|(?:quasi-?)?quote|rename|set!|syntax-(?:case|rules)|unless|unquote(?:-splicing)?|when)(?=[()\[\]\s]|$)", lookbehind: true));
        grammar.Add("builtin", new Pattern(@"((?:^|[^'`#])[(\[])(?:abs|and|append|apply|assoc|ass[qv]|binary-port\?|boolean=?\?|bytevector(?:-append|-copy|-copy!|-length|-u8-ref|-u8-set!|\?)?|caar|cadr|call-with-(?:current-continuation|port|values)|call\/cc|car|cdar|cddr|cdr|ceiling|char(?:->integer|-ready\?|\?|<\?|<=\?|=\?|>\?|>=\?)|close-(?:input-port|output-port|port)|complex\?|cons|current-(?:error|input|output)-port|denominator|dynamic-wind|eof-object\??|eq\?|equal\?|eqv\?|error|error-object(?:-irritants|-message|\?)|eval|even\?|exact(?:-integer-sqrt|-integer\?|\?)?|expt|features|file-error\?|floor(?:-quotient|-remainder|\/)?|flush-output-port|for-each|gcd|get-output-(?:bytevector|string)|inexact\??|input-port(?:-open\?|\?)|integer(?:->char|\?)|lcm|length|list(?:->string|->vector|-copy|-ref|-set!|-tail|\?)?|make-(?:bytevector|list|parameter|string|vector)|map|max|member|memq|memv|min|modulo|negative\?|newline|not|null\?|number(?:->string|\?)|numerator|odd\?|open-(?:input|output)-(?:bytevector|string)|or|output-port(?:-open\?|\?)|pair\?|peek-char|peek-u8|port\?|positive\?|procedure\?|quotient|raise|raise-continuable|rational\?|rationalize|read-(?:bytevector|bytevector!|char|error\?|line|string|u8)|real\?|remainder|reverse|round|set-c[ad]r!|square|string(?:->list|->number|->symbol|->utf8|->vector|-append|-copy|-copy!|-fill!|-for-each|-length|-map|-ref|-set!|\?|<\?|<=\?|=\?|>\?|>=\?)?|substring|symbol(?:->string|\?|=\?)|syntax-error|textual-port\?|truncate(?:-quotient|-remainder|\/)?|u8-ready\?|utf8->string|values|vector(?:->list|->string|-append|-copy|-copy!|-fill!|-for-each|-length|-map|-ref|-set!|\?)?|with-exception-handler|write-(?:bytevector|char|string|u8)|zero\?)(?=[()\[\]\s]|$)", lookbehind: true));
        grammar.Add("operator", new Pattern(@"((?:^|[^'`#])[(\[])(?:[-+*%/]|[<>]=?|=>?)(?=[()\[\]\s]|$)", lookbehind: true));

        // R7RS number pattern, simplified into decimal (dec) and combined
        // binary/octal/hexadecimal (box) alternatives.
        string urealDec = @"\d+(?:\/\d+)|(?:\d+(?:\.\d*)?|\.\d+)(?:[esfdl][+-]?\d+)?";
        string realDec = @"[+-]?(?:" + urealDec + @")|[+-](?:inf|nan)\.0";
        string imaginaryDec = @"[+-](?:(?:" + urealDec + @")|(?:inf|nan)\.0)?i";
        string complexDec = @"(?:" + realDec + @")(?:@(?:" + realDec + @")|(?:" + imaginaryDec + @"))?|(?:" + imaginaryDec + @")";
        string numDec = @"(?:#d(?:#[ei])?|#[ei](?:#d)?)?(?:" + complexDec + @")";
        string urealBox = @"[0-9a-f]+(?:\/[0-9a-f]+)?";
        string realBox = @"[+-]?(?:" + urealBox + @")|[+-](?:inf|nan)\.0";
        string imaginaryBox = @"[+-](?:(?:" + urealBox + @")|(?:inf|nan)\.0)?i";
        string complexBox = @"(?:" + realBox + @")(?:@(?:" + realBox + @")|(?:" + imaginaryBox + @"))?|(?:" + imaginaryBox + @")";
        string numBox = @"#[box](?:#[ei])?|(?:#[ei])?#[box](?:" + complexBox + @")";

        grammar.Add("number", new Pattern(@"(^|[()\[\]\s])(?:(?:" + numDec + @")|(?:" + numBox + @"))(?=[()\[\]\s]|$)", regexOptions: "i", lookbehind: true));
        grammar.Add("boolean", new Pattern(@"(^|[()\[\]\s])#(?:[ft]|false|true)(?=[()\[\]\s]|$)", lookbehind: true));
        grammar.Add("function", new Pattern(@"((?:^|[^'`#])[(\[])(?:[^|()\[\]'\s]+|\|(?:[^\\|]|\\.)*\|)(?=[()\[\]\s]|$)", lookbehind: true));
        grammar.Add("identifier", new Pattern(@"(^|[()\[\]\s])\|(?:[^\\|]|\\.)*\|(?=[()\[\]\s]|$)", lookbehind: true, greedy: true));
        grammar.Add("punctuation", new Pattern(@"[()\[\]']"));

        return grammar;
    }
}
