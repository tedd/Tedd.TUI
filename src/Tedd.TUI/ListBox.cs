using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class ListBox : UIElement
{
    private readonly ScrollBar _scrollBar;

    public ListBox()
    {
        Focusable = true;
        _scrollBar = new ScrollBar()
        {
            Orientation = Orientation.Vertical,
            Width = 1
        };
        _scrollBar.Parent = this;
        _scrollBar.ValueChanged += OnScroll;
    }

    private void OnScroll(object? sender, EventArgs e)
    {
        _scrollOffset = _scrollBar.Value;
        Invalidate();
    }

    private List<object> _items = new List<object>();
    public List<object> Items => _items;

    private int _selectedIndex = -1;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// When true (default), selection is visible even when unfocused.
    /// When false, selection highlighting is only shown while focused.
    /// </summary>
    public bool ShowSelection { get; set; } = true;

    private int _scrollOffset = 0;

    protected override int VisualChildrenCount => 1;
    protected override UIElement GetVisualChild(int index)
    {
        if (index == 0) return _scrollBar;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Default size
        // We'll reserve 1 column for ScrollBar always? Or only if needed?
        // Let's autoshow scrollbar if items > height.
        // But Measure happens before we know items vs height fully if auto height?
        // Assume fixed height or use available height.

        int h = Height > 0 ? Height : availableSize.Height;
        bool showScroll = Items.Count > h;

        if (showScroll)
        {
            _scrollBar.Measure(new Size(1, h));
            _scrollBar.Maximum = Math.Max(0, Items.Count - h);
            _scrollBar.ViewportSize = h;
            _scrollBar.Value = _scrollOffset; // update in case it changed
            _scrollBar.Value = _scrollOffset; // update in case it changed
            // _scrollBar.Opacity = 1f; // removed, property does not exist
            _scrollBar.Visibility = true;
        }
        else
        {
            _scrollBar.Visibility = false;
        }

        return new Size(Width > 0 ? Width : 20, h > 0 ? h : 10);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_scrollBar.Visibility)
        {
            _scrollBar.Arrange(new Rect(finalSize.Width - 1, 0, 1, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        // Draw items.
        // If ScrollBar visible, effective width is w - 1
        int effectiveW = _scrollBar.Visibility ? w - 1 : w;

        // Ensure scroll offset is valid
        if (_scrollOffset > Items.Count - h) _scrollOffset = Math.Max(0, Items.Count - h);
        _scrollBar.Value = _scrollOffset; // Sync if clamped

        for (int i = 0; i < h; i++)
        {
            int itemIndex = i + _scrollOffset;

            // Clear line
            for (int dx = 0; dx < effectiveW; dx++)
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
                if (content.Length > effectiveW) content = content.Substring(0, effectiveW);

                for (int dx = 0; dx < content.Length; dx++)
                {
                    buffer.SetPixel(x + dx, y + i, content[dx], fg, bg);
                }
                // Fill rest of line with bg
                for (int dx = content.Length; dx < effectiveW; dx++)
                {
                    buffer.SetPixel(x + dx, y + i, ' ', fg, bg);
                }
            }
        }

        if (_scrollBar.Visibility)
        {
            _scrollBar.Render(buffer, x, y);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // Check if ScrollBar hit
        if (_scrollBar.Visibility && e.X >= RenderSize.Width - 1)
        {
            // Pass to ScrollBar. 
            // We need to pass local coordinates to ScrollBar.
            // ScrollBar is at (Width-1, 0).
            // So localX = e.X - (Width-1) = 0 usually.
            
            var sbArgs = new MouseEventArgs
            {
                X = e.X - (RenderSize.Width - 1),
                Y = e.Y,
                Handled = false
            };
            _scrollBar.OnMouseDown(sbArgs);
            e.Handled = true;
            return;
        }

        // e.Y is already local relative to this control
        int itemIndex = e.Y + _scrollOffset;

        if (itemIndex >= 0 && itemIndex < Items.Count)
        {
            SelectedIndex = itemIndex;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
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
        _scrollBar.Value = _scrollOffset;
        Invalidate();
    }
}
