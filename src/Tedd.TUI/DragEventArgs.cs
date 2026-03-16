using System;

namespace Tedd.TUI;

public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);
public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

public class DragEventArgs : RoutedEventArgs
{
    public DragEventArgs(RoutedEvent routedEvent) : base(routedEvent)
    {
    }

    public DragEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
    {
    }
}

public class DragStartedEventArgs : DragEventArgs
{
    public int HorizontalOffset { get; }
    public int VerticalOffset { get; }

    public DragStartedEventArgs(int horizontalOffset, int verticalOffset) : base(Thumb.DragStartedEvent)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    public DragStartedEventArgs(RoutedEvent routedEvent, object source, int horizontalOffset, int verticalOffset) : base(routedEvent, source)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        var handler = (DragStartedEventHandler)genericHandler;
        handler(target, this);
    }
}

public class DragDeltaEventArgs : DragEventArgs
{
    public int HorizontalChange { get; }
    public int VerticalChange { get; }

    public DragDeltaEventArgs(int horizontalChange, int verticalChange) : base(Thumb.DragDeltaEvent)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    public DragDeltaEventArgs(RoutedEvent routedEvent, object source, int horizontalChange, int verticalChange) : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        var handler = (DragDeltaEventHandler)genericHandler;
        handler(target, this);
    }
}

public class DragCompletedEventArgs : DragEventArgs
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

    public DragCompletedEventArgs(RoutedEvent routedEvent, object source, int horizontalChange, int verticalChange, bool canceled) : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        var handler = (DragCompletedEventHandler)genericHandler;
        handler(target, this);
    }
}
