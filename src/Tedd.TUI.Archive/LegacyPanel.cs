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

        // Allocate only once
        _zSortedChildren = new UIElement[count];
        _children.CopyTo(_zSortedChildren, 0);

        // We use GetZIndex(c) to sort.
        // We need a stable sort. We implement an O(N log N) iterative merge sort to eliminate allocations while maintaining stability.
        UIElement[] temp = System.Buffers.ArrayPool<UIElement>.Shared.Rent(count);
        try
        {
            int[] zIndices = System.Buffers.ArrayPool<int>.Shared.Rent(count);
            int[] tempZIndices = System.Buffers.ArrayPool<int>.Shared.Rent(count);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    zIndices[i] = Panel.GetZIndex(_zSortedChildren[i]);
                }

                // Iterative Merge Sort
                for (int width = 1; width < count; width = 2 * width)
                {
                    for (int i = 0; i < count; i += 2 * width)
                    {
                        int left = i;
                        int mid = Math.Min(i + width, count);
                        int right = Math.Min(i + 2 * width, count);

                        int l = left;
                        int r = mid;
                        int k = left;

                        while (l < mid && r < right)
                        {
                            if (zIndices[l] <= zIndices[r]) // <= ensures stability
                            {
                                tempZIndices[k] = zIndices[l];
                                temp[k] = _zSortedChildren[l];
                                l++;
                            }
                            else
                            {
                                tempZIndices[k] = zIndices[r];
                                temp[k] = _zSortedChildren[r];
                                r++;
                            }
                            k++;
                        }

                        while (l < mid)
                        {
                            tempZIndices[k] = zIndices[l];
                            temp[k] = _zSortedChildren[l];
                            l++;
                            k++;
                        }

                        while (r < right)
                        {
                            tempZIndices[k] = zIndices[r];
                            temp[k] = _zSortedChildren[r];
                            r++;
                            k++;
                        }

                        for (int j = left; j < right; j++)
                        {
                            _zSortedChildren[j] = temp[j];
                            zIndices[j] = tempZIndices[j];
                        }
                    }
                }
            }
            finally
            {
                System.Buffers.ArrayPool<int>.Shared.Return(zIndices);
                System.Buffers.ArrayPool<int>.Shared.Return(tempZIndices);
            }
        }
        finally
        {
            System.Buffers.ArrayPool<UIElement>.Shared.Return(temp);
        }
    }

    public override int VisualChildrenCount => _children.Count;

    public override UIElement GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        EnsureZSorted();
        return _zSortedChildren![index];
    }
}
