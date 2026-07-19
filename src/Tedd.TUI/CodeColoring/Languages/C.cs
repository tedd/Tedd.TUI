using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class CLanguage : ILanguage
{
    public string Id => "c";
    public string[] Aliases => ["h"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        var comment = new Pattern(@"\/\/(?:[^\r\n\\]|\\(?:\r\n?|\n|(?![\r\n])))*|\/\*[\s\S]*?(?:\*\/|$)", greedy: true);
        var stringPattern = new Pattern(@"""(?:\\(?:\r\n|[\s\S])|[^""\\\r\n])*""", greedy: true);
        var charPattern = new Pattern(@"'(?:\\(?:\r\n|[\s\S])|[^'\\\r\n]){0,32}'", greedy: true);

        grammar["comment"] = new List<Pattern> { comment };
        grammar["string"] = new List<Pattern> { stringPattern };
        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(@"(\b(?:enum|struct)\s+(?:__attribute__\s*\(\([\s\S]*?\)\)\s*)?)\w+|\b[a-z]\w*_t\b", lookbehind: true)
        };
        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:_Alignas|_Alignof|_Atomic|_Bool|_Complex|_Generic|_Imaginary|_Noreturn|_Static_assert|_Thread_local|__attribute__|asm|auto|break|case|char|const|continue|default|do|double|else|enum|extern|float|for|goto|if|inline|int|long|register|return|short|signed|sizeof|static|struct|switch|typedef|typeof|union|unsigned|void|volatile|while)\b")
        };
        grammar["function"] = new List<Pattern> { new Pattern(@"\b[a-z_]\w*(?=\s*\()", regexOptions: "i") };
        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"(?:\b0x(?:[\da-f]+(?:\.[\da-f]*)?|\.[\da-f]+)(?:p[+-]?\d+)?|(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:e[+-]?\d+)?)[ful]{0,4}", regexOptions: "i")
        };
        grammar["operator"] = new List<Pattern> { new Pattern(@">>=?|<<=?|->|([-+&|:])\1|[?:~]|[-+*/%&|^!=<>]=?") };
        grammar.Remove("boolean");

        var macroInside = new Grammar();
        macroInside.Add("string", new List<Pattern>
        {
            // highlight the path of the include statement as a string
            new Pattern(@"^(#\s*include\s*)<[^>]+>", lookbehind: true),
            stringPattern
        });
        macroInside.Add("char", charPattern);
        macroInside.Add("comment", comment);
        macroInside.Add("macro-name", new List<Pattern>
        {
            new Pattern(@"(^#\s*define\s+)\w+\b(?!\()", regexOptions: "i", lookbehind: true),
            new Pattern(@"(^#\s*define\s+)\w+\b(?=\()", regexOptions: "i", lookbehind: true, alias: "function")
        });
        // highlight macro directives as keywords
        macroInside.Add("directive", new Pattern(@"^(#\s*)[a-z]+", lookbehind: true, alias: "keyword"));
        macroInside.Add("directive-hash", new Pattern(@"^#"));
        macroInside.Add("punctuation", new Pattern(@"##|\\(?=[\r\n])"));
        macroInside.Add("expression", new Pattern(@"\S[\s\S]*", inside: grammar));

        grammar.InsertBefore("string", new Grammar
        {
            { "char", new List<Pattern> { charPattern } },
            { "macro", new List<Pattern>
                {
                    // allow for multiline macro definitions
                    new Pattern(@"(^[\t ]*)#\s*[a-z](?:[^\r\n\\/]|\/(?!\*)|\/\*(?:[^*]|\*(?!\/))*\*\/|\\(?:\r\n|[\s\S]))*",
                        regexOptions: "im", lookbehind: true, greedy: true, alias: "property", inside: macroInside)
                }
            }
        });

        // highlight predefined macros as constants
        grammar.InsertBefore("function", new Grammar
        {
            { "constant", new List<Pattern>
                {
                    new Pattern(@"\b(?:EOF|NULL|SEEK_CUR|SEEK_END|SEEK_SET|__DATE__|__FILE__|__LINE__|__TIMESTAMP__|__TIME__|__func__|stderr|stdin|stdout)\b")
                }
            }
        });

        return grammar;
    }
}
