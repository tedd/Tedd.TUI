using System;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI;

public abstract class Selector : ItemsControl
{
    private bool _isUpdatingSelection;

    public int SelectedIndex
    {
        get => field;
        set
        {
            if (field != value)
            {
                if (value < -1 || value >= Items.Count) return;
                field = value;
                if (!_isUpdatingSelection) OnSelectionChanged();
            }
        }
    } = -1;

    public object? SelectedItem
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                // Sync Index
                _isUpdatingSelection = true;
                try
                {
                    int index = Items.IndexOf(value);
                    if (index >= 0)
                    {
                        SelectedIndex = index;
                    }
                    else
                    {
                        SelectedIndex = -1;
                        field = null;
                    }
                }
                finally
                {
                    _isUpdatingSelection = false;
                }
                OnSelectionChanged();
            }
        }
    }

    public event EventHandler? SelectionChanged;

    protected virtual void OnSelectionChanged()
    {
        _isUpdatingSelection = true;
        try
        {
            // Keep SelectedItem in sync if Index changed first
            if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
            {
                SelectedItem = Items[SelectedIndex];
            }
            else
            {
                SelectedItem = null;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsCollectionChanged(sender, e);

        // Re-validate selection state
        if (SelectedItem != null)
        {
            int index = Items.IndexOf(SelectedItem);
            if (index >= 0)
            {
                _isUpdatingSelection = true;
                try { SelectedIndex = index; }
                finally { _isUpdatingSelection = false; }
            }
            else
            {
                _isUpdatingSelection = true;
                try
                {
                    SelectedIndex = -1;
                    SelectedItem = null;
                }
                finally { _isUpdatingSelection = false; }
                // Notify that selection is lost
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
        else if (SelectedIndex >= 0)
        {
            // Re-sync if SelectedIndex points to valid item
            if (SelectedIndex < Items.Count)
            {
                _isUpdatingSelection = true;
                try { SelectedItem = Items[SelectedIndex]; }
                finally { _isUpdatingSelection = false; }
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            else
            {
                _isUpdatingSelection = true;
                try { SelectedIndex = -1; }
                finally { _isUpdatingSelection = false; }
                // No change event needed if both were effectively null/invalid
            }
        }
    }
}
