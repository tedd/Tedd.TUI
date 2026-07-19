using System;
using System.Collections.Generic;

namespace Tedd.TUI;

/// <summary>
/// One parsed XAML markup extension: <c>{Name positional, Key=Value, ...}</c>.
/// Values keep nested extensions (e.g. <c>{RelativeSource Self}</c>) as raw text for
/// the consumer to parse recursively.
/// </summary>
internal sealed class ParsedMarkupExtension
{
    public string Name = "";
    public readonly List<(string? Key, string Value)> Arguments = [];

    /// <summary>First positional (key-less) argument, or null.</summary>
    public string? Positional
    {
        get
        {
            foreach (var (key, value) in Arguments)
            {
                if (key == null) return value;
            }
            return null;
        }
    }
}

/// <summary>
/// Tokenizer for XAML markup-extension attribute syntax. Handles nested braces
/// (<c>Converter={StaticResource X}</c>), single-quoted values
/// (<c>StringFormat='{}{0} items'</c>) and the <c>{}</c> literal escape.
/// </summary>
internal static class MarkupExtensionParser
{
    /// <summary>True when the attribute value is a markup extension (and not the {} escape).</summary>
    public static bool IsExtension(string value) =>
        value.Length >= 2
        && value[0] == '{'
        && value[^1] == '}'
        && !value.StartsWith("{}", StringComparison.Ordinal);

    public static ParsedMarkupExtension Parse(string text)
    {
        if (!IsExtension(text))
            throw new FormatException($"'{text}' is not a markup extension.");

        string inner = text.Substring(1, text.Length - 2).Trim();

        var result = new ParsedMarkupExtension();
        int nameEnd = 0;
        while (nameEnd < inner.Length && !char.IsWhiteSpace(inner[nameEnd])) nameEnd++;
        result.Name = inner.Substring(0, nameEnd);

        string rest = inner.Substring(nameEnd).Trim();
        if (rest.Length == 0) return result;

        foreach (string rawPart in SplitTopLevel(rest, ','))
        {
            string part = rawPart.Trim();
            if (part.Length == 0) continue;

            int eq = FindTopLevel(part, '=');
            if (eq < 0)
            {
                result.Arguments.Add((null, Unquote(part)));
            }
            else
            {
                result.Arguments.Add((part.Substring(0, eq).Trim(), Unquote(part.Substring(eq + 1).Trim())));
            }
        }

        return result;
    }

    /// <summary>Splits on a separator at brace depth 0 and outside single quotes.</summary>
    private static IEnumerable<string> SplitTopLevel(string s, char separator)
    {
        int depth = 0;
        bool quoted = false;
        int start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (quoted)
            {
                if (c == '\'') quoted = false;
            }
            else if (c == '\'') quoted = true;
            else if (c == '{') depth++;
            else if (c == '}') depth--;
            else if (c == separator && depth == 0)
            {
                yield return s.Substring(start, i - start);
                start = i + 1;
            }
        }
        yield return s.Substring(start);
    }

    /// <summary>Index of the first occurrence at brace depth 0 outside quotes, or -1.</summary>
    private static int FindTopLevel(string s, char target)
    {
        int depth = 0;
        bool quoted = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (quoted)
            {
                if (c == '\'') quoted = false;
            }
            else if (c == '\'') quoted = true;
            else if (c == '{') depth++;
            else if (c == '}') depth--;
            else if (c == target && depth == 0) return i;
        }
        return -1;
    }

    private static string Unquote(string s) =>
        s.Length >= 2 && s[0] == '\'' && s[^1] == '\'' ? s.Substring(1, s.Length - 2) : s;
}
