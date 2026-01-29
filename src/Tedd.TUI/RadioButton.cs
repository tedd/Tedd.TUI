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
        var siblings = GetGroupSiblings();
        if (siblings.Count <= 1)
            return;

        // Find the currently selected radio button in the group
        int currentIndex = -1;
        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].IsChecked)
            {
                currentIndex = i;
                break;
            }
        }

        // If nothing is selected, start from the focused one (this)
        if (currentIndex < 0)
            currentIndex = siblings.IndexOf(this);

        int newIndex = currentIndex + direction;

        // Wrap around
        if (newIndex < 0)
            newIndex = siblings.Count - 1;
        else if (newIndex >= siblings.Count)
            newIndex = 0;

        var target = siblings[newIndex];
        target.Focus();
        if (!target.IsChecked)
        {
            target.SetChecked();
        }
    }

    private List<RadioButton> GetGroupSiblings()
    {
        var result = new List<RadioButton>();
        if (Parent is StackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is RadioButton rb && rb.GroupName == this.GroupName)
                {
                    result.Add(rb);
                }
            }
        }
        return result;
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
