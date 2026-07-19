using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring;

public class Pattern
{
    public Regex Regex { get; set; }
    public bool Lookbehind { get; set; }
    public bool Greedy { get; set; }
    public string? Alias { get; set; }
    public Grammar? Inside { get; set; }

    public Pattern(string regexPattern, string regexOptions = "", bool lookbehind = false, bool greedy = false, string? alias = null, Grammar? inside = null)
    {
        RegexOptions options = RegexOptions.Compiled;
        if (regexOptions.Contains("i")) options |= RegexOptions.IgnoreCase;
        if (regexOptions.Contains("m")) options |= RegexOptions.Multiline;
        if (regexOptions.Contains("s")) options |= RegexOptions.Singleline;

        // JS flag mapping: 'i' -> IgnoreCase, 'm' (^/$ match line breaks) -> Multiline,
        // 's' (dot matches newline) -> Singleline.

        Regex = new Regex(regexPattern, options);
        Lookbehind = lookbehind;
        Greedy = greedy;
        Alias = alias;
        Inside = inside;
    }

    public Pattern(Regex regex, bool lookbehind = false, bool greedy = false, string? alias = null, Grammar? inside = null)
    {
        Regex = regex;
        Lookbehind = lookbehind;
        Greedy = greedy;
        Alias = alias;
        Inside = inside;
    }
}
