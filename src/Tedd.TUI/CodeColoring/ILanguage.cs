using System.Collections.Generic;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.CodeColoring;

public interface ILanguage
{
    string Id { get; }
    string[] Aliases { get; }
    Grammar GetGrammar();
}
