using System;

namespace Tedd.TUI;

public class Button : UIElement
{
    public Button()
    {
        Focusable = true;
    }
    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(string), typeof(Button), string.Empty);

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(Button), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get { return (BoxStyle)GetValue(BoxStyleProperty); }
        set { SetValue(BoxStyleProperty, value); }
    }

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register("Foreground", typeof(ConsoleColor), typeof(Button), ConsoleColor.White);

    public ConsoleColor Foreground
    {
        get { return (ConsoleColor)GetValue(ForegroundProperty); }
        set { SetValue(ForegroundProperty, value); }
    }

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(ConsoleColor), typeof(Button), ConsoleColor.Gray);

    public ConsoleColor BorderColor
    {
        get { return (ConsoleColor)GetValue(BorderColorProperty); }
        set { SetValue(BorderColorProperty, value); }
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(ConsoleColor), typeof(Button), ConsoleColor.Yellow);

    public ConsoleColor FocusedForeground
    {
        get { return (ConsoleColor)GetValue(FocusedForegroundProperty); }
        set { SetValue(FocusedForegroundProperty, value); }
    }

    public static readonly DependencyProperty FocusedBorderColorProperty =
        DependencyProperty.Register("FocusedBorderColor", typeof(ConsoleColor), typeof(Button), ConsoleColor.Yellow);

    public ConsoleColor FocusedBorderColor
    {
        get { return (ConsoleColor)GetValue(FocusedBorderColorProperty); }
        set { SetValue(FocusedBorderColorProperty, value); }
    }

    public static readonly RoutedEvent ClickEvent =
        RoutedEvent.Register("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Button));

    public event RoutedEventHandler Click
    {
        add { AddHandler(ClickEvent, value); }
        remove { RemoveHandler(ClickEvent, value); }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            e.Handled = true;
        }
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
        var chars = BoxDrawingChars.Get(BoxStyle);

        var borderFg = IsFocused ? FocusedBorderColor : BorderColor;
        var textFg = IsFocused ? FocusedForeground : Foreground;
        // Check first pixel for default background if transparent
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        // Fill background
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                // Only fill if not border? Or overwrite with border later?
                // Overwriting later is simpler but less efficient.
                // Since we iterate anyway, let's fill everything.
                // However, borders will be drawn on top.
                buffer.SetPixel(x + i, y + j, ' ', textFg, bg);
            }
        }

        // Draw Border (Unicode box drawing)
        // Top/Bottom
        for (int i = 0; i < w; i++)
        {
            buffer.SetPixel(x + i, y, i == 0 ? chars.TopLeft : i == w - 1 ? chars.TopRight : chars.Horizontal, borderFg, bg);
            buffer.SetPixel(x + i, y + h - 1, i == 0 ? chars.BottomLeft : i == w - 1 ? chars.BottomRight : chars.Horizontal, borderFg, bg);
        }
        // Left/Right (excluding corners already drawn)
        for (int i = 1; i < h - 1; i++)
        {
            buffer.SetPixel(x, y + i, chars.Vertical, borderFg, bg);
            buffer.SetPixel(x + w - 1, y + i, chars.Vertical, borderFg, bg);
        }

        // Draw Text
        int textX = x + (w - text.Length) / 2;
        int textY = y + (h - 1) / 2;
        for (int i = 0; i < text.Length; i++)
        {
             if (textX + i > x && textX + i < x + w - 1)
                buffer.SetPixel(textX + i, textY, text[i], textFg, bg);
        }
    }
}
