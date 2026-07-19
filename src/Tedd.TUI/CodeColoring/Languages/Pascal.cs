using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

// Based on Free Pascal
public class PascalLanguage : ILanguage
{
    public string Id => "pascal";
    public string[] Aliases => ["objectpascal", "delphi"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        grammar.Add("directive", new Pattern(@"\{\$[\s\S]*?\}", greedy: true, alias: "property"));
        grammar.Add("comment", new Pattern(@"\(\*[\s\S]*?\*\)|\{[\s\S]*?\}|\/\/.*", greedy: true));
        grammar.Add("string", new Pattern(@"(?:'(?:''|[^'\r\n])*'(?!')|#[&$%]?[a-f\d]+)+|\^[a-z]", regexOptions: "i", greedy: true));

        // asm blocks keep string/comment/number highlighting but no Pascal keywords;
        // the inside grammar is filled below once those entries exist.
        var asmInside = new Grammar();
        grammar.Add("asm", new Pattern(@"(\basm\b)[\s\S]+?(?=\bend\s*[;[])", regexOptions: "i", lookbehind: true, greedy: true, inside: asmInside));

        grammar.Add("keyword", new List<Pattern>
        {
            // Turbo Pascal
            new Pattern(@"(^|[^&])\b(?:absolute|array|asm|begin|case|const|constructor|destructor|do|downto|else|end|file|for|function|goto|if|implementation|inherited|inline|interface|label|nil|object|of|operator|packed|procedure|program|record|reintroduce|repeat|self|set|string|then|to|type|unit|until|uses|var|while|with)\b", regexOptions: "i", lookbehind: true),
            // Free Pascal
            new Pattern(@"(^|[^&])\b(?:dispose|exit|false|new|true)\b", regexOptions: "i", lookbehind: true),
            // Object Pascal
            new Pattern(@"(^|[^&])\b(?:class|dispinterface|except|exports|finalization|finally|initialization|inline|library|on|out|packed|property|raise|resourcestring|threadvar|try)\b", regexOptions: "i", lookbehind: true),
            // Modifiers
            new Pattern(@"(^|[^&])\b(?:absolute|abstract|alias|assembler|bitpacked|break|cdecl|continue|cppdecl|cvar|default|deprecated|dynamic|enumerator|experimental|export|external|far|far16|forward|generic|helper|implements|index|interrupt|iochecks|local|message|name|near|nodefault|noreturn|nostackframe|oldfpccall|otherwise|overload|override|pascal|platform|private|protected|public|published|read|register|reintroduce|result|safecall|saveregisters|softfloat|specialize|static|stdcall|stored|strict|unaligned|unimplemented|varargs|virtual|write)\b", regexOptions: "i", lookbehind: true)
        });
        grammar.Add("number", new List<Pattern>
        {
            // Hexadecimal, octal and binary
            new Pattern(@"(?:[&%]\d+|\$[a-f\d]+)", regexOptions: "i"),
            // Decimal
            new Pattern(@"\b\d+(?:\.\d+)?(?:e[+-]?\d+)?", regexOptions: "i")
        });
        grammar.Add("operator", new List<Pattern>
        {
            new Pattern(@"\.\.|\*\*|:=|<[<=>]?|>[>=]?|[+\-*\/]=?|[@^=]"),
            new Pattern(@"(^|[^&])\b(?:and|as|div|exclude|in|include|is|mod|not|or|shl|shr|xor)\b", lookbehind: true)
        });
        grammar.Add("punctuation", new Pattern(@"\(\.|\.\)|[()\[\]:;,.]"));

        // asm inside = the pascal grammar without asm/keyword/operator
        foreach (var kvp in grammar)
        {
            if (kvp.Key is not ("asm" or "keyword" or "operator"))
            {
                asmInside[kvp.Key] = kvp.Value;
            }
        }

        return grammar;
    }
}
