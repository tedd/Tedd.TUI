using System;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI;

public abstract class Selector : ItemsControl
{
    public Selector()
    {
        AddHandler(ListBoxItem.SelectedEvent, new RoutedEventHandler(OnItemSelected));
    }

    private void OnItemSelected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ListBoxItem item)
        {
            var dataItem = ItemsControlFromItemContainer(item);
            if (dataItem != DependencyProperty.UnsetValue)
            {
                SelectedItem = dataItem;
            }
        }
    }

    private object ItemsControlFromItemContainer(DependencyObject container)
    {
        // Simple logic to find the item. We should ideally look it up, but for now we can iterate.
        // If container is UIElement, we can just find it in Items if it is its own container, or we need ItemContainerGenerator equivalent.
        // In our ItemsControl, we generate containers and add them to ItemsPanel.
        if (ItemsPresenter != null && ItemsPresenter.GetVisualChild(0) is Panel panel)
        {
            int index = panel.Children.IndexOf((UIElement)container);
            if (index >= 0 && index < Items.Count)
            {
                return Items[index];
            }
        }
        return DependencyProperty.UnsetValue;
    }

    private int _selectedIndex = -1;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                if (value < -1 || value >= Items.Count) return;
                _selectedIndex = value;
                OnSelectionChanged();
            }
        }
    }

    private object? _selectedItem;
    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                // Sync Index
                int index = Items.IndexOf(value);
                if (index >= 0)
                {
                    _selectedIndex = index;
                }
                else
                {
                    _selectedIndex = -1;
                    _selectedItem = null;
                }
                OnSelectionChanged();
            }
        }
    }

    public event EventHandler? SelectionChanged;

    protected virtual void OnSelectionChanged()
    {
        // Keep SelectedItem in sync if Index changed first
        if (_selectedIndex >= 0 && _selectedIndex < Items.Count)
        {
            _selectedItem = Items[_selectedIndex];
        }
        else
        {
            _selectedItem = null;
        }

        UpdateContainerSelection();

        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void UpdateContainerSelection()
    {
        if (ItemsPresenter != null && ItemsPresenter.GetVisualChild(0) is Panel panel)
        {
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is ListBoxItem lbi)
                {
                    lbi.IsSelected = (i == _selectedIndex);
                }
            }
        }
    }

    protected internal override void PrepareContainerForItemOverride(UIElement element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is ListBoxItem lbi)
        {
            bool isSelected = false;
            if (_selectedIndex >= 0 && _selectedIndex < Items.Count && Items[_selectedIndex] == item)
            {
                isSelected = true;
            }
            lbi.IsSelected = isSelected;
        }
    }

    protected override void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsCollectionChanged(sender, e);

        // Re-validate selection state
        if (_selectedItem != null)
        {
            int index = Items.IndexOf(_selectedItem);
            if (index >= 0)
            {
                _selectedIndex = index;
            }
            else
            {
                _selectedIndex = -1;
                _selectedItem = null;
                // Notify that selection is lost
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
        else if (_selectedIndex >= 0)
        {
            // Re-sync if SelectedIndex points to valid item
            if (_selectedIndex < Items.Count)
            {
                _selectedItem = Items[_selectedIndex];
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            else
            {
                _selectedIndex = -1;
                // No change event needed if both were effectively null/invalid
            }
        }
    }
}
