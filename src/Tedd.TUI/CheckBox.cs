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

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(ConsoleColor), typeof(CheckBox), ConsoleColor.Yellow);

    public ConsoleColor FocusedForeground
    {
        get { return (ConsoleColor)GetValue(FocusedForegroundProperty); }
        set { SetValue(FocusedForegroundProperty, value); }
    }

    public static readonly DependencyProperty CheckColorProperty =
        DependencyProperty.Register("CheckColor", typeof(ConsoleColor), typeof(CheckBox), ConsoleColor.Green);

    public ConsoleColor CheckColor
    {
        get { return (ConsoleColor)GetValue(CheckColorProperty); }
        set { SetValue(CheckColorProperty, value); }
    }

    public static readonly DependencyProperty BracketColorProperty =
        DependencyProperty.Register("BracketColor", typeof(ConsoleColor), typeof(CheckBox), ConsoleColor.Gray);

    public ConsoleColor BracketColor
    {
        get { return (ConsoleColor)GetValue(BracketColorProperty); }
        set { SetValue(BracketColorProperty, value); }
    }

    public static readonly DependencyProperty CheckedCharProperty =
        DependencyProperty.Register("CheckedChar", typeof(char), typeof(CheckBox), '√');

    public char CheckedChar
    {
        get { return (char)GetValue(CheckedCharProperty); }
        set { SetValue(CheckedCharProperty, value); }
    }

    public static readonly DependencyProperty UncheckedCharProperty =
        DependencyProperty.Register("UncheckedChar", typeof(char), typeof(CheckBox), ' ');

    public char UncheckedChar
    {
        get { return (char)GetValue(UncheckedCharProperty); }
        set { SetValue(UncheckedCharProperty, value); }
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

        var fg = IsFocused ? FocusedForeground : Foreground;
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        buffer.SetPixel(x, y, '[', BracketColor, bg);
        buffer.SetPixel(x + 1, y, IsChecked ? CheckedChar : UncheckedChar, CheckColor, bg);
        buffer.SetPixel(x + 2, y, ']', BracketColor, bg);

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
