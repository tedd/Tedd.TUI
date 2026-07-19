using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class FSharpLanguage : ILanguage
{
    public string Id => "fsharp";
    public string[] Aliases => ["fs"];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        grammar["comment"] = new List<Pattern>
        {
            new Pattern(@"(^|[^\\])\(\*(?!\))[\s\S]*?\*\)", lookbehind: true, greedy: true),
            new Pattern(@"(^|[^\\:])\/\/.*", lookbehind: true, greedy: true)
        };
        grammar["string"] = new List<Pattern>
        {
            new Pattern(@"(?:""""""[\s\S]*?""""""|@""(?:""""|[^""])*""|""(?:\\[\s\S]|[^\\""])*"")B?", greedy: true)
        };

        var classNameInside = new Grammar();
        classNameInside.Add("operator", new Pattern(@"->|\*"));
        classNameInside.Add("punctuation", new Pattern(@"\."));
        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(@"(\b(?:exception|inherit|interface|new|of|type)\s+|\w\s*:\s*|\s:\??>\s*)[.\w]+\b(?:\s*(?:->|\*)\s*[.\w]+\b)*(?!\s*[:.])", lookbehind: true, inside: classNameInside)
        };

        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:let|return|use|yield)(?:!\B|\b)|\b(?:abstract|and|as|asr|assert|atomic|base|begin|break|checked|class|component|const|constraint|constructor|continue|default|delegate|do|done|downcast|downto|eager|elif|else|end|event|exception|extern|external|false|finally|fixed|for|fun|function|functor|global|if|in|include|inherit|inline|interface|internal|land|lazy|lor|lsl|lsr|lxor|match|member|method|mixin|mod|module|mutable|namespace|new|not|null|object|of|open|or|override|parallel|private|process|protected|public|pure|rec|sealed|select|sig|static|struct|tailcall|then|to|trait|true|try|type|upcast|val|virtual|void|volatile|when|while|with)\b")
        };
        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"\b0x[\da-fA-F]+(?:LF|lf|un)?\b"),
            new Pattern(@"\b0b[01]+(?:uy|y)?\b"),
            new Pattern(@"(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:[fm]|e[+-]?\d+)?\b", regexOptions: "i"),
            new Pattern(@"\b\d+(?:[IlLsy]|UL|u[lsy]?)?\b")
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"([<>~&^])\1\1|([*.:<>&])\2|<-|->|[!=:]=|<?\|{1,3}>?|\??(?:<=|>=|<>|[-+*/%=<>])\??|[!?^&]|~[+~-]|:>|:\?>?")
        };

        var preprocessorInside = new Grammar();
        preprocessorInside.Add("directive", new Pattern(@"(^#)\b(?:else|endif|if|light|line|nowarn)\b", lookbehind: true, alias: "keyword"));

        grammar.InsertBefore("keyword", new Grammar
        {
            { "preprocessor", new List<Pattern>
                {
                    new Pattern(@"(^[\t ]*)#.*", regexOptions: "m", lookbehind: true, alias: "property", inside: preprocessorInside)
                }
            }
        });

        grammar.InsertBefore("punctuation", new Grammar
        {
            { "computation-expression", new List<Pattern> { new Pattern(@"\b[_a-z]\w*(?=\s*\{)", regexOptions: "i", alias: "keyword") } }
        });

        var annotationInside = new Grammar();
        annotationInside.Add("punctuation", new Pattern(@"^\[<|>\]$"));
        annotationInside.Add("class-name", new Pattern(@"^\w+$|(^|;\s*)[A-Z]\w*(?=\()", lookbehind: true));
        annotationInside.Add("annotation-content", new Pattern(@"[\s\S]+", inside: grammar));

        grammar.InsertBefore("string", new Grammar
        {
            { "annotation", new List<Pattern> { new Pattern(@"\[<.+?>\]", greedy: true, inside: annotationInside) } },
            { "char", new List<Pattern>
                {
                    new Pattern(@"'(?:[^\\']|\\(?:.|\d{3}|x[a-fA-F\d]{2}|u[a-fA-F\d]{4}|U[a-fA-F\d]{8}))'B?", greedy: true)
                }
            }
        });

        return grammar;
    }
}
