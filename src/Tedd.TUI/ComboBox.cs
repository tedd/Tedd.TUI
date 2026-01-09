using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class ComboBox : UIElement
{
    private ListBox _popupListBox;
    private bool _isDroppedDown = false;
    private object _selectedItem;

    public List<object> Items { get; } = new List<object>();

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
            }
        }
    }

    public ComboBox()
    {
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

        // Draw Button look
        var bg = IsFocused ? ConsoleColor.DarkGray : ConsoleColor.Black;
        var fg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;

        string text = SelectedItem?.ToString() ?? "";
        if (text.Length > w - 2) text = text.Substring(0, w - 2);

        // Draw content
        for (int i = 0; i < w - 1; i++)
        {
            char c = (i < text.Length) ? text[i] : ' ';
            buffer.SetPixel(x + i, y, c, fg, bg);
        }

        // Draw Arrow
        buffer.SetPixel(x + w - 1, y, '▼', ConsoleColor.Black, ConsoleColor.Gray);
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
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.DownArrow)
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

        // Setup ListBox
        _popupListBox.Items.Clear();
        _popupListBox.Items.AddRange(Items);
        _popupListBox.Width = RenderSize.Width;
        _popupListBox.Height = 5; // Fixed height for now

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

        _popupListBox.Arrange(new Rect(absX, absY, _popupListBox.Width, _popupListBox.Height));

        // Unsubscribe to avoid duplicates if any
        _popupListBox.SelectionChanged -= Popup_SelectionChanged;
        _popupListBox.SelectionChanged += Popup_SelectionChanged;

        root.SetOverlay(_popupListBox);
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
            root.ClearOverlay();
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
