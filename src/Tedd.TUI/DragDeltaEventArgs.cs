using System;

namespace Tedd.TUI;

public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);

public class DragDeltaEventArgs : RoutedEventArgs
{
    public double HorizontalChange { get; }
    public double VerticalChange { get; }

    public DragDeltaEventArgs(double horizontalChange, double verticalChange)
        : base(Thumb.DragDeltaEvent)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    public DragDeltaEventArgs(RoutedEvent routedEvent, object source, double horizontalChange, double verticalChange)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is DragDeltaEventHandler handler)
        {
            handler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}
