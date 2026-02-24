using System;
using System.Reflection;

namespace Tedd.TUI;

public class ListBox : Selector
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

    /// <summary>
    /// When true (default), selection is visible even when unfocused.
    /// When false, selection highlighting is only shown while focused.
    /// </summary>
    public bool ShowSelection { get; set; } = true;

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register("Foreground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.Gray);

    public ConsoleColor Foreground
    {
        get { return (ConsoleColor)GetValue(ForegroundProperty); }
        set { SetValue(ForegroundProperty, value); }
    }

    public static readonly DependencyProperty SelectionForegroundProperty =
        DependencyProperty.Register("SelectionForeground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.Black);

    public ConsoleColor SelectionForeground
    {
        get { return (ConsoleColor)GetValue(SelectionForegroundProperty); }
        set { SetValue(SelectionForegroundProperty, value); }
    }

    public static readonly DependencyProperty SelectionBackgroundProperty =
        DependencyProperty.Register("SelectionBackground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.White);

    public ConsoleColor SelectionBackground
    {
        get { return (ConsoleColor)GetValue(SelectionBackgroundProperty); }
        set { SetValue(SelectionBackgroundProperty, value); }
    }

    public static readonly DependencyProperty FocusedSelectionForegroundProperty =
        DependencyProperty.Register("FocusedSelectionForeground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.White);

    public ConsoleColor FocusedSelectionForeground
    {
        get { return (ConsoleColor)GetValue(FocusedSelectionForegroundProperty); }
        set { SetValue(FocusedSelectionForegroundProperty, value); }
    }

    public static readonly DependencyProperty FocusedSelectionBackgroundProperty =
        DependencyProperty.Register("FocusedSelectionBackground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.Blue);

    public ConsoleColor FocusedSelectionBackground
    {
        get { return (ConsoleColor)GetValue(FocusedSelectionBackgroundProperty); }
        set { SetValue(FocusedSelectionBackgroundProperty, value); }
    }

    private int _scrollOffset = 0;

    public override int VisualChildrenCount => 1;
    public override UIElement GetVisualChild(int index)
    {
        if (index == 0) return _scrollBar;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 1. Calculate Height
        int h;
        if (Height >= 0)
        {
            h = Height;
        }
        else
        {
            // Auto Height
            h = Items.Count;
            // Constrain to available space
            if (h > availableSize.Height) h = availableSize.Height;
        }

        // 2. Determine if ScrollBar is needed
        bool showScroll = Items.Count > h;

        // 3. Configure ScrollBar
        if (showScroll)
        {
            _scrollBar.Measure(new Size(1, h));
            _scrollBar.Maximum = Math.Max(0, Items.Count - h);
            _scrollBar.ViewportSize = h;
            _scrollBar.Value = _scrollOffset;
            _scrollBar.Visibility = true;
        }
        else
        {
            _scrollBar.Visibility = false;
        }

        // 4. Calculate Width
        int w;
        if (Width >= 0)
        {
            w = Width;
        }
        else
        {
            // Auto Width
            int maxLen = 0;
            foreach (var item in Items)
            {
                var s = GetItemText(item);
                if (!string.IsNullOrEmpty(s))
                {
                    if (s.Length > maxLen) maxLen = s.Length;
                }
            }
            w = maxLen;
            if (showScroll) w++;

            // Constrain
            if (w > availableSize.Width) w = availableSize.Width;
        }

        return new Size(w, h);
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
                var fg = Foreground;
                if (isSelected)
                {
                    if (IsFocused)
                    {
                        // Focused: selected item is blue
                        bg = FocusedSelectionBackground;
                        fg = FocusedSelectionForeground;
                    }
                    else if (ShowSelection)
                    {
                        // Not focused but ShowSelection enabled: inverted black/white
                        bg = SelectionBackground;
                        fg = SelectionForeground;
                    }
                    // else: ShowSelection is false and not focused, use default colors
                }

                string content = GetItemText(Items[itemIndex]);
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
            // SelectionChanged is raised by base.SelectedIndex setter
        }
        e.Handled = true;
    }

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
            OnSelectionChanged();
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
