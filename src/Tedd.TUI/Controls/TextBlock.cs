using System;
using System.Collections.Generic;

namespace Tedd.TUI.Controls;

public enum TextWrapping
{
    NoWrap,
    Wrap
}

public class TextBlock : UIElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextBlock), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(TextBlock), TextWrapping.NoWrap);

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
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

        if (TextWrapping == TextWrapping.NoWrap || availableSize.Width <= 0)
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

        if (TextWrapping == TextWrapping.NoWrap)
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

    internal static void WrapSingleLine(string line, int maxWidth, List<string> output)
    {
        if (line.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }

        int i = 0;
        int n = line.Length;

        // Optimization: Replaced System.Text.StringBuilder with stackalloc Span<char> and ArrayPool<char>.
        // Time Complexity: O(N) where N is the length of the string line.
        // Space Complexity: O(M) where M is the maxWidth, utilizing stack memory for normal cases (<= 2048).
        // If maxWidth is extremely large, fallback to array to avoid StackOverflow, but TUI widths are typically < 1000.
        char[]? arrayPoolBuffer = null;

        try
        {
            Span<char> current = maxWidth <= 2048 ? stackalloc char[maxWidth] : (arrayPoolBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(maxWidth)).AsSpan(0, maxWidth);

            int currentLen = 0;
            ReadOnlySpan<char> lineSpan = line.AsSpan();

            while (i < n)
            {
                // Consume leading whitespace at start of line (skip), elsewhere keep one space between words.
                if (lineSpan[i] == ' ')
                {
                    if (currentLen > 0 && currentLen < maxWidth)
                    {
                        current[currentLen++] = ' ';
                    }
                    i++;
                    continue;
                }

                // Read next word.
                int wordStart = i;
                while (i < n && lineSpan[i] != ' ') i++;
                int wordLen = i - wordStart;

                if (wordLen > maxWidth)
                {
                    // Hard-break a word that's too long for the line width.
                    if (currentLen > 0)
                    {
                        // TrimEnd
                        int trimLen = currentLen;
                        while (trimLen > 0 && char.IsWhiteSpace(current[trimLen - 1])) trimLen--;
                        output.Add(new string(current.Slice(0, trimLen)));
                        currentLen = 0;
                    }

                    int pos = wordStart;
                    while (pos < wordStart + wordLen)
                    {
                        int take = Math.Min(maxWidth, wordStart + wordLen - pos);
                        output.Add(new string(lineSpan.Slice(pos, take)));
                        pos += take;
                    }
                    continue;
                }

                if (currentLen + wordLen > maxWidth)
                {
                    int trimLen = currentLen;
                    while (trimLen > 0 && char.IsWhiteSpace(current[trimLen - 1])) trimLen--;
                    output.Add(new string(current.Slice(0, trimLen)));
                    currentLen = 0;
                }

                lineSpan.Slice(wordStart, wordLen).CopyTo(current.Slice(currentLen));
                currentLen += wordLen;
            }

            if (currentLen > 0)
            {
                int trimLen = currentLen;
                while (trimLen > 0 && char.IsWhiteSpace(current[trimLen - 1])) trimLen--;
                output.Add(new string(current.Slice(0, trimLen)));
            }
        }
        finally
        {
            if (arrayPoolBuffer != null)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(arrayPoolBuffer);
            }
        }
    }
}
