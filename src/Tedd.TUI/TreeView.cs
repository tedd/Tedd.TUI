using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace Tedd.TUI;

public class TreeView : UIElement
{
    private readonly ScrollViewer _scrollViewer;
    private readonly StackPanel _stackPanel;
    private ObservableCollection<TreeViewItem> _items = [];
    public IList<TreeViewItem> Items => _items;

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(TreeView), null);

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set
        {
            SetValue(ItemsSourceProperty, value);
            GenerateItems();
        }
    }

    private System.Threading.Lock _displayMemberCacheLock = new();
    private Dictionary<Type, PropertyInfo?> _displayMemberCache = [];

    public string DisplayMemberPath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                lock (_displayMemberCacheLock)
                {
                    _displayMemberCache.Clear();
                }
                RebuildVisualTree();
            }
        }
    }

    private System.Threading.Lock _childItemsCacheLock = new();
    private Dictionary<Type, PropertyInfo?> _childItemsCache = [];

    public string ChildItemsPath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                lock (_childItemsCacheLock)
                {
                    _childItemsCache.Clear();
                }
                RebuildVisualTree();
            }
        }
    }

    public TreeViewItem? SelectedItem
    {
        get;
        set
        {
            if (field != value)
            {
                if (field != null) field.IsSelected = false;
                field = value;
                if (field != null)
                {
                    // Auto-expand parents
                    var parent = field.ParentItem;
                    while (parent != null)
                    {
                        if (!parent.IsExpanded) parent.IsExpanded = true;
                        parent = parent.ParentItem;
                    }
                    field.IsSelected = true;
                }
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                EnsureVisible(field);
            }
        }
    }

    public event EventHandler SelectionChanged;

    public TreeView()
    {
        Focusable = true;
        _stackPanel = new StackPanel { Orientation = Orientation.Vertical };
        _scrollViewer = new ScrollViewer
        {
            Content = _stackPanel,
            HorizontalScrollBarVisibility = true,
            VerticalScrollBarVisibility = true
        };
        _scrollViewer.Parent = this; // Set visual parent

        _items.CollectionChanged += OnItemsChanged;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TreeViewItem item in e.NewItems)
            {
                item.ParentItem = null; // Top level has no parent item
                // But it might need TreeView reference?
                // We don't store TreeView ref in item explicitly, but item relies on visual/logical tree.
                SubscribeItem(item);
            }
        }
        if (e.OldItems != null)
        {
            foreach (TreeViewItem item in e.OldItems)
            {
                UnsubscribeItem(item);
            }
        }
        RebuildVisualTree();
    }

    private void SubscribeItem(TreeViewItem item)
    {
        item.Expanded += OnItemExpanded;
        item.Collapsed += OnItemCollapsed;
        item.Selected += OnItemSelected;
        // Recursively subscribe children if they exist?
        // Only if they are already in Items collection of the item.
        foreach (var sub in item.Items)
        {
            if (sub is TreeViewItem tvi) SubscribeItem(tvi);
        }

        // Also need to listen to sub-items collection change?
        // Ideally yes, but TreeViewItem logic should handle notification bubbling?
        // Or we just re-subscribe when Expanded happens?
        // If an item is collapsed, changes inside don't matter for visual tree.
        // But if we expand, we need to see new items.
        // TreeViewItem doesn't expose CollectionChanged for its Items publicly as an event we can hook easily without casting.
        // But we can cast to ObservableCollection or INotifyCollectionChanged.
        if (item.Items is INotifyCollectionChanged lincc)
        {
            lincc.CollectionChanged += (s, e) =>
            {
                // Re-subscribe new items
                if (e.NewItems != null) foreach (var i in e.NewItems) { if (i is TreeViewItem tvi) SubscribeItem(tvi); }
                if (e.OldItems != null) foreach (var i in e.OldItems) { if (i is TreeViewItem tvi) UnsubscribeItem(tvi); }
                // If expanded, rebuild
                if (item.IsExpanded) RebuildVisualTree();
            };
        }
    }

    private void UnsubscribeItem(TreeViewItem item)
    {
        item.Expanded -= OnItemExpanded;
        item.Collapsed -= OnItemCollapsed;
        item.Selected -= OnItemSelected;
        foreach (var sub in item.Items)
        {
            if (sub is TreeViewItem tvi) UnsubscribeItem(tvi);
        }
    }

    private void OnItemExpanded(object? sender, EventArgs e) => RebuildVisualTree();
    private void OnItemCollapsed(object? sender, EventArgs e) => RebuildVisualTree();

    private void OnItemSelected(object? sender, EventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            SelectedItem = item;
        }
    }

    private void GenerateItems()
    {
        _items.Clear();
        if (ItemsSource == null) return;

        foreach (var data in ItemsSource)
        {
            var item = CreateTreeViewItem(data);
            _items.Add(item);
        }
    }

    private TreeViewItem CreateTreeViewItem(object data)
    {
        if (data is TreeViewItem tvi) return tvi;

        var item = new TreeViewItem();
        item.DataContext = data;

        // Header
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            var type = data.GetType();
            PropertyInfo? prop = null;
            lock (_displayMemberCacheLock)
            {
                if (!_displayMemberCache.TryGetValue(type, out prop))
                {
                    prop = type.GetProperty(DisplayMemberPath);
                    _displayMemberCache[type] = prop;
                }
            }
            item.Header = prop?.GetValue(data) ?? data.ToString();
        }
        else
        {
            item.Header = data;
        }

        // Children
        // We can't bind ItemsSource on TreeViewItem easily without complex logic.
        // We will do one-time generation for now (lazy loading not supported in this simple version).
        if (!string.IsNullOrEmpty(ChildItemsPath))
        {
            var type = data.GetType();
            PropertyInfo? prop = null;
            lock (_childItemsCacheLock)
            {
                if (!_childItemsCache.TryGetValue(type, out prop))
                {
                    prop = type.GetProperty(ChildItemsPath);
                    _childItemsCache[type] = prop;
                }
            }
            if (prop != null)
            {
                var children = prop.GetValue(data) as IEnumerable;
                if (children != null)
                {
                    foreach (var childData in children)
                    {
                        item.Items.Add(CreateTreeViewItem(childData));
                    }
                }
            }
        }

        return item;
    }

    private void RebuildVisualTree()
    {
        _stackPanel.Children.Clear();
        foreach (var item in _items)
        {
            AddVisibleItems(item, 0);
        }
        Invalidate();
    }

    private void AddVisibleItems(TreeViewItem item, int level)
    {
        item.Level = level;
        _stackPanel.AddChild(item); // This sets item.Parent = _stackPanel (Visual Parent)
        // item.ParentItem handles Logical Parent via override.

        if (item.IsExpanded)
        {
            foreach (var sub in item.Items)
            {
                if (sub is TreeViewItem tvi) AddVisibleItems(tvi, level + 1);
            }
        }
    }

    public override int VisualChildrenCount => 1;
    public override UIElement GetVisualChild(int index)
    {
        if (index == 0) return _scrollViewer;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _scrollViewer.Measure(availableSize);
        return _scrollViewer.DesiredSize;
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        _scrollViewer.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        _scrollViewer.Render(buffer, offsetX, offsetY);
    }

    // Input Handling
    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (SelectedItem == null && _items.Count > 0)
        {
            SelectedItem = _items[0];
            e.Handled = true;
            return;
        }

        if (SelectedItem == null) return;

        if (e.Key == ConsoleKey.DownArrow)
        {
            // Find next visible item in stack panel
            int idx = _stackPanel.Children.IndexOf(SelectedItem);
            if (idx >= 0 && idx < _stackPanel.Children.Count - 1)
            {
                var next = _stackPanel.Children[idx + 1] as TreeViewItem;
                if (next != null) SelectedItem = next;
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.UpArrow)
        {
            int idx = _stackPanel.Children.IndexOf(SelectedItem);
            if (idx > 0)
            {
                var prev = _stackPanel.Children[idx - 1] as TreeViewItem;
                if (prev != null) SelectedItem = prev;
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.RightArrow)
        {
            if (SelectedItem.HasItems && !SelectedItem.IsExpanded)
            {
                SelectedItem.IsExpanded = true;
            }
            else if (SelectedItem.HasItems && SelectedItem.IsExpanded)
            {
                // Go to first child
                if (SelectedItem.Items.Count > 0 && SelectedItem.Items[0] is TreeViewItem tvi) SelectedItem = tvi;
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.LeftArrow)
        {
            if (SelectedItem.IsExpanded)
            {
                SelectedItem.IsExpanded = false;
            }
            else if (SelectedItem.ParentItem != null)
            {
                SelectedItem = SelectedItem.ParentItem;
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            SelectedItem.IsExpanded = !SelectedItem.IsExpanded;
            e.Handled = true;
        }
    }

    private void EnsureVisible(TreeViewItem? item)
    {
        if (item == null) return;
        // ScrollViewer doesn't have "ScrollIntoView" logic exposed easily.
        // We need to calculate offset.
        // Simple logic: if item index in stackpanel is outside viewport, scroll.
        // Since StackPanel is vertical, item Y is sum of heights of previous items.
        // Assuming height 1 for all items.
        int idx = _stackPanel.Children.IndexOf(item);
        if (idx < 0) return;

        int itemY = idx; // 1 height per item
        int viewportH = _scrollViewer.RenderSize.Height;
        int scrollY = _scrollViewer.VerticalOffset;

        if (itemY < scrollY)
        {
            _scrollViewer.ScrollToVerticalOffset(itemY);
        }
        else if (itemY >= scrollY + viewportH)
        {
            _scrollViewer.ScrollToVerticalOffset(itemY - viewportH + 1);
        }
    }
}
