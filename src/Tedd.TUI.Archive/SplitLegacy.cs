using System;
using System.Collections.Generic;

namespace Tedd.TUI.Archive
{
    public class SplitLegacy
    {
        public static (string, string)? SetAttachedProperty_Legacy(string name)
        {
            var parts = name.Split('.');
            if (parts.Length != 2) return null;

            string ownerType = parts[0];
            string propName = parts[1];
            return (ownerType, propName);
        }

        public static List<string> TextEditorLines_Legacy(string text)
        {
            return new List<string>(text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
        }

        public static List<string> ParseTableLine_Legacy(string line)
        {
            var parts = line.Split('|');
            var result = new List<string>();
            foreach (var p in parts)
            {
                if (!string.IsNullOrWhiteSpace(p)) result.Add(p.Trim());
            }
            return result;
        }

        public static List<string> ParseInlineWords_Legacy(string text)
        {
            var words = text.Split(' ');
            var result = new List<string>();
            for (int i = 0; i < words.Length; i++)
            {
                result.Add(words[i]);
            }
            return result;
        }
    }
}
