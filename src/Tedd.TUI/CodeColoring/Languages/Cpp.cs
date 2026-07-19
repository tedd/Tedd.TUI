using System.Collections.Generic;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class CppLanguage : ILanguage
{
    public string Id => "cpp";
    public string[] Aliases => ["c++"];

    public Grammar GetGrammar()
    {
        var c = new CLanguage().GetGrammar();
        var grammar = Grammar.Extend(c, new Grammar());

        string keyword = @"\b(?:alignas|alignof|asm|auto|bool|break|case|catch|char|char16_t|char32_t|char8_t|class|co_await|co_return|co_yield|compl|concept|const|const_cast|consteval|constexpr|constinit|continue|decltype|default|delete|do|double|dynamic_cast|else|enum|explicit|export|extern|final|float|for|friend|goto|if|import|inline|int|int16_t|int32_t|int64_t|int8_t|long|module|mutable|namespace|new|noexcept|nullptr|operator|override|private|protected|public|register|reinterpret_cast|requires|return|short|signed|sizeof|static|static_assert|static_cast|struct|switch|template|this|thread_local|throw|try|typedef|typeid|typename|uint16_t|uint32_t|uint64_t|uint8_t|union|unsigned|using|virtual|void|volatile|wchar_t|while)\b";
        string modName = Replace(@"\b(?!<<0>>)\w+(?:\s*\.\s*\w+)*\b", keyword);

        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(Replace(@"(\b(?:class|concept|enum|struct|typename)\s+)(?!<<0>>)\w+", keyword), lookbehind: true),
            // class name of method implementations like `void foo::bar() const {}`
            new Pattern(@"\b[A-Z]\w*(?=\s*::\s*\w+\s*\()"),
            // class name before destructors like `Foo::~Foo() {}`
            new Pattern(@"\b[A-Z_]\w*(?=\s*::\s*~\w+\s*\()", regexOptions: "i"),
            // class name of method implementations with template parameters
            new Pattern(@"\b\w+(?=\s*<(?:[^<>]|<(?:[^<>]|<[^<>]*>)*>)*>\s*::\s*\w+\s*\()")
        };
        grammar["keyword"] = new List<Pattern> { new Pattern(keyword) };
        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"(?:\b0b[01']+|\b0x(?:[\da-f']+(?:\.[\da-f']*)?|\.[\da-f']+)(?:p[+-]?[\d']+)?|(?:\b[\d']+(?:\.[\d']*)?|\B\.[\d']+)(?:e[+-]?[\d']+)?)[ful]{0,4}", regexOptions: "i", greedy: true)
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@">>=?|<<=?|->|--|\+\+|&&|\|\||[?:~]|<=>|[-+*/%&|^!=<>]=?|\b(?:and|and_eq|bitand|bitor|not|not_eq|or|or_eq|xor|xor_eq)\b")
        };
        grammar["boolean"] = new List<Pattern> { new Pattern(@"\b(?:false|true)\b") };

        var moduleInside = new Grammar();
        moduleInside.Add("string", new Pattern(@"^[<""][\s\S]+"));
        moduleInside.Add("operator", new Pattern(@":"));
        moduleInside.Add("punctuation", new Pattern(@"\."));

        grammar.InsertBefore("string", new Grammar
        {
            { "module", new List<Pattern>
                {
                    new Pattern(@"(\b(?:import|module)\s+)(?:""(?:\\(?:\r\n|[\s\S])|[^""\\\r\n])*""|<[^<>\r\n]*>|" + Replace(@"<<0>>(?:\s*:\s*<<0>>)?|:\s*<<0>>", modName) + ")",
                        lookbehind: true, greedy: true, inside: moduleInside)
                }
            },
            { "raw-string", new List<Pattern>
                {
                    new Pattern(@"R""([^()\\ ]{0,16})\([\s\S]*?\)\1""", alias: "string", greedy: true)
                }
            }
        });

        var genericFunctionInside = new Grammar();
        genericFunctionInside.Add("function", new Pattern(@"^\w+"));
        genericFunctionInside.Add("generic", new Pattern(@"<[\s\S]+", alias: "class-name", inside: grammar));

        grammar.InsertBefore("keyword", new Grammar
        {
            { "generic-function", new List<Pattern>
                {
                    new Pattern(@"\b(?!operator\b)[a-z_]\w*\s*<(?:[^<>]|<[^<>]*>)*>(?=\s*\()", regexOptions: "i", inside: genericFunctionInside)
                }
            }
        });

        grammar.InsertBefore("operator", new Grammar
        {
            { "double-colon", new List<Pattern> { new Pattern(@"::", alias: "punctuation") } }
        });

        // The base clause is an optional list of parent classes. Untokenized words
        // that are not namespaces are highlighted as class names inside it.
        var baseInside = new Grammar();
        foreach (var kvp in grammar)
        {
            baseInside[kvp.Key] = kvp.Value;
        }
        baseInside.InsertBefore("double-colon", new Grammar
        {
            { "class-name", new List<Pattern> { new Pattern(@"\b[a-z_]\w*\b(?!\s*::)", regexOptions: "i") } }
        });

        grammar.InsertBefore("class-name", new Grammar
        {
            { "base-clause", new List<Pattern>
                {
                    new Pattern(@"(\b(?:class|struct)\s+\w+\s*:\s*)[^;{}""'\s]+(?:\s+[^;{}""'\s]+)*(?=\s*[;{])", lookbehind: true, greedy: true, inside: baseInside)
                }
            }
        });

        return grammar;
    }
}
