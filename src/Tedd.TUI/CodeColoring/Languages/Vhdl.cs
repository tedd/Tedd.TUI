using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class VhdlLanguage : ILanguage
{
    public string Id => "vhdl";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"--.+"));
        // logic vectors
        grammar.Add("vhdl-vectors", new Pattern(@"\b[oxb]""[\da-f_]+""|""[01uxzwlh-]+""", regexOptions: "i", alias: "number"));
        // operator overloading
        grammar.Add("quoted-function", new Pattern(@"""\S+?""(?=\()", alias: "function"));
        grammar.Add("string", new Pattern(@"""(?:[^\\""\r\n]|\\(?:\r\n|[\s\S]))*"""));
        grammar.Add("attribute", new Pattern(@"\b'\w+", alias: "attr-name"));
        grammar.Add("keyword", new Pattern(@"\b(?:access|after|alias|all|architecture|array|assert|attribute|begin|block|body|buffer|bus|case|component|configuration|constant|disconnect|downto|else|elsif|end|entity|exit|file|for|function|generate|generic|group|guarded|if|impure|in|inertial|inout|is|label|library|linkage|literal|loop|map|new|next|null|of|on|open|others|out|package|port|postponed|private|procedure|process|pure|range|record|register|reject|report|return|select|severity|shared|signal|subtype|then|to|transport|type|unaffected|units|until|use|variable|view|wait|when|while|with)\b", regexOptions: "i"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b", regexOptions: "i"));
        grammar.Add("function", new Pattern(@"\w+(?=\()"));
        // decimal, based, physical, and exponential numbers
        grammar.Add("number", new Pattern(@"'[01uxzwlh-]'|\b(?:\d+#[\da-f_.]+#|\d[\d_.]*)(?:e[-+]?\d+)?", regexOptions: "i"));
        grammar.Add("operator", new Pattern(@"[<>]=?|:=|[-+*/&=]|\b(?:abs|and|mod|nand|nor|not|or|rem|rol|ror|sla|sll|sra|srl|xnor|xor)\b", regexOptions: "i"));
        grammar.Add("punctuation", new Pattern(@"[{}[\];(),.:]"));
        return grammar;
    }
}
