using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class NginxLanguage : ILanguage
{
    public string Id => "nginx";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        string variable = @"\$(?:\w[a-z\d]*(?:_[^\x00-\x1F\s""'\\()$]*)?|\{[^}\s""'\\]+\})";

        var stringInside = new Grammar();
        stringInside.Add("escape", new Pattern(@"\\[""'\\nrt]", alias: "entity"));
        stringInside.Add("variable", new Pattern(variable, regexOptions: "i"));

        var directiveInside = new Grammar();
        directiveInside.Add("string", new Pattern(@"((?:^|[^\\])(?:\\\\)*)(?:""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*')", lookbehind: true, greedy: true, inside: stringInside));
        directiveInside.Add("comment", new Pattern(@"(\s)#.*", lookbehind: true, greedy: true));
        directiveInside.Add("keyword", new Pattern(@"^\S+", greedy: true));
        directiveInside.Add("boolean", new Pattern(@"(\s)(?:off|on)(?!\S)", lookbehind: true));
        directiveInside.Add("number", new Pattern(@"(\s)\d+[a-z]*(?!\S)", regexOptions: "i", lookbehind: true));
        directiveInside.Add("variable", new Pattern(variable, regexOptions: "i"));

        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"(^|[\s{};])#.*", lookbehind: true, greedy: true));
        grammar.Add("directive", new Pattern(@"(^|\s)\w(?:[^;{}""'\\\s]|\\.|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|\s+(?:#.*(?!.)|(?![#\s])))*?(?=\s*[;{])", lookbehind: true, greedy: true, inside: directiveInside));
        grammar.Add("punctuation", new Pattern(@"[{};]"));
        return grammar;
    }
}
