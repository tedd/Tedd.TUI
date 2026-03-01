using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI;

public class TreeViewItem : HeaderedItemsControl
{
    public bool HasItems => Items.Count > 0;

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                if (_isExpanded) Expanded?.Invoke(this, EventArgs.Empty);
                else Collapsed?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                if (_isSelected) Selected?.Invoke(this, EventArgs.Empty);
                else Unselected?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    internal int Level { get; set; }
    internal bool IsLastChild { get; set; }

    public event EventHandler Expanded;
    public event EventHandler Collapsed;
    public event EventHandler Selected;
    public event EventHandler Unselected;

    internal TreeViewItem? ParentItem { get; set; }

    // Override InheritanceParent to prefer Logical Parent (ParentItem) over Visual Parent (Parent/StackPanel)
    // This ensures DataContext flows through the hierarchy even when flattened visually.
    protected override DependencyObject InheritanceParent => ParentItem ?? base.InheritanceParent;

    public TreeViewItem()
    {
    }

    protected override void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsCollectionChanged(sender, e);
        if (e.NewItems != null)
        {
            foreach (var obj in e.NewItems)
            {
                if (obj is TreeViewItem item)
                {
                    item.ParentItem = this;
                }
            }
        }
        if (e.OldItems != null)
        {
            foreach (var obj in e.OldItems)
            {
                if (obj is TreeViewItem item)
                {
                    if (item.ParentItem == this) item.ParentItem = null;
                }
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        string headerText = Header?.ToString() ?? "";
        int indent = Level * 2;
        int indicator = 4; // "[+] "

        return new Size(indent + indicator + headerText.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var bg = IsSelected ? ConsoleColor.Blue : (Background ?? ConsoleColor.Black);
        var fg = IsSelected ? ConsoleColor.White : (Header is UIElement ? ConsoleColor.White : ConsoleColor.Gray);

        // Indentation
        for (int i = 0; i < Level * 2; i++)
        {
            buffer.SetPixel(x + i, y, ' ', fg, ConsoleColor.Black);
        }

        int contentX = x + Level * 2;

        if (HasItems)
        {
            char c = IsExpanded ? '-' : '+';
            buffer.SetPixel(contentX, y, '[', ConsoleColor.DarkGray, bg);
            buffer.SetPixel(contentX + 1, y, c, ConsoleColor.White, bg);
            buffer.SetPixel(contentX + 2, y, ']', ConsoleColor.DarkGray, bg);
        }
        else
        {
            buffer.SetPixel(contentX, y, ' ', fg, bg);
            buffer.SetPixel(contentX + 1, y, ' ', fg, bg);
            buffer.SetPixel(contentX + 2, y, ' ', fg, bg);
        }

        contentX += 4;

        string text = Header?.ToString() ?? "";
        for (int i = 0; i < text.Length; i++)
        {
            buffer.SetPixel(contentX + i, y, text[i], fg, bg);
        }
    }
}
