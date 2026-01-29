using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class ListBox : UIElement
{
    public ListBox()
    {
        Focusable = true;
    }
    private List<object> _items = new List<object>();
    public List<object> Items => _items;

    public int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// When true (default), selection is visible even when unfocused.
    /// When false, selection highlighting is only shown while focused.
    /// </summary>
    public bool ShowSelection { get; set; } = true;

    private int _scrollOffset = 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        // Default size
        return new Size(Width > 0 ? Width : 20, Height > 0 ? Height : 10);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        // Draw border? Or assume user puts it in a Border control?
        // Let's draw items.

        for (int i = 0; i < h; i++)
        {
            int itemIndex = i + _scrollOffset;

            // Clear line
            for (int dx = 0; dx < w; dx++)
            {
                var pixelBg = Background ?? buffer.GetPixel(x + dx, y + i).Background;
                buffer.SetPixel(x + dx, y + i, ' ', ConsoleColor.White, pixelBg);
            }

            if (itemIndex < Items.Count)
            {
                bool isSelected = (itemIndex == SelectedIndex);
                var bg = Background ?? buffer.GetPixel(x, y + i).Background;
                var fg = ConsoleColor.Gray;
                if (isSelected)
                {
                    if (IsFocused)
                    {
                        // Focused: selected item is blue
                        bg = ConsoleColor.Blue;
                        fg = ConsoleColor.White;
                    }
                    else if (ShowSelection)
                    {
                        // Not focused but ShowSelection enabled: inverted black/white
                        bg = ConsoleColor.White;
                        fg = ConsoleColor.Black;
                    }
                    // else: ShowSelection is false and not focused, use default colors
                }

                string content = Items[itemIndex]?.ToString() ?? "";
                if (content.Length > w) content = content.Substring(0, w);

                for (int dx = 0; dx < content.Length; dx++)
                {
                    buffer.SetPixel(x + dx, y + i, content[dx], fg, bg);
                }
                // Fill rest of line with bg
                for (int dx = content.Length; dx < w; dx++)
                {
                    buffer.SetPixel(x + dx, y + i, ' ', fg, bg);
                }
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // e.Y is already local relative to this control
        int itemIndex = e.Y + _scrollOffset;

        if (itemIndex >= 0 && itemIndex < Items.Count)
        {
            SelectedIndex = itemIndex;
        }
        e.Handled = true;
    }

    public event EventHandler SelectionChanged;

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.UpArrow)
        {
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            if (SelectedIndex < Items.Count - 1)
            {
                SelectedIndex++;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void EnsureVisible(int index)
    {
        if (index < _scrollOffset)
        {
            _scrollOffset = index;
        }
        else if (index >= _scrollOffset + RenderSize.Height)
        {
            _scrollOffset = index - RenderSize.Height + 1;
        }
    }
}
