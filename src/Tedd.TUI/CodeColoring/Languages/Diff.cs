using System.Collections.Generic;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.CodeColoring.Languages;

public class DiffLanguage : ILanguage
{
    public string Id => "diff";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        grammar.Add("coord", new List<Pattern>
        {
            new Pattern(@"^(?:\*{3}|-{3}|\+{3}).*$", regexOptions: "m"),
            new Pattern(@"^@@.*@@$", regexOptions: "m"),
            new Pattern(@"^\d.*$", regexOptions: "m")
        });

        // Prefixes
        // deleted-sign: -
        // deleted-arrow: <
        // inserted-sign: +
        // inserted-arrow: >
        // unchanged: space
        // diff: !

        AddPrefix(grammar, "deleted-sign", "-", "deleted");
        AddPrefix(grammar, "deleted-arrow", "<", "deleted");
        AddPrefix(grammar, "inserted-sign", "+", "inserted");
        AddPrefix(grammar, "inserted-arrow", ">", "inserted");
        AddPrefix(grammar, "unchanged", " ", null);
        AddPrefix(grammar, "diff", "!", "bold");

        return grammar;
    }

    private void AddPrefix(Grammar grammar, string name, string prefix, string? alias)
    {
        var inside = new Grammar();
        inside.Add("line", new Pattern(@"(.)(?=[\s\S]).*(?:\r\n?|\n)?", lookbehind: true));
        inside.Add("prefix", new Pattern(@"[\s\S]", alias: alias ?? name));

        grammar.Add(name, new Pattern($@"^(?:[{prefix}].*(?:\r\n?|\n|(?![\\s\\S])))+", regexOptions: "m", alias: alias, inside: inside));
    }
}
