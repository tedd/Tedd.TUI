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

        // Draw Border (Single Box)
        char hLine = '─';
        char vLine = '│';
        char tl = '┌';
        char tr = '┐';
        char bl = '└';
        char br = '┘';

        // Horizontal
        for (int i = 1; i < w - 1; i++)
        {
            buffer.SetPixel(x + i, y, hLine, ConsoleColor.Gray, ConsoleColor.Black);
            buffer.SetPixel(x + i, y + h - 1, hLine, ConsoleColor.Gray, ConsoleColor.Black);
        }
        // Vertical
        for (int i = 1; i < h - 1; i++)
        {
            buffer.SetPixel(x, y + i, vLine, ConsoleColor.Gray, ConsoleColor.Black);
            buffer.SetPixel(x + w - 1, y + i, vLine, ConsoleColor.Gray, ConsoleColor.Black);
        }
        // Corners
        buffer.SetPixel(x, y, tl, ConsoleColor.Gray, ConsoleColor.Black);
        buffer.SetPixel(x + w - 1, y, tr, ConsoleColor.Gray, ConsoleColor.Black);
        buffer.SetPixel(x, y + h - 1, bl, ConsoleColor.Gray, ConsoleColor.Black);
        buffer.SetPixel(x + w - 1, y + h - 1, br, ConsoleColor.Gray, ConsoleColor.Black);

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
