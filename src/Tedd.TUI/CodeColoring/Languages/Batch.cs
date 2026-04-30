using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class BatchLanguage : ILanguage
{
    public string Id => "batch";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var variablePattern = new Pattern(@"%%?[~:\w]+%?|!\S+!");
        var parameterInside = new Grammar();
        parameterInside.Add("punctuation", new Pattern(@":"));
        var parameterPattern = new Pattern(@"\/[a-z?]+(?=[ :]|$):?|-[a-z]\b|--[a-z-]+\b", regexOptions: "im", alias: "attr-name", inside: parameterInside);
        var stringPattern = new Pattern(@"""(?:[\\""]""|[^""])*""(?!"")");
        var numberPattern = new Pattern(@"(?:\b|-)\d+\b");

        grammar.Add("comment", new List<Pattern>
        {
            new Pattern(@"^::.*", regexOptions: "m"),
            new Pattern(@"((?:^|[&(])[ \t]*)rem\b(?:[^^&)\r\n]|\^(?:\r\n|[\s\S]))*", regexOptions: "im", lookbehind: true)
        });

        grammar.Add("label", new Pattern(@"^:.*", regexOptions: "m", alias: "property"));

        var commandPatterns = new List<Pattern>();

        // FOR command
        var forInside = new Grammar();
        forInside.Add("keyword", new Pattern(@"\b(?:do|in)\b|^for\b", regexOptions: "i"));
        forInside.Add("string", stringPattern);
        forInside.Add("parameter", parameterPattern);
        forInside.Add("variable", variablePattern);
        forInside.Add("number", numberPattern);
        forInside.Add("punctuation", new Pattern(@"[()',]"));

        commandPatterns.Add(new Pattern(@"((?:^|[&(])[ \t]*)for(?: \/[a-z?](?:[ :](?:""[^""]*""|[^\s""/]\S*))?)* \S+ in \([^)]+\) do", regexOptions: "im", lookbehind: true, inside: forInside));

        // IF command
        var ifInside = new Grammar();
        ifInside.Add("keyword", new Pattern(@"\b(?:cmdextversion|defined|errorlevel|exist|not)\b|^if\b", regexOptions: "i"));
        ifInside.Add("string", stringPattern);
        ifInside.Add("parameter", parameterPattern);
        ifInside.Add("variable", variablePattern);
        ifInside.Add("number", numberPattern);
        ifInside.Add("operator", new Pattern(@"\^|==|\b(?:equ|geq|gtr|leq|lss|neq)\b", regexOptions: "i"));

        commandPatterns.Add(new Pattern(@"((?:^|[&(])[ \t]*)if(?: \/[a-z?](?:[ :](?:""[^""]*""|[^\s""/]\S*))?)* (?:not )?(?:cmdextversion \d+|defined \w+|errorlevel \d+|exist \S+|(?:"".*?""|(?!"""")(?:(?!==)\S)+)?(?:==| (?:equ|geq|gtr|leq|lss|neq) )(?:"".*?""|[^\s""]\S*))", regexOptions: "im", lookbehind: true, inside: ifInside));

        // ELSE command
        var elseInside = new Grammar();
        elseInside.Add("keyword", new Pattern(@"^else\b", regexOptions: "i"));
        commandPatterns.Add(new Pattern(@"((?:^|[&()])[ \t]*)else\b", regexOptions: "im", lookbehind: true, inside: elseInside));

        // SET command
        var setInside = new Grammar();
        setInside.Add("keyword", new Pattern(@"^set\b", regexOptions: "i"));
        setInside.Add("string", stringPattern);
        setInside.Add("parameter", parameterPattern);
        setInside.Add("variable", new List<Pattern> { variablePattern, new Pattern(@"\w+(?=(?:[*\/%+\-&^|]|<<|>>)?=)") });
        setInside.Add("number", numberPattern);
        setInside.Add("operator", new Pattern(@"[*\/%+\-&^|]=?|<<=?|>>=?|[!~_=]"));
        setInside.Add("punctuation", new Pattern(@"[()',]"));

        commandPatterns.Add(new Pattern(@"((?:^|[&(])[ \t]*)set(?: \/[a-z](?:[ :](?:""[^""]*""|[^\s""/]\S*))?)* (?:[^^&)\r\n]|\^(?:\r\n|[\s\S]))*", regexOptions: "im", lookbehind: true, inside: setInside));

        // Other commands
        var otherInside = new Grammar();
        otherInside.Add("keyword", new Pattern(@"^\w+\b"));
        otherInside.Add("string", stringPattern);
        otherInside.Add("parameter", parameterPattern);
        otherInside.Add("label", new Pattern(@"(^\s*):\S+", regexOptions: "m", lookbehind: true, alias: "property"));
        otherInside.Add("variable", variablePattern);
        otherInside.Add("number", numberPattern);
        otherInside.Add("operator", new Pattern(@"\^"));

        commandPatterns.Add(new Pattern(@"((?:^|[&(])[ \t]*@?)\w+\b(?:""(?:[\\""]""|[^""])*""(?!"")|[^""^&)\r\n]|\^(?:\r\n|[\s\S]))*", regexOptions: "m", lookbehind: true, inside: otherInside));

        grammar.Add("command", commandPatterns);
        grammar.Add("operator", new Pattern(@"[&@]"));
        grammar.Add("punctuation", new Pattern(@"[()']"));

        return grammar;
    }
}
