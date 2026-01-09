using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class TabControl : UIElement
{
    private List<TabItem> _items = new List<TabItem>();
    public List<TabItem> Items => _items;

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

            var bg = (i == SelectedIndex) ? ConsoleColor.Gray : ConsoleColor.Black;
            var fg = (i == SelectedIndex) ? ConsoleColor.Black : ConsoleColor.White;

            for (int k = 0; k < header.Length; k++)
            {
                buffer.SetPixel(headerX + k, y, header[k], fg, bg);
            }
            headerX += header.Length + 1;
        }

        // Draw Content Border line
        for (int i = 0; i < w; i++)
            buffer.SetPixel(x + i, y + 1, '-', ConsoleColor.Gray, ConsoleColor.Black);

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

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

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
                    return;
                }
                currentX += len + 1;
            }
        }
    }
}

public class TabItem : DependencyObject
{
    public UIElement Parent { get; set; }

    public object Header { get; set; }
    public object Content { get; set; }
}
