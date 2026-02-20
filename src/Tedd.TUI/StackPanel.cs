using System.Collections.Generic;
using System.Linq;
using System;

namespace Tedd.TUI;

public enum Orientation
{
    Horizontal,
    Vertical
}

public class StackPanel : UIElement
{
    private readonly List<UIElement> _children = new List<UIElement>();
    public IList<UIElement> Children => _children;

    public void AddChild(UIElement child)
    {
        _children.Add(child);
        child.Parent = this;
    }

    public override int VisualChildrenCount => _children.Count;

    public override UIElement GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register("Orientation", typeof(Orientation), typeof(StackPanel), Orientation.Vertical);

    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size stackSize = new Size(0, 0);
        
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            Size childSize = child.DesiredSize;

            if (Orientation == Orientation.Vertical)
            {
                stackSize.Width = Math.Max(stackSize.Width, childSize.Width);
                stackSize.Height += childSize.Height;
            }
            else
            {
                stackSize.Width += childSize.Width;
                stackSize.Height = Math.Max(stackSize.Height, childSize.Height);
            }
        }
        
        return stackSize;
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int offset = 0;

        foreach (var child in Children)
        {
            if (Orientation == Orientation.Vertical)
            {
                child.Arrange(new Rect(0, offset, finalSize.Width, child.DesiredSize.Height));
                offset += child.DesiredSize.Height;
            }
            else
            {
                child.Arrange(new Rect(offset, 0, child.DesiredSize.Width, finalSize.Height));
                offset += child.DesiredSize.Width;
            }
        }
    }

    // Intent: Optimize Render by skipping fully clipped children
    // Why:
    // - Under unbounded constraints (ScrollViewer), StackPanel evaluates bounds to int.MaxValue.
    // - Rendering offscreen children wastes vast CPU resources iterating strings in bounding loops.
    // Constraints/Invariants:
    // - Children must have accurate RenderSize relative bounding from Measure/Arrange passes.
    // Failure modes:
    // - Text or visual clipping regressions if `childX + childW > clip.X` geometry logic is off by one.
    // Verification:
    // - Verify ScrollViewer scrolled areas actually render when items track into the viewport bounds.
    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        Rect clip = buffer.GetClip();

        foreach (var child in Children)
        {
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
