using System;

namespace Tedd.TUI;

public class RadioButton : ToggleButton
{
    public RadioButton()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register("GroupName", typeof(string), typeof(RadioButton), string.Empty);

    public string GroupName
    {
        get => (string)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(TuiColor), typeof(RadioButton), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty CheckColorProperty =
        DependencyProperty.Register("CheckColor", typeof(TuiColor), typeof(RadioButton), TuiColor.Green);

    public TuiColor CheckColor
    {
        get => (TuiColor)GetValue(CheckColorProperty);
        set => SetValue(CheckColorProperty, value);
    }

    public static readonly DependencyProperty BracketColorProperty =
        DependencyProperty.Register("BracketColor", typeof(TuiColor), typeof(RadioButton), TuiColor.Gray);

    public TuiColor BracketColor
    {
        get => (TuiColor)GetValue(BracketColorProperty);
        set => SetValue(BracketColorProperty, value);
    }

    public static readonly DependencyProperty CheckedCharProperty =
        DependencyProperty.Register("CheckedChar", typeof(char), typeof(RadioButton), 'o');

    public char CheckedChar
    {
        get => (char)GetValue(CheckedCharProperty);
        set => SetValue(CheckedCharProperty, value);
    }

    public static readonly DependencyProperty UncheckedCharProperty =
        DependencyProperty.Register("UncheckedChar", typeof(char), typeof(RadioButton), ' ');

    public char UncheckedChar
    {
        get => (char)GetValue(UncheckedCharProperty);
        set => SetValue(UncheckedCharProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        if (dp == IsCheckedProperty && IsChecked == true)
        {
            UpdateGroup();
        }

        base.OnPropertyChanged(dp);
    }

    protected override void OnToggle()
    {
        if (IsChecked != true)
        {
            IsChecked = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Content?.ToString() ?? string.Empty;
        // (o) Text
        return new Size(4 + text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var fg = IsFocused ? FocusedForeground : Foreground;
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        buffer.SetPixel(x, y, '(', BracketColor, bg);
        buffer.SetPixel(x + 1, y, IsChecked == true ? CheckedChar : UncheckedChar, CheckColor, bg);
        buffer.SetPixel(x + 2, y, ')', BracketColor, bg);

        string text = Content?.ToString() ?? string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            buffer.SetPixel(x + 4 + i, y, text[i], fg, bg);
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == ConsoleKey.UpArrow || e.Key == ConsoleKey.LeftArrow)
        {
            NavigateToSibling(-1);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow || e.Key == ConsoleKey.RightArrow)
        {
            NavigateToSibling(1);
            e.Handled = true;
        }
        else
        {
            base.OnKeyDown(e);
        }
    }

    private void NavigateToSibling(int direction)
    {
        if (Parent is not StackPanel panel)
            return;

        var children = panel.Children;
        var count = children.Count;
        if (count == 0)
            return;

        int checkedIndex = -1;
        int thisIndex = -1;

        // Find the index of the checked radio button or this radio button
        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            if (child is RadioButton rb && rb.GroupName == this.GroupName)
            {
                if (rb.IsChecked == true)
                {
                    checkedIndex = i;
                    break;
                }
                if (child == this)
                {
                    thisIndex = i;
                }
            }
        }

        // If no radio button is checked, start from this radio button
        int startIndex = checkedIndex >= 0 ? checkedIndex : thisIndex;

        if (startIndex < 0)
            return;

        // Find the next sibling in the given direction
        int currentIndex = startIndex;
        for (int i = 0; i < count; i++)
        {
            currentIndex += direction;

            if (currentIndex < 0)
                currentIndex = count - 1;
            else if (currentIndex >= count)
                currentIndex = 0;

            if (currentIndex == startIndex)
                break; // Looped around and found nothing else

            var child = children[currentIndex];
            if (child is RadioButton rb && rb.GroupName == this.GroupName)
            {
                rb.Focus();
                if (rb.IsChecked != true)
                {
                    rb.IsChecked = true;
                }
                return;
            }
        }
    }

    private void UpdateGroup()
    {
        if (Parent is StackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child != this && child is RadioButton rb && rb.GroupName == this.GroupName)
                {
                    rb.IsChecked = false;
                }
            }
        }
    }
}
