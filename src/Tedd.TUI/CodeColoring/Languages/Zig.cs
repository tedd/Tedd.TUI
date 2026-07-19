using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class ZigLanguage : ILanguage
{
    public string Id => "zig";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        string keyword = @"\b(?:align|allowzero|and|anyframe|anytype|asm|async|await|break|cancel|catch|comptime|const|continue|defer|else|enum|errdefer|error|export|extern|fn|for|if|inline|linksection|nakedcc|noalias|nosuspend|null|or|orelse|packed|promise|pub|resume|return|stdcallcc|struct|suspend|switch|test|threadlocal|try|undefined|union|unreachable|usingnamespace|var|volatile|while)\b";
        string identifier = @"\b(?!" + keyword + @")(?!\d)\w+\b";
        string align = @"align\s*\((?:[^()]|\([^()]*\))*\)";
        string prefixTypeOp = @"(?:\?|\bpromise->|(?:\[[^[\]]*\]|\*(?!\*)|\*\*)(?:\s*" + align + @"|\s*const\b|\s*volatile\b|\s*allowzero\b)*)";
        string suffixExpr = @"(?:\bpromise\b|(?:\berror\.)?" + identifier + @"(?:\." + identifier + @")*(?!\s+" + identifier + @"))";
        string type = @"(?!\s)(?:!?\s*(?:" + prefixTypeOp + @"\s*)*" + suffixExpr + ")+";

        grammar.Add("comment", new List<Pattern>
        {
            new Pattern(@"\/\/[/!].*", alias: "comment"),
            new Pattern(@"\/{2}.*")
        });
        grammar.Add("string", new List<Pattern>
        {
            // "string" and c"string"
            new Pattern(@"(^|[^\\@])c?""(?:[^""\\\r\n]|\\.)*""", lookbehind: true, greedy: true),
            // multiline strings and c-strings
            new Pattern(@"([\r\n])([ \t]+c?\\{2}).*(?:(?:\r\n?|\n)\2.*)*", lookbehind: true, greedy: true)
        });
        // characters 'a', '\n', '\xFF', '\u{10FFFF}'
        grammar.Add("char", new Pattern(@"(^|[^\\])'(?:[^'\\\r\n]|[\uD800-\uDFFF]{2}|\\(?:.|x[a-fA-F\d]{2}|u\{[a-fA-F\d]{1,6}\}))'", lookbehind: true, greedy: true));
        grammar.Add("builtin", new Pattern(@"\B@(?!\d)\w+(?=\s*\()"));
        grammar.Add("label", new Pattern(@"(\b(?:break|continue)\s*:\s*)\w+\b|\b(?!\d)\w+\b(?=\s*:\s*(?:\{|while\b))", lookbehind: true));
        grammar.Add("class-name", new List<Pattern>
        {
            // const Foo = struct {};
            new Pattern(@"\b(?!\d)\w+(?=\s*=\s*(?:(?:extern|packed)\s+)?(?:enum|struct|union)\s*[({])"),
            new Pattern(@"(:\s*)" + type + @"(?=\s*(?:" + align + @"\s*)?[=;,)])|" + type + @"(?=\s*(?:" + align + @"\s*)?\{)", lookbehind: true, inside: grammar),
            new Pattern(@"(\)\s*)" + type + @"(?=\s*(?:" + align + @"\s*)?;)", lookbehind: true, inside: grammar)
        });
        grammar.Add("builtin-type", new Pattern(@"\b(?:anyerror|bool|c_u?(?:int|long|longlong|short)|c_longdouble|c_void|comptime_(?:float|int)|f(?:16|32|64|128)|[iu](?:8|16|32|64|128|size)|noreturn|type|void)\b", alias: "keyword"));
        grammar.Add("keyword", new Pattern(keyword));
        grammar.Add("function", new Pattern(@"\b(?!\d)\w+(?=\s*\()"));
        grammar.Add("number", new Pattern(@"\b(?:0b[01]+|0o[0-7]+|0x[a-fA-F\d]+(?:\.[a-fA-F\d]*)?(?:[pP][+-]?[a-fA-F\d]+)?|\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b"));
        grammar.Add("operator", new Pattern(@"\.[*?]|\.{2,3}|[-=]>|\*\*|\+\+|\|\||(?:<<|>>|[-+*]%|[-+*/%^&|<>!=])=?|[?~]"));
        grammar.Add("punctuation", new Pattern(@"[.:,;(){}[\]]"));

        return grammar;
    }
}
