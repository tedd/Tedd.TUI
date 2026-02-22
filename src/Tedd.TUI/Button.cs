namespace Tedd.TUI;

public class Button : UIElement
{
    public Button()
    {
        Focusable = true;
    }
    public string Content
    {
        get => (string)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(string), typeof(Button), string.Empty);

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(Button), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    public static readonly RoutedEvent ClickEvent =
        RoutedEvent.Register("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Button));

    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
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

        var borderFg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.Gray;
        var textFg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;
        // Check first pixel for default background if transparent
        var bg = Background ?? buffer.GetPixel(x, y).Background;

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
