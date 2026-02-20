using System;

namespace Tedd.TUI;

public class RadioButton : UIElement
{
    public RadioButton()
    {
        Focusable = true;
    }
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register("IsChecked", typeof(bool), typeof(RadioButton), false);

    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(string), typeof(RadioButton), string.Empty);

    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register("GroupName", typeof(string), typeof(RadioButton), string.Empty);

    public string GroupName
    {
        get { return (string)GetValue(GroupNameProperty); }
        set { SetValue(GroupNameProperty, value); }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Content;
        // (o) Text
        return new Size(4 + text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var fg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;
        var bg = ConsoleColor.Black;

        buffer.SetPixel(x, y, '(', ConsoleColor.Gray, bg);
        buffer.SetPixel(x + 1, y, IsChecked ? 'o' : ' ', ConsoleColor.Green, bg);
        buffer.SetPixel(x + 2, y, ')', ConsoleColor.Gray, bg);

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
        if (!IsChecked)
        {
            SetChecked();
        }
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            if (!IsChecked)
            {
                SetChecked();
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.UpArrow || e.Key == ConsoleKey.LeftArrow)
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
                if (rb.IsChecked)
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
                if (!rb.IsChecked)
                {
                    rb.SetChecked();
                }
                return;
            }
        }
    }

    private void SetChecked()
    {
        IsChecked = true;
        UpdateGroup();
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
