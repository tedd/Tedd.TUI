using System;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI;

public abstract class Selector : ItemsControl
{
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

        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
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
