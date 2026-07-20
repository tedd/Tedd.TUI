using System;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.Markdown;

/// <summary>
/// Container for a fenced markdown code block. Wraps a syntax-highlighted
/// <see cref="CodeDocument"/> in a single-line frame whose title is the code language,
/// paints a distinct background, caps its visible height (scrollbars appear when the code
/// is taller/wider than the viewport), and shows a "Copy" affordance in the top-right
/// corner while hovered. Clicking it copies the raw code to the <see cref="Clipboard"/>.
/// </summary>
public class MarkdownCodeBlock : Border
{
    // The unhighlighted source text, copied verbatim to the clipboard.
    private readonly string _code;

    // Copy-button hit region on the top border line, in element-local coordinates.
    // (-1 = not currently drawn, e.g. when the block is not hovered.)
    private int _copyButtonStart = -1;
    private int _copyButtonEnd = -1;

    // Latches to true after a successful copy so the label reads "Copied"; reset when
    // the pointer leaves the block.
    private bool _copied;

    private readonly TuiColor _copyForeground;
    private readonly TuiColor _copyBackground;
    private readonly TuiColor _copiedBackground;

    private const string CopyLabel = " Copy ";
    private const string CopiedLabel = " Copied ";

    public MarkdownCodeBlock(string code, string language, CodeDocument content, MarkdownTheme theme)
    {
        _code = code ?? string.Empty;

        BoxStyle = BoxStyle.Single;
        // A single-cell frame with light horizontal breathing room; no vertical padding
        // so the line cap counts actual code rows.
        Padding = new Thickness(1, 0, 1, 0);

        // Scrollbars ride the border line (they don't steal a content row/column) and only
        // appear when the code overflows the capped viewport.
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

        Background = theme.CodeBlock.Background ?? TuiColor.Black;
        BorderColor = theme.CodeBlockBorder ?? TuiColor.DarkGray;

        _copyForeground = theme.CodeBlockCopyForeground ?? TuiColor.Black;
        _copyBackground = theme.CodeBlockCopyBackground ?? TuiColor.Cyan;
        _copiedBackground = theme.CodeBlockCopiedBackground ?? TuiColor.Green;

        Content = content;

        if (!string.IsNullOrEmpty(language))
        {
            Title = new TextBlock
            {
                Text = " " + language + " ",
                Foreground = theme.CodeBlock.Foreground ?? TuiColor.White,
                Background = Background
            };
        }

        // Cap the visible height to the configured number of code lines. A shorter block
        // shrinks to fit; a taller one keeps this height and scrolls. +2 accounts for the
        // top and bottom border lines (vertical padding is zero).
        int lineCount = CountLines(_code);
        int maxLines = Math.Max(1, theme.MaxVisibleCodeLines);
        int visibleLines = Math.Min(Math.Max(1, lineCount), maxLines);
        Height = visibleLines + 2;
    }

    private static int CountLines(string code)
    {
        if (string.IsNullOrEmpty(code)) return 1;
        int count = 1;
        foreach (char c in code)
        {
            if (c == '\n') count++;
        }
        return count;
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        base.Render(buffer, offsetX, offsetY);

        int w = RenderSize.Width;
        int h = RenderSize.Height;
        if (w <= 0 || h <= 0) return;

        // The copy affordance only appears on hover and only when the top border is wide
        // enough to host it clear of the corners.
        string label = _copied ? CopiedLabel : CopyLabel;
        if (!IsMouseOver || w < label.Length + 4)
        {
            _copyButtonStart = -1;
            _copyButtonEnd = -1;
            return;
        }

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Right-aligned on the top border, ending one cell before the top-right corner.
        int localStart = w - 1 - label.Length;
        var bg = _copied ? _copiedBackground : _copyBackground;
        for (int i = 0; i < label.Length; i++)
        {
            buffer.SetPixel(x + localStart + i, y, label[i], _copyForeground, bg);
        }

        _copyButtonStart = localStart;
        _copyButtonEnd = localStart + label.Length; // exclusive
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        // A click on the top-border copy region copies the raw code. e.X/e.Y are local.
        if (e.Y == 0 && _copyButtonStart >= 0 && e.X >= _copyButtonStart && e.X < _copyButtonEnd)
        {
            Clipboard.SetText(_code);
            _copied = true;
            Invalidate();
            e.Handled = true;
        }
    }

    public override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        // Drop the "Copied" latch and hide the button once the pointer leaves.
        _copied = false;
        _copyButtonStart = -1;
        _copyButtonEnd = -1;
    }
}
