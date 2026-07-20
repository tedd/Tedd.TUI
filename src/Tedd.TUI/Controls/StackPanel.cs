using System.Collections.Generic;
using System.Linq;
using System;

namespace Tedd.TUI.Controls;

public enum Orientation
{
    Horizontal,
    Vertical
}

public class StackPanel : Panel
{
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register("Orientation", typeof(Orientation), typeof(StackPanel), Orientation.Vertical);

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size stackSize = new Size(0, 0);

        // WPF-faithful: pass infinity along the stack axis so children report
        // their natural/desired size on that axis. Stretch on the stack axis
        // therefore has no effect (matches WPF). Use int.MaxValue as the TUI
        // equivalent of double.PositiveInfinity (the same convention used by
        // Border for scroll viewports).
        Size childAvailable = Orientation == Orientation.Vertical
            ? new Size(availableSize.Width, int.MaxValue)
            : new Size(int.MaxValue, availableSize.Height);

        foreach (var child in Children)
        {
            child.Measure(childAvailable);
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

}
