using System;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI.Archive;


public class TextBlockLegacy : UIElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextBlockLegacy), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register("TextWrapping", typeof(Tedd.TUI.TextWrapping), typeof(TextBlockLegacy), Tedd.TUI.TextWrapping.NoWrap);

    public Tedd.TUI.TextWrapping TextWrapping
    {
        get => (Tedd.TUI.TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    // Cached wrapping result so we don't re-wrap between Measure and Render with the same width.
    private List<string>? _cachedLines;
    private int _cachedWrapWidth = -1;
    private string? _cachedWrapText;

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Text;
        if (string.IsNullOrEmpty(text))
            return new Size(0, 0);

        if (TextWrapping == Tedd.TUI.TextWrapping.NoWrap || availableSize.Width <= 0)
        {
            _cachedLines = null;
            _cachedWrapWidth = -1;
            _cachedWrapText = null;
            return new Size(text.Length, 1);
        }

        var lines = WrapText(text, availableSize.Width);
        _cachedLines = lines;
        _cachedWrapWidth = availableSize.Width;
        _cachedWrapText = text;

        int maxWidth = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Length > maxWidth) maxWidth = lines[i].Length;
        }
        return new Size(maxWidth, lines.Count);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        string text = Text;
        if (string.IsNullOrEmpty(text)) return;

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        if (TextWrapping == Tedd.TUI.TextWrapping.NoWrap)
        {
            RenderLine(buffer, x, y, text);
            return;
        }

        // Use the cached wrap if it matches the current render width; otherwise re-wrap.
        List<string> lines;
        if (_cachedLines != null && _cachedWrapWidth == RenderSize.Width && _cachedWrapText == text)
        {
            lines = _cachedLines;
        }
        else
        {
            lines = WrapText(text, RenderSize.Width);
            _cachedLines = lines;
            _cachedWrapWidth = RenderSize.Width;
            _cachedWrapText = text;
        }

        int maxRows = Math.Min(lines.Count, RenderSize.Height);
        for (int row = 0; row < maxRows; row++)
        {
            RenderLine(buffer, x, y + row, lines[row]);
        }
    }

    private void RenderLine(VirtualBuffer buffer, int x, int y, string line)
    {
        int maxLen = Math.Min(line.Length, RenderSize.Width);
        for (int i = 0; i < maxLen; i++)
        {
            if (RenderSize.Height <= 0) return;
            var bg = Background ?? buffer.GetPixel(x + i, y).Background;
            buffer.SetPixel(x + i, y, line[i], Foreground, bg);
        }
    }

    /// <summary>
    /// Word-wraps the input text to fit within <paramref name="maxWidth"/>. Words longer
    /// than <paramref name="maxWidth"/> are hard-broken. Preserves explicit line breaks in
    /// the source string (\n or \r\n).
    /// </summary>
    private static List<string> WrapText(string text, int maxWidth)
    {
        var result = new List<string>();
        if (maxWidth <= 0)
        {
            result.Add(string.Empty);
            return result;
        }

        // Split on explicit newlines first so they always force a break.
        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            WrapSingleLine(rawLine, maxWidth, result);
        }
        if (result.Count == 0) result.Add(string.Empty);
        return result;
    }

    private static void WrapSingleLine(string line, int maxWidth, List<string> output)
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
            // Consume leading whitespace at start of line (skip), elsewhere keep one space between words.
            if (line[i] == ' ')
            {
                if (current.Length > 0 && current.Length < maxWidth)
                {
                    current.Append(' ');
                }
                i++;
                continue;
            }

            // Read next word.
            int wordStart = i;
            while (i < n && line[i] != ' ') i++;
            int wordLen = i - wordStart;

            if (wordLen > maxWidth)
            {
                // Hard-break a word that's too long for the line width.
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
