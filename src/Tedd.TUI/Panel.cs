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
        // We need a stable sort. We use MemoryExtensions.Sort with a composed key (ZIndex as high 32 bits, index as low 32 bits) to eliminate allocations.
        // This takes O(N log N) time and O(N) space (from ArrayPool), avoiding complex merge-sort overhead and extra array rentals.
        long[] keys = System.Buffers.ArrayPool<long>.Shared.Rent(count);
        try
        {
            for (int i = 0; i < count; i++)
            {
                // Encode ZIndex into high 32 bits, and original index into low 32 bits for stable sorting.
                // A long preserves the signed bit properly.
                keys[i] = ((long)GetZIndex(_zSortedChildren[i]) << 32) | (uint)i;
            }

            MemoryExtensions.Sort(keys.AsSpan(0, count), _zSortedChildren.AsSpan(0, count));
        }
        finally
        {
            System.Buffers.ArrayPool<long>.Shared.Return(keys);
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
