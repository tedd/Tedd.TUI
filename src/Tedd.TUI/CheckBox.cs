using System;

namespace Tedd.TUI;

public class CheckBox : UIElement
{
    public CheckBox()
    {
        Focusable = true;
    }
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register("IsChecked", typeof(bool), typeof(CheckBox), false);

    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(string), typeof(CheckBox), string.Empty);

    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Content;
        // [x] Text
        return new Size(4 + text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var fg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;
        var bg = ConsoleColor.Black;

        buffer.SetPixel(x, y, '[', ConsoleColor.Gray, bg);
        buffer.SetPixel(x + 1, y, IsChecked ? 'x' : ' ', ConsoleColor.Green, bg);
        buffer.SetPixel(x + 2, y, ']', ConsoleColor.Gray, bg);

        string text = Content;
        for (int i = 0; i < text.Length; i++)
        {
            buffer.SetPixel(x + 4 + i, y, text[i], fg, bg);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        Toggle();
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            Toggle();
            e.Handled = true;
        }
    }

    private void Toggle()
    {
        IsChecked = !IsChecked;
    }
}
