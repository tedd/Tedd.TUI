using System;
using System.Collections.Generic;

namespace Tedd.TUI.Archive
{
    public class SplitOptimized
    {
        public static (string, string)? SetAttachedProperty_Optimized(string name)
        {
            ReadOnlySpan<char> nameSpan = name.AsSpan();
            int dotIdx = nameSpan.IndexOf('.');
            if (dotIdx == -1 || nameSpan.Slice(dotIdx + 1).IndexOf('.') != -1) return null;

            string ownerType = nameSpan.Slice(0, dotIdx).ToString();
            string propName = nameSpan.Slice(dotIdx + 1).ToString();
            return (ownerType, propName);
        }

        public static List<string> TextEditorLines_Optimized(string text)
        {
            var _lines = new List<string>();
            var span = text.AsSpan();
            foreach (var line in span.EnumerateLines())
            {
                _lines.Add(line.ToString());
            }

            if (span.Length > 0 && (span[^1] == '\n' || span[^1] == '\r'))
            {
                _lines.Add("");
            }
            return _lines;
        }

        public static List<string> ParseTableLine_Optimized(string line)
        {
            ReadOnlySpan<char> span = line.AsSpan();
            var result = new List<string>();
            int start = 0;
            while (start < span.Length)
            {
                int end = span.Slice(start).IndexOf('|');
                if (end == -1)
                {
                    var p = span.Slice(start);
                    if (!p.IsWhiteSpace()) result.Add(p.Trim().ToString());
                    break;
                }
                var p2 = span.Slice(start, end);
                if (!p2.IsWhiteSpace()) result.Add(p2.Trim().ToString());
                start += end + 1;
            }
            return result;
        }

        public static List<string> ParseInlineWords_Optimized(string text)
        {
            ReadOnlySpan<char> span = text.AsSpan();
            var result = new List<string>();
            int start = 0;
            while (start < span.Length)
            {
                int end = span.Slice(start).IndexOf(' ');
                string word;
                bool isLast = false;

                if (end == -1)
                {
                    word = span.Slice(start).ToString();
                    isLast = true;
                    start = span.Length;
                }
                else
                {
                    word = span.Slice(start, end).ToString();
                    start += end + 1;
                }
                result.Add(word);
            }
            return result;
        }
    }
}
