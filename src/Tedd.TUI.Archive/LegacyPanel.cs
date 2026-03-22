using System;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI.Archive;

public abstract class LegacyPanel : UIElement
{
    private readonly UIElementCollection _children;
    public UIElementCollection Children => _children;

    public LegacyPanel()
    {
        _children = new UIElementCollection(this);
    }

    public void AddChild(UIElement child)
    {
        _children.Add(child);
    }

    private UIElement[]? _zSortedChildren;

    public void InvalidateZState()
    {
        _zSortedChildren = null;
        Invalidate();
    }

    public void EnsureZSorted()
    {
        if (_zSortedChildren != null) return;

        int count = _children.Count;
        if (count == 0)
        {
            _zSortedChildren = Array.Empty<UIElement>();
            return;
        }

        // We use GetZIndex(c) to sort.
        // We need a stable sort. LINQ OrderBy is stable.
        _zSortedChildren = System.Linq.Enumerable.OrderBy(_children, c => Panel.GetZIndex(c)).ToArray();
    }

    public override int VisualChildrenCount => _children.Count;

    public override UIElement GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        EnsureZSorted();
        return _zSortedChildren![index];
    }
}
