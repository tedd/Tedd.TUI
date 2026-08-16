using System;
using System.Reflection;

namespace Tedd.TUI.Controls;

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

        // No local Foreground: the predefined themes style ListBox.Foreground (Gray in
        // the default Dark theme), and a local value here would block them.
    }

    private void OnScroll(object? sender, EventArgs e)
    {
        _scrollOffset = _scrollBar.Value;
        Invalidate();
    }

    public override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Handled) return;

        // Delegate to the internal scrollbar: shares its notch accumulator, clamping
        // and ValueChanged -> _scrollOffset sync. When there is nothing to scroll the
        // bar leaves the event unhandled so an outer scroll viewer can take it.
        if (_scrollBar.Visibility)
            _scrollBar.OnMouseWheel(e);
    }

    /// <summary>
    /// When true (default), selection is visible even when unfocused.
    /// When false, selection highlighting is only shown while focused.
    /// </summary>
    public bool ShowSelection { get; set; } = true;

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    public static readonly DependencyProperty SelectionForegroundProperty =
        DependencyProperty.Register("SelectionForeground", typeof(TuiColor), typeof(ListBox), TuiColor.Black);

    public TuiColor SelectionForeground
    {
        get => (TuiColor)GetValue(SelectionForegroundProperty);
        set => SetValue(SelectionForegroundProperty, value);
    }

    public static readonly DependencyProperty SelectionBackgroundProperty =
        DependencyProperty.Register("SelectionBackground", typeof(TuiColor), typeof(ListBox), TuiColor.White);

    public TuiColor SelectionBackground
    {
        get => (TuiColor)GetValue(SelectionBackgroundProperty);
        set => SetValue(SelectionBackgroundProperty, value);
    }

    public static readonly DependencyProperty FocusedSelectionForegroundProperty =
        DependencyProperty.Register("FocusedSelectionForeground", typeof(TuiColor), typeof(ListBox), TuiColor.White);

    public TuiColor FocusedSelectionForeground
    {
        get => (TuiColor)GetValue(FocusedSelectionForegroundProperty);
        set => SetValue(FocusedSelectionForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedSelectionBackgroundProperty =
        DependencyProperty.Register("FocusedSelectionBackground", typeof(TuiColor), typeof(ListBox), TuiColor.Blue);

    public TuiColor FocusedSelectionBackground
    {
        get => (TuiColor)GetValue(FocusedSelectionBackgroundProperty);
        set => SetValue(FocusedSelectionBackgroundProperty, value);
    }

    private int _scrollOffset = 0;

    public override int VisualChildrenCount => 1;
    public override UIElement GetVisualChild(int index)
    {
        if (index == 0) return _scrollBar;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Auto height clamps to the offered rows and the overflow goes to the list's own
    /// scrollbar, so a list is a vertical viewport. It never scrolls horizontally.
    /// </remarks>
    public override bool ScrollsOwnContent(Orientation orientation) =>
        orientation == Orientation.Vertical;

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
            _scrollBar.SetLayoutMetrics(0, Math.Max(0, Items.Count - h), h);
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
                buffer.SetPixel(x + dx, y + i, ' ', Foreground, pixelBg);
            }

            if (itemIndex < Items.Count)
            {
                // Every selected row highlights, not just the primary one, so a
                // multi-selection is visible as a block.
                bool isSelected = IsIndexSelected(itemIndex);
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

                if (ItemTemplate != null)
                {
                    // Fill row with selection background first, then render template content on top
                    for (int dx = 0; dx < effectiveW; dx++)
                    {
                        buffer.SetPixel(x + dx, y + i, ' ', fg, bg);
                    }
                    var container = GetContainerForItemCore();
                    PrepareContainerForItemOverride(container, Items[itemIndex]);
                    container.Measure(new Size(effectiveW, 1));
                    container.Arrange(new Rect(0, 0, effectiveW, 1));
                    container.Render(buffer, x, y + i);
                }
                else
                {
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
        }

        if (_scrollBar.Visibility)
        {
            _scrollBar.Render(buffer, x, y);
        }
    }

    // Bug: Clicking nested focusable child inside ListBox item causes focus to be stolen by ListBox.
    // Root cause: ListBox.OnMouseDown unconditionally calls Focus() on bubbling mouse down.
    // Fix: Return early if the mouse down event has already been handled.
    // Regression: Covered by FocusOverlayTests & general focus routing
    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Handled) return;
        base.OnMouseDown(e);
        Focus();

        // Check if ScrollBar hit
        if (_scrollBar.Visibility && e.X >= RenderSize.Width - 1)
        {
            // Pass to ScrollBar.
            // We need to pass local coordinates to ScrollBar.
            // ScrollBar is at (Width-1, 0).
            // So localX = e.X - (Width-1) = 0 usually.

            // Global coordinates must be carried over: a thumb press anchors its drag
            // in global space, and captured moves arrive with real global coordinates.
            var sbArgs = new MouseEventArgs
            {
                X = e.X - (RenderSize.Width - 1),
                Y = e.Y,
                GlobalX = e.GlobalX,
                GlobalY = e.GlobalY,
                GlobalXF = e.GlobalXF,
                GlobalYF = e.GlobalYF,
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
            // Plain click selects, Shift+click extends the range from the last clicked
            // item, Control+click toggles one — the standard list gestures. In
            // SelectionMode.Single the modifiers are ignored and this is a plain select.
            ApplySelectionGesture(itemIndex, e.Modifiers);
        }
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        bool shift = (e.Modifiers & ConsoleModifiers.Shift) != 0;
        bool ctrl = (e.Modifiers & ConsoleModifiers.Control) != 0;
        bool multiSelect = SelectionMode != SelectionMode.Single;

        if (e.Key == ConsoleKey.UpArrow)
        {
            MoveCurrent(-1, extend: shift);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            MoveCurrent(1, extend: shift);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.PageUp)
        {
            MoveCurrent(-PageStep, extend: shift, clampToEdge: true);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.PageDown)
        {
            MoveCurrent(PageStep, extend: shift, clampToEdge: true);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Home)
        {
            MoveTo(0, extend: shift);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.End)
        {
            MoveTo(Items.Count - 1, extend: shift);
            e.Handled = true;
        }
        else if (ctrl && e.Key == ConsoleKey.A && multiSelect)
        {
            SelectAll();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Spacebar && multiSelect)
        {
            // Space is the keyboard equivalent of a Control+click, acting on the row the
            // cursor is on — which survives that row being deselected.
            ToggleSelection(CurrentIndex);
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            OnSelectionChanged();
            e.Handled = true;
        }
    }

    /// <summary>One page of movement: a full viewport, less a row of overlap for orientation.</summary>
    private int PageStep => Math.Max(1, RenderSize.Height - 1);

    /// <summary>
    /// Moves the current item by <paramref name="delta"/> rows. With <paramref name="extend"/>
    /// (Shift held) the selection grows from the anchor instead of being replaced. A step that
    /// runs off the end stops at the edge when <paramref name="clampToEdge"/> is set (paging)
    /// and is ignored otherwise (single-row steps, which would otherwise wrap the intent).
    /// </summary>
    private void MoveCurrent(int delta, bool extend, bool clampToEdge = false)
    {
        int next = CurrentIndex + delta;
        if (next < 0 || next >= Items.Count)
        {
            if (!clampToEdge) return;
            next = Math.Clamp(next, 0, Items.Count - 1);
        }

        MoveTo(next, extend);
    }

    /// <summary>Moves the current item to <paramref name="index"/>, extending the range if asked.</summary>
    private void MoveTo(int index, bool extend)
    {
        if (index < 0 || index >= Items.Count) return;

        if (extend && SelectionMode != SelectionMode.Single)
            ExtendSelectionTo(index);
        else
            SelectSingle(index);

        EnsureVisible(index);
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
