using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace Tedd.TUI;

public abstract class ItemsControl : UIElement
{
    private readonly ItemCollection _items = new ItemCollection();
    public ItemCollection Items => _items;

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ItemsControl), null);

    public IEnumerable ItemsSource
    {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(ItemsControl), null);

    public string DisplayMemberPath
    {
        get { return (string)GetValue(DisplayMemberPathProperty); }
        set { SetValue(DisplayMemberPathProperty, value); }
    }

    private IEnumerable? _currentItemsSource;
    private Dictionary<Type, PropertyInfo?> _displayMemberCache = new Dictionary<Type, PropertyInfo?>();
    private bool _isUpdating = false;

    public ItemsControl()
    {
        _items.CollectionChanged += OnItemsCollectionChanged;
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == ItemsSourceProperty)
        {
            var newValue = (IEnumerable?)GetValue(ItemsSourceProperty);
            if (_currentItemsSource != newValue)
            {
                if (_currentItemsSource is INotifyCollectionChanged oldIncc)
                {
                    oldIncc.CollectionChanged -= OnSourceCollectionChanged;
                }
                _currentItemsSource = newValue;
                OnItemsSourceChanged(newValue);
            }
        }
        else if (dp == DisplayMemberPathProperty)
        {
            _displayMemberCache.Clear();
            Invalidate();
        }
    }

    private void OnItemsSourceChanged(IEnumerable? newValue)
    {
        _isUpdating = true;
        try
        {
            if (newValue != null)
            {
                _items.SetReadOnly(false);
                _items.InternalClear();
                foreach (var item in newValue)
                {
                    _items.InternalAdd(item);
                }
                _items.SetReadOnly(true);

                if (newValue is INotifyCollectionChanged incc)
                {
                    incc.CollectionChanged += OnSourceCollectionChanged;
                }
            }
            else
            {
                _items.SetReadOnly(false);
                _items.InternalClear();
            }
        }
        finally
        {
            _isUpdating = false;
        }
        // Force full refresh
        Invalidate();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    int index = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        if (index >= 0 && index <= _items.Count)
                            _items.InternalInsert(index++, item);
                        else
                            _items.InternalAdd(item);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    int index = e.OldStartingIndex;
                    if (index >= 0)
                    {
                        for(int i=0; i<e.OldItems.Count; i++)
                        {
                             if (index < _items.Count) _items.InternalRemoveAt(index);
                        }
                    }
                    else
                    {
                        foreach (var item in e.OldItems)
                        {
                            int i = _items.IndexOf(item);
                            if (i >= 0) _items.InternalRemoveAt(i);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems != null && e.OldItems != null)
                {
                     int index = e.NewStartingIndex;
                     if (index >= 0)
                     {
                         for (int i = 0; i < e.NewItems.Count; i++)
                         {
                             if (index + i < _items.Count)
                                 _items.InternalSet(index + i, e.NewItems[i]);
                         }
                     }
                     else
                     {
                         foreach (var oldItem in e.OldItems)
                         {
                             int i = _items.IndexOf(oldItem);
                             if (i >= 0) _items.InternalRemoveAt(i);
                         }
                         foreach (var newItem in e.NewItems)
                         {
                             _items.InternalAdd(newItem);
                         }
                     }
                }
                break;
            case NotifyCollectionChangedAction.Move:
                if (e.OldItems != null && e.NewItems != null)
                {
                    int oldIndex = e.OldStartingIndex;
                    int newIndex = e.NewStartingIndex;
                    if (oldIndex >= 0 && newIndex >= 0)
                    {
                        var itemsToMove = new List<object>();
                        foreach(var item in e.OldItems) itemsToMove.Add(item);

                        for(int i=0; i<itemsToMove.Count; i++) _items.InternalRemoveAt(oldIndex);
                        for(int i=0; i<itemsToMove.Count; i++) _items.InternalInsert(newIndex + i, itemsToMove[i]);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                 _items.InternalClear();
                 if (sender is IEnumerable ie)
                 {
                     foreach(var i in ie) _items.InternalAdd(i);
                 }
                 break;
        }
    }

    protected virtual void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isUpdating) Invalidate();
    }

    public string GetItemText(object item)
    {
        if (item == null) return "";
        if (string.IsNullOrEmpty(DisplayMemberPath)) return item.ToString() ?? "";

        var type = item.GetType();
        if (!_displayMemberCache.TryGetValue(type, out var prop))
        {
            prop = type.GetProperty(DisplayMemberPath);
            _displayMemberCache[type] = prop;
        }

        if (prop != null)
        {
            return prop.GetValue(item)?.ToString() ?? "";
        }

        return item.ToString() ?? "";
    }
}
