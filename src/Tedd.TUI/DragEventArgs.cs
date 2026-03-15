using System;

namespace Tedd.TUI;

public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);
public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

public class DragStartedEventArgs : RoutedEventArgs
{
    public int HorizontalOffset { get; }
    public int VerticalOffset { get; }

    public DragStartedEventArgs(int horizontalOffset, int verticalOffset) : base(Thumb.DragStartedEvent)
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
            genericHandler.DynamicInvoke(target, this);
        }
    }
}

public class DragDeltaEventArgs : RoutedEventArgs
{
    public int HorizontalChange { get; }
    public int VerticalChange { get; }

    public DragDeltaEventArgs(int horizontalChange, int verticalChange) : base(Thumb.DragDeltaEvent)
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
            genericHandler.DynamicInvoke(target, this);
        }
    }
}

public class DragCompletedEventArgs : RoutedEventArgs
{
    public int HorizontalChange { get; }
    public int VerticalChange { get; }
    public bool Canceled { get; }

    public DragCompletedEventArgs(int horizontalChange, int verticalChange, bool canceled) : base(Thumb.DragCompletedEvent)
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
            genericHandler.DynamicInvoke(target, this);
        }
    }
}
