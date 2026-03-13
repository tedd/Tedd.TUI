using System;

namespace Tedd.TUI;

public class DragStartedEventArgs : RoutedEventArgs
{
    public int HorizontalOffset { get; }
    public int VerticalOffset { get; }

    public DragStartedEventArgs(int horizontalOffset, int verticalOffset, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is DragStartedEventHandler typedHandler)
        {
            typedHandler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}

public class DragDeltaEventArgs : RoutedEventArgs
{
    public int HorizontalChange { get; }
    public int VerticalChange { get; }

    public DragDeltaEventArgs(int horizontalChange, int verticalChange, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is DragDeltaEventHandler typedHandler)
        {
            typedHandler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}

public class DragCompletedEventArgs : RoutedEventArgs
{
    public int HorizontalChange { get; }
    public int VerticalChange { get; }
    public bool Canceled { get; }

    public DragCompletedEventArgs(int horizontalChange, int verticalChange, bool canceled, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is DragCompletedEventHandler typedHandler)
        {
            typedHandler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}
