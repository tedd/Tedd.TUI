using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class VisualBasicLanguage : ILanguage
{
    public string Id => "visual-basic";
    public string[] Aliases => ["vb", "vba", "vbnet"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var commentInside = new Grammar();
        commentInside.Add("keyword", new Pattern(@"^REM", regexOptions: "i"));

        grammar.Add("comment", new Pattern(@"(?:['‘’]|REM\b)(?:[^\r\n_]|_(?:\r\n?|\n)?)*", regexOptions: "i", inside: commentInside));
        grammar.Add("directive", new Pattern(@"#(?:Const|Else|ElseIf|End|ExternalChecksum|ExternalSource|If|Region)(?:\b_[ \t]*(?:\r\n?|\n)|.)+", regexOptions: "i", alias: "property", greedy: true));
        grammar.Add("string", new Pattern(@"\$?[""“”](?:[""“”]{2}|[^""“”])*[""“”]C?", regexOptions: "i", greedy: true));
        grammar.Add("date", new Pattern(@"#[ \t]*(?:\d+([/-])\d+\1\d+(?:[ \t]+(?:\d+[ \t]*(?:AM|PM)|\d+:\d+(?::\d+)?(?:[ \t]*(?:AM|PM))?))?|\d+[ \t]*(?:AM|PM)|\d+:\d+(?::\d+)?(?:[ \t]*(?:AM|PM))?)[ \t]*#", regexOptions: "i", alias: "number"));
        grammar.Add("number", new Pattern(@"(?:(?:\b\d+(?:\.\d+)?|\.\d+)(?:E[+-]?\d+)?|&[HO][\dA-F]+)(?:[FRD]|U?[ILS])?", regexOptions: "i"));
        grammar.Add("boolean", new Pattern(@"\b(?:False|Nothing|True)\b", regexOptions: "i"));
        grammar.Add("keyword", new Pattern(@"\b(?:AddHandler|AddressOf|Alias|And(?:Also)?|As|Boolean|ByRef|Byte|ByVal|Call|Case|Catch|C(?:Bool|Byte|Char|Date|Dbl|Dec|Int|Lng|Obj|SByte|Short|Sng|Str|Type|UInt|ULng|UShort)|Char|Class|Const|Continue|Currency|Date|Decimal|Declare|Default|Delegate|Dim|DirectCast|Do|Double|Each|Else(?:If)?|End(?:If)?|Enum|Erase|Error|Event|Exit|Finally|For|Friend|Function|Get(?:Type|XMLNamespace)?|Global|GoSub|GoTo|Handles|If|Implements|Imports|In|Inherits|Integer|Interface|Is|IsNot|Let|Lib|Like|Long|Loop|Me|Mod|Module|Must(?:Inherit|Override)|My(?:Base|Class)|Namespace|Narrowing|New|Next|Not(?:Inheritable|Overridable)?|Object|Of|On|Operator|Option(?:al)?|Or(?:Else)?|Out|Overloads|Overridable|Overrides|ParamArray|Partial|Private|Property|Protected|Public|RaiseEvent|ReadOnly|ReDim|RemoveHandler|Resume|Return|SByte|Select|Set|Shadows|Shared|short|Single|Static|Step|Stop|String|Structure|Sub|SyncLock|Then|Throw|To|Try|TryCast|Type|TypeOf|U(?:Integer|Long|Short)|Until|Using|Variant|Wend|When|While|Widening|With(?:Events)?|WriteOnly|Xor)\b", regexOptions: "i"));
        grammar.Add("operator", new Pattern(@"[+\-*/\\^<=>&#@$%!]|\b_(?=[ \t]*[\r\n])"));
        grammar.Add("punctuation", new Pattern(@"[{}().,:?]"));

        return grammar;
    }
}
