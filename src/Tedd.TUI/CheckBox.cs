using System;

namespace Tedd.TUI;

public class CheckBox : ToggleButton
{
    public CheckBox()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register(nameof(FocusedForeground), typeof(ConsoleColor), typeof(CheckBox), ConsoleColor.Yellow);

    public ConsoleColor FocusedForeground
    {
        get => (ConsoleColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty CheckColorProperty =
        DependencyProperty.Register(nameof(CheckColor), typeof(ConsoleColor), typeof(CheckBox), ConsoleColor.Green);

    public ConsoleColor CheckColor
    {
        get => (ConsoleColor)GetValue(CheckColorProperty);
        set => SetValue(CheckColorProperty, value);
    }

    public static readonly DependencyProperty BracketColorProperty =
        DependencyProperty.Register(nameof(BracketColor), typeof(ConsoleColor), typeof(CheckBox), ConsoleColor.Gray);

    public ConsoleColor BracketColor
    {
        get => (ConsoleColor)GetValue(BracketColorProperty);
        set => SetValue(BracketColorProperty, value);
    }

    public static readonly DependencyProperty CheckedCharProperty =
        DependencyProperty.Register(nameof(CheckedChar), typeof(char), typeof(CheckBox), '√');

    public char CheckedChar
    {
        get => (char)GetValue(CheckedCharProperty);
        set => SetValue(CheckedCharProperty, value);
    }

    public static readonly DependencyProperty UncheckedCharProperty =
        DependencyProperty.Register(nameof(UncheckedChar), typeof(char), typeof(CheckBox), ' ');

    public char UncheckedChar
    {
        get => (char)GetValue(UncheckedCharProperty);
        set => SetValue(UncheckedCharProperty, value);
    }

    public static readonly DependencyProperty IndeterminateCharProperty =
        DependencyProperty.Register(nameof(IndeterminateChar), typeof(char), typeof(CheckBox), '-');

    public char IndeterminateChar
    {
        get => (char)GetValue(IndeterminateCharProperty);
        set => SetValue(IndeterminateCharProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Content?.ToString() ?? string.Empty;
        // [x] Text
        return new Size(4 + text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var fg = IsFocused ? FocusedForeground : Foreground;
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        char mark = UncheckedChar;
        var isChecked = IsChecked;
        if (isChecked == true)
        {
            mark = CheckedChar;
        }
        else if (isChecked == null)
        {
            mark = IndeterminateChar;
        }

        buffer.SetPixel(x, y, '[', BracketColor, bg);
        buffer.SetPixel(x + 1, y, mark, CheckColor, bg);
        buffer.SetPixel(x + 2, y, ']', BracketColor, bg);

        string text = Content?.ToString() ?? string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            buffer.SetPixel(x + 4 + i, y, text[i], fg, bg);
        }
    }
}
