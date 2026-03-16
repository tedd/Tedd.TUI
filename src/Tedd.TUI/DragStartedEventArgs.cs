using System;

namespace Tedd.TUI;

public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);

public class DragStartedEventArgs : RoutedEventArgs
{
    public double HorizontalOffset { get; }
    public double VerticalOffset { get; }

    public DragStartedEventArgs(double horizontalOffset, double verticalOffset)
        : base(Thumb.DragStartedEvent)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    public DragStartedEventArgs(RoutedEvent routedEvent, object source, double horizontalOffset, double verticalOffset)
        : base(routedEvent, source)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is DragStartedEventHandler handler)
        {
            handler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}
