using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class GitLanguage : ILanguage
{
    public string Id => "git";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        // One-line comment like in a git status output
        grammar.Add("comment", new Pattern(@"^#.*", regexOptions: "m"));
        // Changed lines in a git diff output
        grammar.Add("deleted", new Pattern(@"^[-–].*", regexOptions: "m"));
        grammar.Add("inserted", new Pattern(@"^\+.*", regexOptions: "m"));
        grammar.Add("string", new Pattern(@"(""|')(?:\\.|(?!\1)[^\\\r\n])*\1"));

        // A git command, e.g. `$ git add file.txt`
        var commandInside = new Grammar();
        commandInside.Add("parameter", new Pattern(@"\s--?\w+"));
        grammar.Add("command", new Pattern(@"^.*\$ git .*$", regexOptions: "m", inside: commandInside));

        // Coordinates in a git diff, e.g. `@@ -1 +1,2 @@`
        grammar.Add("coord", new Pattern(@"^@@.*@@$", regexOptions: "m"));
        // A "commit [SHA1]" line in git log output
        grammar.Add("commit-sha1", new Pattern(@"^commit \w{40}$", regexOptions: "m"));
        return grammar;
    }
}
