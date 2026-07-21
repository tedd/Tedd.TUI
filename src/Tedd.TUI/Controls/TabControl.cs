using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI.Controls;

public class TabControl : Selector
{
    public TabControl()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(TabControl), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    public static readonly DependencyProperty FocusedTabForegroundProperty =
        DependencyProperty.Register("FocusedTabForeground", typeof(TuiColor), typeof(TabControl), TuiColor.Yellow);

    /// <summary>Header text color of the selected tab while the control has focus.</summary>
    public TuiColor FocusedTabForeground
    {
        get => (TuiColor)GetValue(FocusedTabForegroundProperty);
        set => SetValue(FocusedTabForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedTabBackgroundProperty =
        DependencyProperty.Register("FocusedTabBackground", typeof(TuiColor), typeof(TabControl), TuiColor.DarkBlue);

    /// <summary>Header background of the selected tab while the control has focus.</summary>
    public TuiColor FocusedTabBackground
    {
        get => (TuiColor)GetValue(FocusedTabBackgroundProperty);
        set => SetValue(FocusedTabBackgroundProperty, value);
    }

    public static readonly DependencyProperty SelectedTabForegroundProperty =
        DependencyProperty.Register("SelectedTabForeground", typeof(TuiColor), typeof(TabControl), TuiColor.Black);

    /// <summary>Header text color of the selected tab while the control is unfocused.</summary>
    public TuiColor SelectedTabForeground
    {
        get => (TuiColor)GetValue(SelectedTabForegroundProperty);
        set => SetValue(SelectedTabForegroundProperty, value);
    }

    public static readonly DependencyProperty SelectedTabBackgroundProperty =
        DependencyProperty.Register("SelectedTabBackground", typeof(TuiColor), typeof(TabControl), TuiColor.Gray);

    /// <summary>Header background of the selected tab while the control is unfocused.</summary>
    public TuiColor SelectedTabBackground
    {
        get => (TuiColor)GetValue(SelectedTabBackgroundProperty);
        set => SetValue(SelectedTabBackgroundProperty, value);
    }

    public static readonly DependencyProperty TabForegroundProperty =
        DependencyProperty.Register("TabForeground", typeof(TuiColor), typeof(TabControl), TuiColor.White);

    /// <summary>Header text color of unselected tabs.</summary>
    public TuiColor TabForeground
    {
        get => (TuiColor)GetValue(TabForegroundProperty);
        set => SetValue(TabForegroundProperty, value);
    }

    public static readonly DependencyProperty TabBackgroundProperty =
        DependencyProperty.Register("TabBackground", typeof(TuiColor?), typeof(TabControl), null);

    /// <summary>Header background of unselected tabs; null adopts the underlying surface.</summary>
    public TuiColor? TabBackground
    {
        get => (TuiColor?)GetValue(TabBackgroundProperty);
        set => SetValue(TabBackgroundProperty, value);
    }

    public static readonly DependencyProperty StripLineForegroundProperty =
        DependencyProperty.Register("StripLineForeground", typeof(TuiColor), typeof(TabControl), TuiColor.Gray);

    /// <summary>Color of the horizontal line under the tab headers.</summary>
    public TuiColor StripLineForeground
    {
        get => (TuiColor)GetValue(StripLineForegroundProperty);
        set => SetValue(StripLineForegroundProperty, value);
    }

    public static readonly DependencyProperty StripLineBackgroundProperty =
        DependencyProperty.Register("StripLineBackground", typeof(TuiColor?), typeof(TabControl), null);

    /// <summary>Background behind the strip line; null adopts the underlying surface.</summary>
    public TuiColor? StripLineBackground
    {
        get => (TuiColor?)GetValue(StripLineBackgroundProperty);
        set => SetValue(StripLineBackgroundProperty, value);
    }

    public override int VisualChildrenCount => (SelectedIndex >= 0 && SelectedIndex < Items.Count) ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (VisualChildrenCount > 0 && index == 0)
        {
            var selected = SelectedItem;
            if (selected is TabItem ti && ti.Content is UIElement uie) return uie;
            if (selected is UIElement element) return element; // Direct UIElement content
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public override UIElement FindName(string name)
    {
        if (Name == name) return this;
        foreach (var item in Items)
        {
            if (item is TabItem ti)
            {
                // Search inside TabItem content
                if (ti.Content is UIElement uie)
                {
                    var found = uie.FindName(name);
                    if (found != null) return found;
                }
                // Also TabItem itself
                if (ti.Name == name) return ti;
            }
            else if (item is UIElement uie)
            {
                var found = uie.FindName(name);
                if (found != null) return found;
            }
        }
        return null;
    }

    // Handle Parent/DataContext propagation for Items
    protected override void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsCollectionChanged(sender, e);

        // When items are added, set their Parent to this TabControl to ensure DataContext flows.
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is UIElement uie)
                {
                    uie.Parent = this;
                }
            }
        }

