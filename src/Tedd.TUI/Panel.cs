using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public abstract class Panel : UIElement
{
    private readonly UIElementCollection _children;
    public UIElementCollection Children => _children;


    public static readonly DependencyProperty ZIndexProperty =
        DependencyProperty.RegisterAttached("ZIndex", typeof(int), typeof(Panel), 0);

    public static void SetZIndex(UIElement element, int value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(ZIndexProperty, value);
    }

    public static int GetZIndex(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (int)element.GetValue(ZIndexProperty);
    }

    protected Panel()
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

    private void EnsureZSorted()
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
                    zIndices[i] = GetZIndex(_zSortedChildren[i]);
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
            // Clear the used segment to avoid retaining references in the pooled array.
            Array.Clear(temp, 0, count);
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

    // Intent: Optimize Render by skipping fully clipped children
    // Why:
    // - Under unbounded constraints (ScrollViewer), Panels evaluate bounds to int.MaxValue.
    // - Rendering offscreen children wastes vast CPU resources iterating strings in bounding loops.
    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        Rect clip = buffer.GetClip();

        int count = VisualChildrenCount;
        for (int i = 0; i < count; i++)
        {
            var child = GetVisualChild(i);

            // Calculate child's absolute bounding box
            int childX = x + child.RenderSize.X;
            int childY = y + child.RenderSize.Y;
            int childW = child.RenderSize.Width;
            int childH = child.RenderSize.Height;

            // Simple intersection test with current clip
            bool overlapsClip =
                childX < clip.X + clip.Width && childX + childW > clip.X &&
                childY < clip.Y + clip.Height && childY + childH > clip.Y;

            if (overlapsClip)
            {
                child.Render(buffer, x, y);
            }
        }
    }
}
