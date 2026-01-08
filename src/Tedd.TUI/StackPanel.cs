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
        child.DataContext = this.DataContext; // Inherit current context
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register("Orientation", typeof(Orientation), typeof(StackPanel), Orientation.Vertical);

    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        foreach (var child in Children)
        {
            child.DataContext = newValue;
        }
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

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        foreach (var child in Children)
        {
            child.Render(buffer, x, y);
        }
    }
}
