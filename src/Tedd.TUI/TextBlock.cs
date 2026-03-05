using System;

namespace Tedd.TUI;

public class TextBlock : UIElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextBlock), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

        for (int i = 0; i < text.Length; i++)
        {
            // Clip to bounds
            if (i < RenderSize.Width && RenderSize.Height > 0)
                if (i < RenderSize.Width && RenderSize.Height > 0)
                {
                    var bg = Background ?? buffer.GetPixel(x + i, y).Background;
                    buffer.SetPixel(x + i, y, text[i], Foreground, bg);
                }
        }
    }
}
