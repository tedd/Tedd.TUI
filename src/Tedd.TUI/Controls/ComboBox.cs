using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tedd.TUI.Controls;

public class ComboBox : Selector
{
    private ListBox _popupListBox;
    private ComboBoxPopupBorder? _popupBorder;
    private bool _isDroppedDown = false;
    private bool _arrowFocused = false; // True when focus is on the dropdown arrow

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(TuiColor), typeof(ComboBox), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.Register("HoverForeground", typeof(TuiColor), typeof(ComboBox), TuiColor.Cyan);

    /// <summary>Text foreground used while the mouse hovers the control and it is not focused.</summary>
    public TuiColor HoverForeground
    {
        get => (TuiColor)GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedTextBackgroundColorProperty =
        DependencyProperty.Register("FocusedTextBackgroundColor", typeof(TuiColor), typeof(ComboBox), TuiColor.DarkGray);

    public TuiColor FocusedTextBackgroundColor
    {
        get => (TuiColor)GetValue(FocusedTextBackgroundColorProperty);
        set => SetValue(FocusedTextBackgroundColorProperty, value);
    }

    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register("ArrowColor", typeof(TuiColor), typeof(ComboBox), TuiColor.Black);

    public TuiColor ArrowColor
    {
        get => (TuiColor)GetValue(ArrowColorProperty);
        set => SetValue(ArrowColorProperty, value);
    }

    public static readonly DependencyProperty ArrowBackgroundColorProperty =
        DependencyProperty.Register("ArrowBackgroundColor", typeof(TuiColor), typeof(ComboBox), TuiColor.Gray);

    public TuiColor ArrowBackgroundColor
    {
        get => (TuiColor)GetValue(ArrowBackgroundColorProperty);
        set => SetValue(ArrowBackgroundColorProperty, value);
    }

    public static readonly DependencyProperty FocusedArrowColorProperty =
        DependencyProperty.Register("FocusedArrowColor", typeof(TuiColor), typeof(ComboBox), TuiColor.Yellow);

    public TuiColor FocusedArrowColor
    {
        get => (TuiColor)GetValue(FocusedArrowColorProperty);
        set => SetValue(FocusedArrowColorProperty, value);
    }

    public static readonly DependencyProperty FocusedArrowBackgroundColorProperty =
        DependencyProperty.Register("FocusedArrowBackgroundColor", typeof(TuiColor), typeof(ComboBox), TuiColor.DarkGray);

    public TuiColor FocusedArrowBackgroundColor
    {
        get => (TuiColor)GetValue(FocusedArrowBackgroundColorProperty);
        set => SetValue(FocusedArrowBackgroundColorProperty, value);
    }

    public static readonly DependencyProperty PopupBackgroundProperty =
        DependencyProperty.Register("PopupBackground", typeof(TuiColor), typeof(ComboBox), TuiColor.Black);

    public TuiColor PopupBackground
    {
        get => (TuiColor)GetValue(PopupBackgroundProperty);
        set => SetValue(PopupBackgroundProperty, value);
    }

    public static readonly DependencyProperty PopupBorderColorProperty =
        DependencyProperty.Register("PopupBorderColor", typeof(TuiColor), typeof(ComboBox), TuiColor.White);

    public TuiColor PopupBorderColor
    {
        get => (TuiColor)GetValue(PopupBorderColorProperty);
        set => SetValue(PopupBorderColorProperty, value);
    }

    public ComboBox()
    {
        Focusable = true;
        _popupListBox = new ListBox();
    }

    /// <summary>
    /// A ComboBox shows one item in its collapsed state, so — as in WPF and MAUI — it is
    /// always single-selection. The <see cref="Selector.SelectionMode"/> it inherits is
    /// coerced back to <see cref="SelectionMode.Single"/> rather than half-working:
    /// the dropdown closes on the first pick, so a range could never be built.
    /// </summary>
    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        if (dp == SelectionModeProperty && SelectionMode != SelectionMode.Single)
        {
            SetValue(SelectionModeProperty, SelectionMode.Single);
            return;
        }

        base.OnPropertyChanged(dp);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Width > 0 ? Width : 15, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;

        // Draw text area
        var textBg = IsFocused && !_arrowFocused ? FocusedTextBackgroundColor : (Background ?? TuiColor.Black);
        var textFg = IsFocused && !_arrowFocused ? FocusedForeground
            : !IsFocused && IsMouseOver ? HoverForeground
            : Foreground;

        string text = GetItemText(SelectedItem);
        if (text.Length > w - 2) text = text.Substring(0, w - 2);

        // Draw content
        for (int i = 0; i < w - 1; i++)
        {
            char c = (i < text.Length) ? text[i] : ' ';
            buffer.SetPixel(x + i, y, c, textFg, textBg);
        }

        // Draw Arrow with focus indication
        var arrowBg = IsFocused && _arrowFocused ? FocusedArrowBackgroundColor : ArrowBackgroundColor;
        var arrowFg = IsFocused && _arrowFocused ? FocusedArrowColor : ArrowColor;
        buffer.SetPixel(x + w - 1, y, 'v', arrowFg, arrowBg);
    }

