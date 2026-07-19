using System;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI;

public abstract class Selector : ItemsControl
{
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(Selector), -1, bindsTwoWayByDefault: true);

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(Selector), null, bindsTwoWayByDefault: true);

    // Guards the SelectedIndex <-> SelectedItem cross-sync in OnPropertyChanged so a
    // change to one side updates the other exactly once without recursing.
    private bool _syncingSelection;

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty)!;
        set
        {
            // Out-of-range writes are rejected, preserving the current selection.
            if (value < -1 || value >= Items.Count) return;
            SetValue(SelectedIndexProperty, value);
        }
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public event EventHandler? SelectionChanged;

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (_syncingSelection) return;

        if (dp == SelectedIndexProperty)
        {
            _syncingSelection = true;
            try
            {
                int index = SelectedIndex;
                if (index < -1 || index >= Items.Count)
                {
                    // Values written directly through SetValue (e.g. by a binding)
                    // bypass the CLR wrapper's range check; normalize to "no selection".
                    index = -1;
                    SetValue(SelectedIndexProperty, -1);
                }
                SetValue(SelectedItemProperty, index >= 0 ? Items[index] : null);
            }
            finally
            {
                _syncingSelection = false;
            }
            OnSelectionChanged();
        }
        else if (dp == SelectedItemProperty)
        {
            _syncingSelection = true;
            try
            {
                int index = Items.IndexOf(SelectedItem);
                if (index >= 0)
                {
                    SetValue(SelectedIndexProperty, index);
                }
                else
                {
                    // Unknown item clears the selection entirely.
                    SetValue(SelectedIndexProperty, -1);
                    SetValue(SelectedItemProperty, null);
                }
            }
            finally
            {
                _syncingSelection = false;
            }
            OnSelectionChanged();
        }
    }

    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsCollectionChanged(sender, e);

        // Re-validate selection state
        object? selectedItem = SelectedItem;
        int selectedIndex = SelectedIndex;

        if (selectedItem != null)
        {
            int index = Items.IndexOf(selectedItem);
            if (index >= 0)
            {
                // The item is still present; silently re-sync the index (no
                // SelectionChanged, the logical selection did not change).
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedIndexProperty, index);
                }
                finally
                {
                    _syncingSelection = false;
                }
            }
            else
            {
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedIndexProperty, -1);
                    SetValue(SelectedItemProperty, null);
                }
                finally
                {
                    _syncingSelection = false;
                }
                // Notify that selection is lost
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
        else if (selectedIndex >= 0)
        {
            // Re-sync if SelectedIndex points to valid item
            if (selectedIndex < Items.Count)
            {
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedItemProperty, Items[selectedIndex]);
                }
                finally
                {
                    _syncingSelection = false;
                }
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            else
            {
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedIndexProperty, -1);
                }
                finally
                {
                    _syncingSelection = false;
                }
                // No change event needed if both were effectively null/invalid
            }
        }
    }
}
