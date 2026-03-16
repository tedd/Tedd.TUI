using System;

namespace Tedd.TUI.Markdown;

public class Image : UIElement
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(string), typeof(Image), string.Empty);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty AltTextProperty =
        DependencyProperty.Register("AltText", typeof(string), typeof(Image), string.Empty);

    public string AltText
    {
        get => (string)GetValue(AltTextProperty);
        set => SetValue(AltTextProperty, value);
    }

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = $"[{AltText}]";
        if (string.IsNullOrEmpty(AltText)) text = "[Image]";
        return new Size(text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        // Render Alt Text for Console
        string text = $"[{AltText}]";
        if (string.IsNullOrEmpty(AltText)) text = "[Image]";

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var bg = Background ?? buffer.GetPixel(x, y).Background;

        for (int i = 0; i < text.Length; i++)
        {
            if (i < RenderSize.Width && RenderSize.Height > 0)
            {
                buffer.SetPixel(x + i, y, text[i], Foreground, bg);
            }
        }
    }
}
