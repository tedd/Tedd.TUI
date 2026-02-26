using System;
using System.Collections.ObjectModel;

namespace Tedd.TUI;

public class UIElementCollection : Collection<UIElement>
{
    private readonly UIElement _owner;

    public UIElementCollection(UIElement owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override void InsertItem(int index, UIElement item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        base.InsertItem(index, item);
        item.Parent = _owner;
    }

    protected override void RemoveItem(int index)
    {
        var item = this[index];
        base.RemoveItem(index);
        if (item.Parent == _owner)
        {
            item.Parent = null;
        }
    }

    protected override void SetItem(int index, UIElement item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        var oldItem = this[index];
        base.SetItem(index, item);

        if (oldItem.Parent == _owner)
        {
            oldItem.Parent = null;
        }
        item.Parent = _owner;
    }

    protected override void ClearItems()
    {
        foreach (var item in this)
        {
            if (item.Parent == _owner)
            {
                item.Parent = null;
            }
        }
        base.ClearItems();
    }
}
