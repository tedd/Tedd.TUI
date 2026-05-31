using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI;

public class TreeViewItem : HeaderedItemsControl
{
    public bool HasItems => Items.Count > 0;

    public bool IsExpanded
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                if (field) Expanded?.Invoke(this, EventArgs.Empty);
                else Collapsed?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    public bool IsSelected
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                if (field) Selected?.Invoke(this, EventArgs.Empty);
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

        var bg = IsSelected ? TuiColor.Blue : (Background ?? TuiColor.Black);
        var fg = IsSelected ? TuiColor.White : (Header is UIElement ? TuiColor.White : TuiColor.Gray);

        // Indentation
        for (int i = 0; i < Level * 2; i++)
        {
            buffer.SetPixel(x + i, y, ' ', fg, TuiColor.Black);
        }

        int contentX = x + Level * 2;

        if (HasItems)
        {
            char c = IsExpanded ? '-' : '+';
            buffer.SetPixel(contentX, y, '[', TuiColor.DarkGray, bg);
            buffer.SetPixel(contentX + 1, y, c, TuiColor.White, bg);
            buffer.SetPixel(contentX + 2, y, ']', TuiColor.DarkGray, bg);
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

    // Bug: Clicking a TreeViewItem does not focus the TreeView, change selection, or expand/collapse the node.
    // Root cause: TreeViewItem lacked OnMouseDown override, letting click events bubble unhandled to containers.
    // Fix: Add OnMouseDown to focus the parent TreeView, set SelectedItem, toggle expansion if clicked on indicator, and set Handled.
    // Regression: TreeViewCoverageTests.TreeViewItem_OnMouseDown_SelectsAndExpands
    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Handled) return;
        base.OnMouseDown(e);

        var tree = FindAncestor<TreeView>();
        if (tree != null)
        {
            tree.Focus();
            tree.SelectedItem = this;
        }

        if (HasItems && e.X >= Level * 2 && e.X < Level * 2 + 3)
        {
            IsExpanded = !IsExpanded;
        }

        e.Handled = true;
    }
}
