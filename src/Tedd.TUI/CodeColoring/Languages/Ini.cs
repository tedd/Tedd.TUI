using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

// Mimics the behavior of the Win32 API INI parser.
public class IniLanguage : ILanguage
{
    public string Id => "ini";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"(^[ \f\t\v]*)[#;][^\n\r]*", regexOptions: "m", lookbehind: true));

        var sectionInside = new Grammar();
        sectionInside.Add("section-name", new Pattern(@"(^\[[ \f\t\v]*)[^ \f\t\v\]]+(?:[ \f\t\v]+[^ \f\t\v\]]+)*", lookbehind: true, alias: "selector"));
        sectionInside.Add("punctuation", new Pattern(@"\[|\]"));
        grammar.Add("section", new Pattern(@"(^[ \f\t\v]*)\[[^\n\r\]]*\]?", regexOptions: "m", lookbehind: true, inside: sectionInside));

        grammar.Add("key", new Pattern(@"(^[ \f\t\v]*)[^ \f\n\r\t\v=]+(?:[ \f\t\v]+[^ \f\n\r\t\v=]+)*(?=[ \f\t\v]*=)", regexOptions: "m", lookbehind: true, alias: "attr-name"));

        var valueInside = new Grammar();
        valueInside.Add("inner-value", new Pattern(@"^(""|').+(?=\1$)", lookbehind: true));
        grammar.Add("value", new Pattern(@"(=[ \f\t\v]*)[^ \f\n\r\t\v]+(?:[ \f\t\v]+[^ \f\n\r\t\v]+)*", lookbehind: true, alias: "attr-value", inside: valueInside));

        grammar.Add("punctuation", new Pattern(@"="));
        return grammar;
    }
}
