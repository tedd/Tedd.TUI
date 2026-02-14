using System.Collections.Generic;
using Tedd.TUI.CodeColoring.Languages;

namespace Tedd.TUI.CodeColoring;

public static class LanguageRegistry
{
    private static Dictionary<string, Grammar> _grammars = new Dictionary<string, Grammar>();

    public static Grammar GetGrammar(string language)
    {
        if (_grammars.ContainsKey(language))
        {
            return _grammars[language];
        }

        // Lazy loading
        var grammar = LoadGrammar(language);
        if (grammar != null)
        {
            _grammars[language] = grammar;
        }
        return grammar;
    }

    private static Grammar LoadGrammar(string language)
    {
        switch (language.ToLower())
        {
            case "clike": return CLike.GetGrammar();
            case "markup": return Markup.GetGrammar();
            case "xml": return Markup.GetGrammar();
            case "html": return Markup.GetGrammar();
            case "regex": return RegexLang.GetGrammar();
            case "csharp": return CSharp.GetGrammar();
            case "cs": return CSharp.GetGrammar();
            case "bash": return Bash.GetGrammar();
            case "sh": return Bash.GetGrammar();
            case "shell": return Bash.GetGrammar();
            case "basic": return Basic.GetGrammar();
            case "batch": return Batch.GetGrammar();
            case "powershell": return PowerShell.GetGrammar();
            case "json": return Json.GetGrammar();
            case "yaml": return Yaml.GetGrammar();
            default: return null;
        }
    }
}
