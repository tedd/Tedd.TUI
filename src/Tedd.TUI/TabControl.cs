using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class TabControl : UIElement
{
    public TabControl()
    {
        Focusable = true;
    }
    private List<TabItem> _items = new List<TabItem>();
    public List<TabItem> Items => _items;

    public override int VisualChildrenCount => (SelectedIndex >= 0 && SelectedIndex < Items.Count && Items[SelectedIndex].Content is UIElement) ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (VisualChildrenCount > 0 && index == 0)
        {
             return (UIElement)Items[SelectedIndex].Content;
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public override UIElement FindName(string name)
    {
        if (Name == name) return this;
        foreach (var item in Items)
        {
            if (item.Content is UIElement uie)
            {
                var found = uie.FindName(name);
                if (found != null) return found;
            }
        }
        return null;
    }

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(TabControl), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get { return (BoxStyle)GetValue(BoxStyleProperty); }
        set { SetValue(BoxStyleProperty, value); }
    }

    private int _selectedIndex = 0;
    public int SelectedIndex
    {
        get { return _selectedIndex; }
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                UpdateContent();
            }
        }
    }

    public void AddItem(TabItem item)
    {
        _items.Add(item);
        item.Parent = this;
        if (item.Content is UIElement uie)
        {
            uie.Parent = this;
            uie.DataContext = this.DataContext; 
        }

        // If it's the first item, select it
        if (_items.Count == 1)
        {
            UpdateContent();
        }
    }

    private void UpdateContent()
    {
        // No-op here, render handles showing correct content?
        // Or we need to manage visibility of content.
        // Actually TabControl should render headers AND the selected content.
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Height = Header Height (1) + Max Content Height
        // Width = Max Content Width or available

        int w = Width > 0 ? Width : availableSize.Width;
        int h = Height > 0 ? Height : availableSize.Height;

        // Measure children
        foreach(var item in _items)
        {
            if (item.Content is UIElement uie)
            {
                uie.Measure(new Size(w, h - 2)); // -2 for header and border
            }
        }

        return new Size(w, h);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        // Headers are drawn by TabControl
        // Content is arranged below header
        foreach(var item in _items)
        {
            if (item.Content is UIElement uie)
            {
                uie.Arrange(new Rect(0, 2, finalSize.Width, finalSize.Height - 2));
            }
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;

        // Draw Headers
        int headerX = x;
        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            string header = $" {item.Header} ";

            ConsoleColor bg, fg;
            if (i == SelectedIndex)
            {
                // Selected tab: use different colors based on focus state
                if (IsFocused)
                {
                    fg = ConsoleColor.Yellow;
                    bg = ConsoleColor.DarkBlue;
                }
                else
                {
                    fg = ConsoleColor.Black;
                    bg = ConsoleColor.Gray;
                }
            }
            else
            {
                // Unselected tabs
                fg = ConsoleColor.White;
                bg = ConsoleColor.Black;
            }

            for (int k = 0; k < header.Length; k++)
            {
                buffer.SetPixel(headerX + k, y, header[k], fg, bg);
            }
            headerX += header.Length + 1;
        }

        // Draw Content Border line (Unicode horizontal)
        char hChar = BoxDrawingChars.Get(BoxStyle).Horizontal;
        for (int i = 0; i < w; i++)
            buffer.SetPixel(x + i, y + 1, hChar, ConsoleColor.Gray, ConsoleColor.Black);

        // Draw Selected Content
        if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
        {
             var content = Items[SelectedIndex].Content as UIElement;
             if (content != null)
             {
                 content.Render(buffer, x, y); // Relative position handled in Arrange, but wait.
                 // Render uses parent's offsetX/Y + RenderSize.X/Y.
                 // The child's RenderSize (from Arrange) is (0, 2).
                 // So we pass offsetX, offsetY, and it adds its own RenderSize.
                 // Correct.
             }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Items.Count == 0) return;

        bool switchTab = false;
        int dir = 0;

        if (e.Key == ConsoleKey.LeftArrow)
        {
            switchTab = true;
            dir = -1;
        }
        else if (e.Key == ConsoleKey.RightArrow)
        {
            switchTab = true;
            dir = 1;
        }

        if (switchTab)
        {
            int next = SelectedIndex + dir;
            if (next < 0) next = Items.Count - 1;
            if (next >= Items.Count) next = 0;
            SelectedIndex = next;
            e.Handled = true;
            // Keep focus on TabControl (WinForms behavior) so user can keep navigating with arrows
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // e.X/Y are local coordinates
        if (e.Y == 0) // Header click
        {
            int currentX = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                var headerStr = Items[i].Header?.ToString() ?? "";
                int len = headerStr.Length + 2; // " Header "
                if (e.X >= currentX && e.X < currentX + len)
                {
                    SelectedIndex = i;
                    e.Handled = true;
                    FocusFirstInSelectedTab();
                    return;
                }
                currentX += len + 1;
            }
        }
    }

    private void FocusFirstInSelectedTab()
    {
        if (GetRoot() is TuiWindow window && SelectedIndex >= 0 && SelectedIndex < Items.Count)
        {
            var content = Items[SelectedIndex].Content as UIElement;
            if (content != null)
                window.FocusFirstIn(content);
        }
    }
}

public class TabItem : DependencyObject
{
    public UIElement Parent { get; set; }

    public object Header { get; set; }
    public object Content { get; set; }
}
