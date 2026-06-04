using System;
using System.Collections.Generic;

namespace Tedd.TUI.Archive.Controls;

public class TextBlockLegacy
{
    // Keeping a stripped down version of WrapSingleLine for benchmarking.
    public static void WrapSingleLine(string line, int maxWidth, List<string> output)
    {
        if (line.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }

        int i = 0;
        int n = line.Length;
        var current = new System.Text.StringBuilder(maxWidth);

        while (i < n)
        {
            if (line[i] == ' ')
            {
                if (current.Length > 0 && current.Length < maxWidth)
                {
                    current.Append(' ');
                }
                i++;
                continue;
            }

            int wordStart = i;
            while (i < n && line[i] != ' ') i++;
            int wordLen = i - wordStart;

            if (wordLen > maxWidth)
            {
                if (current.Length > 0)
                {
                    output.Add(current.ToString().TrimEnd());
                    current.Clear();
                }
                int pos = wordStart;
                while (pos < wordStart + wordLen)
                {
                    int take = Math.Min(maxWidth, wordStart + wordLen - pos);
                    output.Add(line.Substring(pos, take));
                    pos += take;
                }
                continue;
            }

            if (current.Length + wordLen > maxWidth)
            {
                output.Add(current.ToString().TrimEnd());
                current.Clear();
            }

            current.Append(line, wordStart, wordLen);
        }

        if (current.Length > 0)
        {
            output.Add(current.ToString().TrimEnd());
        }
    }
}