        // If removed, clear parent?
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is UIElement uie && uie.Parent == this)
                {
                    uie.Parent = null;
                }
            }
        }

        // Ensure selection is valid
        if (SelectedIndex == -1 && Items.Count > 0)
        {
            SelectedIndex = 0;
        }
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);

        // Update IsSelected on TabItems
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] is TabItem ti)
            {
                ti.IsSelected = (i == SelectedIndex);
            }
        }

        // Parent management for Content
        // We need to ensure the Content of the selected TabItem has the correct parent chain for DataContext
        if (SelectedItem is TabItem selectedTab && selectedTab.Content is UIElement content)
        {
            // Set Content's parent to TabItem (logical)
            // But wait, if we didn't use Template, Content.Parent might be null.
            // We force it here to ensure DataContext flows: TabControl -> TabItem -> Content
            if (content.Parent != selectedTab)
            {
                content.Parent = selectedTab;
            }
            // Ensure TabItem parent is this
            if (selectedTab.Parent != this)
            {
                selectedTab.Parent = this;
            }
        }

        Invalidate();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int w = Width > 0 ? Width : availableSize.Width;
        int h = Height > 0 ? Height : availableSize.Height;

        // Header Height = 1 (Text) + 1 (Border line) = 2
        int headerHeight = 2;

        // Measure Content
        if (SelectedItem is TabItem ti && ti.Content is UIElement uie)
        {
            uie.Measure(new Size(w, Math.Max(0, h - headerHeight)));
        }
        else if (SelectedItem is UIElement content)
        {
            content.Measure(new Size(w, Math.Max(0, h - headerHeight)));
        }

        return new Size(w, h);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int headerHeight = 2;

        if (SelectedItem is TabItem ti && ti.Content is UIElement uie)
        {
            uie.Arrange(new Rect(0, headerHeight, finalSize.Width, Math.Max(0, finalSize.Height - headerHeight)));
        }
        else if (SelectedItem is UIElement content)
        {
            content.Arrange(new Rect(0, headerHeight, finalSize.Width, Math.Max(0, finalSize.Height - headerHeight)));
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
            string headerText = "";
            if (item is TabItem ti)
            {
                headerText = ti.Header?.ToString() ?? "";
            }
            else
            {
                headerText = GetItemText(item);
            }

            string header = $" {headerText} ";

            TuiColor bg, fg;
            if (i == SelectedIndex)
            {
                if (IsFocused)
                {
                    fg = FocusedTabForeground;
                    bg = FocusedTabBackground;
                }
                else
                {
                    fg = SelectedTabForeground;
                    bg = SelectedTabBackground;
                }
            }
            else
            {
                fg = TabForeground;
                bg = TabBackground ?? buffer.GetPixel(headerX, y).Background;
            }

            for (int k = 0; k < header.Length; k++)
            {
                buffer.SetPixel(headerX + k, y, header[k], fg, bg);
            }
            headerX += header.Length + 1;
        }

        // Draw Content Border line
        char hChar = BoxDrawingChars.Get(BoxStyle).Horizontal;
        var stripFg = StripLineForeground;
        var stripBg = StripLineBackground;
        for (int i = 0; i < w; i++)
            buffer.SetPixel(x + i, y + 1, hChar, stripFg, stripBg ?? buffer.GetPixel(x + i, y + 1).Background);

        // Draw Selected Content
        if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
        {
            UIElement? content = null;
            if (Items[SelectedIndex] is TabItem ti) content = ti.Content as UIElement;
            else content = Items[SelectedIndex] as UIElement;

            if (content != null)
            {
                content.Render(buffer, x, y);
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Don't switch our tabs when a nested control (e.g. an inner TabControl,
        // ListBox, etc.) has already consumed the arrow key during the bubble phase.
        if (e.Handled) return;

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
        }
    }

    // Bug: Clicking TextBox or TextEditor inside TabControl doesn't focus or loses focus.
    // Root cause: TabControl.OnMouseDown unconditionally calls Focus() and steals focus back on bubbling mouse down.
    // Fix: Return early if the mouse down event has already been handled.
    // Regression: TabControlTests.TabControl_ClickChild_KeepsChildFocus
    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Handled) return;
        base.OnMouseDown(e);
        Focus();

        // Check Header Click
        if (e.Y == 0)
        {
            int currentX = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                string headerText = "";
                if (Items[i] is TabItem ti) headerText = ti.Header?.ToString() ?? "";
                else headerText = GetItemText(Items[i]);

                int len = headerText.Length + 2; // " Header "
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
