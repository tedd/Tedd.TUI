using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tedd.TUI;

public class ItemCollection : ObservableCollection<object>
{
    private bool _isReadOnly = false;

    internal void SetReadOnly(bool isReadOnly)
    {
        _isReadOnly = isReadOnly;
    }

    public void AddRange(IEnumerable<object> collection)
    {
        CheckReadOnly();
        if (collection == null) return;
        foreach (var item in collection)
        {
            Add(item);
        }
    }

    protected override void InsertItem(int index, object item)
    {
        CheckReadOnly();
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        CheckReadOnly();
        base.RemoveItem(index);
    }

    protected override void SetItem(int index, object item)
    {
        CheckReadOnly();
        base.SetItem(index, item);
    }

    protected override void ClearItems()
    {
        CheckReadOnly();
        base.ClearItems();
    }

    // Internal methods bypass the CheckReadOnly check to allow ItemsControl to sync
    internal void InternalAdd(object item)
    {
        base.InsertItem(Count, item);
    }

    internal void InternalInsert(int index, object item)
    {
        base.InsertItem(index, item);
    }

    internal void InternalRemoveAt(int index)
    {
        base.RemoveItem(index);
    }

    internal void InternalClear()
    {
        base.ClearItems();
    }

    internal void InternalSet(int index, object item)
    {
        base.SetItem(index, item);
    }

    private void CheckReadOnly()
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("Operation is not valid while ItemsSource is in use. Access and modify elements with ItemsControl.ItemsSource instead.");
        }
    }
}
