using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class HclLanguage : ILanguage
{
    public string Id => "hcl";
    public string[] Aliases => ["terraform", "tf"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"(?:\/\/|#).*|\/\*[\s\S]*?(?:\*\/|$)"));
        grammar.Add("heredoc", new Pattern(@"<<-?(\w+\b)[\s\S]*?^[ \t]*\1", regexOptions: "m", greedy: true, alias: "string"));

        var resourceTypeInside = new Grammar();
        resourceTypeInside.Add("type", new Pattern(@"(data|ephemeral|resource|\s+)(?:""(?:\\[\s\S]|[^\\""])*"")", regexOptions: "i", lookbehind: true, alias: "variable"));

        var blockTypeInside = new Grammar();
        blockTypeInside.Add("type", new Pattern(@"(backend|module|output|provider|provisioner|variable)\s+(?:[\w-]+|""(?:\\[\s\S]|[^\\""])*"")\s+", regexOptions: "i", lookbehind: true, alias: "variable"));

        grammar.Add("keyword", new List<Pattern>
        {
            new Pattern(@"(?:data|ephemeral|resource)\s+(?:""(?:\\[\s\S]|[^\\""])*"")(?=\s+""[\w-]+""\s+\{)", regexOptions: "i", inside: resourceTypeInside),
            new Pattern(@"(?:backend|module|output|provider|provisioner|variable)\s+(?:[\w-]+|""(?:\\[\s\S]|[^\\""])*"")\s+(?=\{)", regexOptions: "i", inside: blockTypeInside),
            new Pattern(@"[\w-]+(?=\s+\{)")
        });

        grammar.Add("property", new List<Pattern>
        {
            new Pattern(@"[-\w\.]+(?=\s*=(?!=))"),
            new Pattern(@"""(?:\\[\s\S]|[^\\""])+""(?=\s*[:=])")
        });

        var interpolationInside = new Grammar();
        interpolationInside.Add("type", new Pattern(@"(\b(?:count|data|local|module|path|self|terraform|var)\b\.)[\w\*]+", regexOptions: "i", lookbehind: true, alias: "variable"));
        interpolationInside.Add("keyword", new Pattern(@"\b(?:count|data|local|module|path|self|terraform|var)\b", regexOptions: "i"));
        interpolationInside.Add("function", new Pattern(@"\w+(?=\()"));
        interpolationInside.Add("string", new Pattern(@"""(?:\\[\s\S]|[^\\""])*""", greedy: true));
        interpolationInside.Add("number", new Pattern(@"\b0x[\da-f]+\b|\b\d+(?:\.\d*)?(?:e[+-]?\d+)?", regexOptions: "i"));
        interpolationInside.Add("punctuation", new Pattern(@"[!\$#%&'()*+,.\/;<=>@\[\\\]^`{|}~?:]"));

        var stringInside = new Grammar();
        stringInside.Add("interpolation", new Pattern(@"(^|[^$])\$\{(?:[^{}""]|""(?:[^\\""]|\\[\s\S])*"")*\}", lookbehind: true, inside: interpolationInside));

        grammar.Add("string", new Pattern(@"""(?:[^\\$""]|\\[\s\S]|\$(?:(?="")|\$+(?!\$)|[^""${])|\$\{(?:[^{}""]|""(?:[^\\""]|\\[\s\S])*"")*\})*""", greedy: true, inside: stringInside));
        grammar.Add("number", new Pattern(@"\b0x[\da-f]+\b|\b\d+(?:\.\d*)?(?:e[+-]?\d+)?", regexOptions: "i"));
        grammar.Add("boolean", new Pattern(@"\b(?:false|true)\b", regexOptions: "i"));
        grammar.Add("punctuation", new Pattern(@"[=\[\]{}]"));
        return grammar;
    }
}
