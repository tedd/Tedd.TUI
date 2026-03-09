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

    internal void InvalidateZState()
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

        // Fast path: if all children use the default ZIndex (0), just copy in insertion order.
        bool needsSort = false;
        for (int i = 0; i < count; i++)
        {
            if (GetZIndex(_children[i]) != 0)
            {
                needsSort = true;
                break;
            }
        }

        var arr = new UIElement[count];

        if (!needsSort)
        {
            for (int i = 0; i < count; i++)
                arr[i] = _children[i];
        }
        else
        {
            // Stable sort: build (zIndex, originalIndex) key pairs in a single pass,
            // caching the ZIndex so GetZIndex is not called again during Array.Sort.
            var keys = new (int Z, int Idx)[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = _children[i];
                keys[i] = (GetZIndex(_children[i]), i);
            }

            Array.Sort(keys, arr, Comparer<(int Z, int Idx)>.Create((a, b) =>
            {
                int c = a.Z.CompareTo(b.Z);
                return c != 0 ? c : a.Idx.CompareTo(b.Idx);
            }));
        }

        _zSortedChildren = arr;
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
