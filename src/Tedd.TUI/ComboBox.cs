using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class ComboBox : UIElement
{
    private ListBox _popupListBox;
    private Border? _popupBorder;
    private bool _isDroppedDown = false;
    private object _selectedItem;
    private bool _arrowFocused = false; // True when focus is on the dropdown arrow

    public List<object> Items { get; } = new List<object>();

    public event EventHandler? SelectionChanged;

    public object SelectedItem
    {
        get { return _selectedItem; }
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                if (_popupListBox != null)
                {
                    _popupListBox.SelectedIndex = Items.IndexOf(value);
                }
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ComboBox()
    {
        Focusable = true;
        _popupListBox = new ListBox();
        _popupListBox.Items.AddRange(Items); // Sync logic needed
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
        var textBg = IsFocused && !_arrowFocused ? ConsoleColor.DarkGray : ConsoleColor.Black;
        var textFg = IsFocused && !_arrowFocused ? ConsoleColor.Yellow : ConsoleColor.White;

        string text = SelectedItem?.ToString() ?? "";
        if (text.Length > w - 2) text = text.Substring(0, w - 2);

        // Draw content
        for (int i = 0; i < w - 1; i++)
        {
            char c = (i < text.Length) ? text[i] : ' ';
            buffer.SetPixel(x + i, y, c, textFg, textBg);
        }

        // Draw Arrow with focus indication
        var arrowBg = IsFocused && _arrowFocused ? ConsoleColor.DarkGray : ConsoleColor.Gray;
        var arrowFg = IsFocused && _arrowFocused ? ConsoleColor.Yellow : ConsoleColor.Black;
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
            current = current.Parent;
        }

        // Calculate available height below
        int spaceBelow = Math.Max(0, root.RenderSize.Height - absY);
        // We need 2 for border
        int maxContentHeight = Math.Max(0, spaceBelow - 2);

        // Setup ListBox
        _popupListBox.Items.Clear();
        _popupListBox.Items.AddRange(Items);

        // Popup width matches ComboBox width, adjusted for border
        int contentWidth = Math.Max(0, RenderSize.Width - 2); 
        _popupListBox.Width = contentWidth;

        // Dynamic height based on items, clamped to available space
        int desiredHeight = Items.Count;
        if (desiredHeight == 0) desiredHeight = 1;
        _popupListBox.Height = Math.Min(desiredHeight, maxContentHeight);
        
        // Ensure ListBox is opaque
        _popupListBox.Background = ConsoleColor.Black;

        // Create a Border for the popup
        _popupBorder = new Border
        {
            Width = RenderSize.Width,
            Height = _popupListBox.Height + 2,
            Child = _popupListBox,
            BorderColor = ConsoleColor.White,
            BoxStyle = BoxStyle.Single
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

    public void CloseDropdown()
    {
        var root = GetRoot() as TuiWindow;
        if (root != null)
        {
            if (_popupBorder != null)
            {
                root.RemoveOverlay(_popupBorder);
                _popupBorder = null;
            }
            root.SetFocus(this);

            // Sync selection back
            if (_popupListBox.SelectedIndex >= 0 && _popupListBox.SelectedIndex < Items.Count)
            {
                SelectedItem = Items[_popupListBox.SelectedIndex];
            }
        }
        _isDroppedDown = false;
    }
}
