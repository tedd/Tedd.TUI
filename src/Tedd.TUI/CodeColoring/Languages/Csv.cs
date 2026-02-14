using System.Collections.Generic;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.CodeColoring.Languages;

public class CsvLanguage : ILanguage
{
    public string Id => "csv";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("value", new Pattern(@"[^\r\n,""]+|""(?:[^""]|"""")*""(?!"")"));
        grammar.Add("punctuation", new Pattern(@","));
        return grammar;
    }
}
