using System;

namespace Tedd.TUI;

public class CheckBox : ToggleButton
{
    public CheckBox()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(TuiColor), typeof(CheckBox), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.Register("HoverForeground", typeof(TuiColor), typeof(CheckBox), TuiColor.Cyan);

    /// <summary>Label foreground used while the mouse hovers the control and it is not focused.</summary>
    public TuiColor HoverForeground
    {
        get => (TuiColor)GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public static readonly DependencyProperty CheckColorProperty =
        DependencyProperty.Register("CheckColor", typeof(TuiColor), typeof(CheckBox), TuiColor.Green);

    public TuiColor CheckColor
    {
        get => (TuiColor)GetValue(CheckColorProperty);
        set => SetValue(CheckColorProperty, value);
    }

    public static readonly DependencyProperty BracketColorProperty =
        DependencyProperty.Register("BracketColor", typeof(TuiColor), typeof(CheckBox), TuiColor.Gray);

    public TuiColor BracketColor
    {
        get => (TuiColor)GetValue(BracketColorProperty);
        set => SetValue(BracketColorProperty, value);
    }

    public static readonly DependencyProperty CheckedCharProperty =
        DependencyProperty.Register("CheckedChar", typeof(char), typeof(CheckBox), '√');

    public char CheckedChar
    {
        get => (char)GetValue(CheckedCharProperty);
        set => SetValue(CheckedCharProperty, value);
    }

    public static readonly DependencyProperty UncheckedCharProperty =
        DependencyProperty.Register("UncheckedChar", typeof(char), typeof(CheckBox), ' ');

    public char UncheckedChar
    {
        get => (char)GetValue(UncheckedCharProperty);
        set => SetValue(UncheckedCharProperty, value);
    }

    public static readonly DependencyProperty IndeterminateCharProperty =
        DependencyProperty.Register("IndeterminateChar", typeof(char), typeof(CheckBox), '-');

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

        var fg = IsFocused ? FocusedForeground : IsMouseOver ? HoverForeground : Foreground;
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
