using System.Text.RegularExpressions;
using System.Linq;

namespace Tedd.TUI.CodeColoring;

public static class RegexUtils
{
    public static string Replace(string pattern, params string[] replacements)
    {
        return Regex.Replace(pattern, @"<<(\d+)>>", m =>
        {
            int index = int.Parse(m.Groups[1].Value);
            if (index >= 0 && index < replacements.Length)
            {
                return "(?:" + replacements[index] + ")";
            }
            return m.Value;
        });
    }

    public static string Nested(string pattern, int depthLog2)
    {
        for (int i = 0; i < depthLog2; i++)
        {
            pattern = pattern.Replace("<<self>>", "(?:" + pattern + ")");
        }
        return pattern.Replace("<<self>>", "[^\\s\\S]");
    }
}
