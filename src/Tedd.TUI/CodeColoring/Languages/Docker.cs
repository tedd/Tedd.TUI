using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class DockerLanguage : ILanguage
{
    public string Id => "docker";
    public string[] Aliases => ["dockerfile"];

    public Grammar GetGrammar()
    {
        // Negated lookaheads like `[ \t]+(?![ \t])` make quantifiers behave
        // atomically to prevent exponential backtracking (see Prism docker.js).
        string spaceAfterBackSlash = @"\\[\r\n](?:\s|\\[\r\n]|#.*(?!.))*(?![\s#]|\\[\r\n])";
        string space = @"(?:[ \t]+(?![ \t])(?:" + spaceAfterBackSlash + @")?|" + spaceAfterBackSlash + ")";
        string stringPattern = @"""(?:[^""\\\r\n]|\\(?:\r\n|[\s\S]))*""|'(?:[^'\\\r\n]|\\(?:\r\n|[\s\S]))*'";
        string option = @"--[\w-]+=(?:" + stringPattern + @"|(?![""'])(?:[^\s\\]|\\.)+)";

        var stringRule = new Pattern(stringPattern, greedy: true);
        var commentRule = new Pattern(@"(^[ \t]*)#.*", regexOptions: "m", lookbehind: true, greedy: true);

        var optionsInside = new Grammar();
        optionsInside.Add("property", new Pattern(@"(^|\s)--[\w-]+", lookbehind: true));
        optionsInside.Add("string", new List<Pattern>
        {
            stringRule,
            new Pattern(@"(=)(?![""'])(?:[^\s\\]|\\.)+", lookbehind: true)
        });
        optionsInside.Add("operator", new Pattern(@"\\$", regexOptions: "m"));
        optionsInside.Add("punctuation", new Pattern(@"="));

        var instructionInside = new Grammar();
        instructionInside.Add("options", new Pattern(@"(^(?:ONBUILD" + space + @")?\w+" + space + @")" + option + @"(?:" + space + option + @")*", regexOptions: "i", lookbehind: true, greedy: true, inside: optionsInside));
        instructionInside.Add("keyword", new List<Pattern>
        {
            new Pattern(@"(^(?:ONBUILD" + space + @")?HEALTHCHECK" + space + @"(?:" + option + space + @")*)(?:CMD|NONE)\b", regexOptions: "i", lookbehind: true, greedy: true),
            new Pattern(@"(^(?:ONBUILD" + space + @")?FROM" + space + @"(?:" + option + space + @")*(?!--)[^ \t\\]+" + space + @")AS", regexOptions: "i", lookbehind: true, greedy: true),
            new Pattern(@"(^ONBUILD" + space + @")\w+", regexOptions: "i", lookbehind: true, greedy: true),
            new Pattern(@"^\w+", greedy: true)
        });
        instructionInside.Add("comment", commentRule);
        instructionInside.Add("string", stringRule);
        instructionInside.Add("variable", new Pattern(@"\$(?:\w+|\{[^{}""'\\]*\})"));
        instructionInside.Add("operator", new Pattern(@"\\$", regexOptions: "m"));

        var grammar = new Grammar();
        grammar.Add("instruction", new Pattern(@"(^[ \t]*)(?:ADD|ARG|CMD|COPY|ENTRYPOINT|ENV|EXPOSE|FROM|HEALTHCHECK|LABEL|MAINTAINER|ONBUILD|RUN|SHELL|STOPSIGNAL|USER|VOLUME|WORKDIR)(?=\s)(?:\\.|[^\r\n\\])*(?:\\$(?:\s|#.*$)*(?![\s#])(?:\\.|[^\r\n\\])*)*", regexOptions: "im", lookbehind: true, greedy: true, inside: instructionInside));
        grammar.Add("comment", commentRule);
        return grammar;
    }
}
