using System;

namespace Tedd.TUI;

public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

public class DragCompletedEventArgs : RoutedEventArgs
{
    public double HorizontalChange { get; }
    public double VerticalChange { get; }
    public bool Canceled { get; }

    public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled)
        : base(Thumb.DragCompletedEvent)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    public DragCompletedEventArgs(RoutedEvent routedEvent, object source, double horizontalChange, double verticalChange, bool canceled)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is DragCompletedEventHandler handler)
        {
            handler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}