    public override void OnGotFocus()
    {
        base.OnGotFocus();
        // Start with text area focused
        _arrowFocused = false;
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        // Reset arrow focus state when losing focus
        _arrowFocused = false;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        ToggleDropdown();
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Handle Tab for internal focus navigation (text area <-> arrow)
        if (e.Key == ConsoleKey.Tab)
        {
            if (!e.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                // Forward Tab: if not on arrow, move to arrow; if on arrow, let it pass through
                if (!_arrowFocused)
                {
                    _arrowFocused = true;
                    e.Handled = true;
                    return;
                }
                // else: _arrowFocused is true, don't handle - let focus move to next control
            }
            else
            {
                // Shift+Tab: if on arrow, move to text area; if on text area, let it pass through
                if (_arrowFocused)
                {
                    _arrowFocused = false;
                    e.Handled = true;
                    return;
                }
                // else: on text area, don't handle - let focus move to previous control
            }
        }

        // Space, Enter, or Arrow keys open dropdown
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter
            || e.Key == ConsoleKey.DownArrow || e.Key == ConsoleKey.UpArrow)
        {
            ToggleDropdown();
            e.Handled = true;
        }
    }

    private void ToggleDropdown()
    {
        var root = GetRoot() as TuiWindow;
        if (root == null) return;

        if (_isDroppedDown)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown(root);
        }
    }

    private void OpenDropdown(TuiWindow root)
    {
        _isDroppedDown = true;

        // Calculate position relative to Window
        int absX = RenderSize.X;
        int absY = RenderSize.Y + RenderSize.Height;

        var current = Parent;
        while (current != null && current != root)
        {
            absX += current.RenderSize.X;
            absY += current.RenderSize.Y;
            // Compensate for any ancestor ScrollViewer scroll offset, otherwise the popup
            // is placed at the un-scrolled (often offscreen) position of the ComboBox.
            if (current is ScrollViewer sv)
            {
                absX -= sv.HorizontalOffset;
                absY -= sv.VerticalOffset;
            }
            current = current.Parent;
        }

        // Calculate available height below
        int spaceBelow = Math.Max(0, root.RenderSize.Height - absY);
        // We need 2 for border
        int maxContentHeight = Math.Max(0, spaceBelow - 2);

        // Setup ListBox
        _popupListBox.ItemsSource = this.Items;
        _popupListBox.DisplayMemberPath = this.DisplayMemberPath;
        _popupListBox.ItemTemplate = this.ItemTemplate;
        _popupListBox.SelectedIndex = this.SelectedIndex;

        // Popup width matches ComboBox width, adjusted for border
        int contentWidth = Math.Max(0, RenderSize.Width - 2);
        _popupListBox.Width = contentWidth;

        // Dynamic height based on items, clamped to available space
        int desiredHeight = Items.Count;
        if (desiredHeight == 0) desiredHeight = 1;
        _popupListBox.Height = Math.Min(desiredHeight, maxContentHeight);

        // Ensure ListBox is opaque
        _popupListBox.Background = PopupBackground;

        // Create a Border for the popup
        _popupBorder = new ComboBoxPopupBorder
        {
            Width = RenderSize.Width,
            Height = _popupListBox.Height + 2,
            Child = _popupListBox,
            BorderColor = PopupBorderColor,
            BoxStyle = BoxStyle.Single,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Owner = this
        };

        // Measure and arrange the popup (border)
        _popupBorder.Measure(new Size(_popupBorder.Width, _popupBorder.Height));
        _popupBorder.Arrange(new Rect(absX, absY, _popupBorder.Width, _popupBorder.Height));

        // Unsubscribe to avoid duplicates if any
        _popupListBox.SelectionChanged -= Popup_SelectionChanged;
        _popupListBox.SelectionChanged += Popup_SelectionChanged;

        root.PushOverlay(_popupBorder);
        root.SetFocus(_popupListBox);
    }

    private void Popup_SelectionChanged(object? sender, EventArgs e)
    {
        CloseDropdown();
    }

    public void CloseDropdown(bool restoreFocus = true)
    {
        var root = GetRoot() as TuiWindow;
        if (root != null)
        {
            if (_popupBorder != null)
            {
                root.RemoveOverlay(_popupBorder);
                _popupBorder = null;
            }
            if (restoreFocus)
            {
                root.SetFocus(this);
            }

            // Sync selection back
            if (_popupListBox.SelectedIndex >= 0 && _popupListBox.SelectedIndex < Items.Count)
            {
                SelectedIndex = _popupListBox.SelectedIndex;
            }
        }
        _isDroppedDown = false;
    }

    internal class ComboBoxPopupBorder : Border
    {
        public required ComboBox Owner { get; set; }

        public ComboBoxPopupBorder()
        {
            // Dropdown lists are tight chrome: items sit directly inside the border line.
            Padding = new Thickness(0);
        }
    }
}
