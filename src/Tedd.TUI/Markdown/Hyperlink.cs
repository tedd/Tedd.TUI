using System;

namespace Tedd.TUI.Markdown;

public class Hyperlink : UIElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(Hyperlink), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty UrlProperty =
        DependencyProperty.Register("Url", typeof(string), typeof(Hyperlink), string.Empty);

    public string Url
    {
        get => (string)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.Register("HoverForeground", typeof(TuiColor), typeof(Hyperlink), TuiColor.Cyan);

    /// <summary>
    /// Foreground used while the mouse hovers the link and it is not focused. Terminals can't
    /// render true underlines per-cell, so hover feedback is a color shift (default cyan)
    /// rather than an underline.
    /// </summary>
    public TuiColor HoverForeground
    {
        get => (TuiColor)GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public event EventHandler? Click;

    public Hyperlink()
    {
        Focusable = true;
        // Default style from theme will be applied by renderer or parent?
        // Actually, we should set default Foreground here or rely on Theme?
        // But UIElement doesn't know about MarkdownTheme.
        // We will likely set Foreground when creating this control in MarkdownRenderer.
        Foreground = TuiColor.Blue;
    }

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Text;
        if (string.IsNullOrEmpty(text))
            return new Size(0, 0);

        return new Size(text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        string text = Text;
        if (string.IsNullOrEmpty(text)) return;

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Focus wins over hover so a keyboard user keeps the focus highlight even when the
        // pointer happens to rest on the focused link.
        var fg = IsFocused ? TuiColor.Cyan : IsMouseOver ? HoverForeground : Foreground;
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        for (int i = 0; i < text.Length; i++)
        {
            // Clip
            if (i < RenderSize.Width && RenderSize.Height > 0)
            {
                buffer.SetPixel(x + i, y, text[i], fg, bg);
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        Click?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            Click?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
