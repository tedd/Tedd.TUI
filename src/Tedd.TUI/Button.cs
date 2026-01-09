using System;

namespace Tedd.TUI;

public class Button : UIElement
{
    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(string), typeof(Button), string.Empty);

    public event EventHandler Click;

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        Click?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Content;
        // Button padding [ Text ]
        return new Size(text.Length + 4, 3);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;
        string text = Content;

        // Draw Border (Simple Box)
        // Top/Bottom
        for (int i = 0; i < w; i++)
        {
            buffer.SetPixel(x + i, y, '-', ConsoleColor.Gray, ConsoleColor.Black);
            buffer.SetPixel(x + i, y + h - 1, '-', ConsoleColor.Gray, ConsoleColor.Black);
        }
        // Left/Right
        for (int i = 0; i < h; i++)
        {
            buffer.SetPixel(x, y + i, '|', ConsoleColor.Gray, ConsoleColor.Black);
            buffer.SetPixel(x + w - 1, y + i, '|', ConsoleColor.Gray, ConsoleColor.Black);
        }
        // Corners
        buffer.SetPixel(x, y, '+', ConsoleColor.Gray, ConsoleColor.Black);
        buffer.SetPixel(x + w - 1, y, '+', ConsoleColor.Gray, ConsoleColor.Black);
        buffer.SetPixel(x, y + h - 1, '+', ConsoleColor.Gray, ConsoleColor.Black);
        buffer.SetPixel(x + w - 1, y + h - 1, '+', ConsoleColor.Gray, ConsoleColor.Black);

        // Draw Text
        int textX = x + (w - text.Length) / 2;
        int textY = y + (h - 1) / 2;
        for (int i = 0; i < text.Length; i++)
        {
             if (textX + i > x && textX + i < x + w - 1)
                buffer.SetPixel(textX + i, textY, text[i], ConsoleColor.White, ConsoleColor.Black);
        }
    }
}
