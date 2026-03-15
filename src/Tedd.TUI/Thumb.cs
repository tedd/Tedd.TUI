using System;

namespace Tedd.TUI;

public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);
public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

public class DragStartedEventArgs : RoutedEventArgs
{
    public double HorizontalOffset { get; }
    public double VerticalOffset { get; }

    public DragStartedEventArgs(double horizontalOffset, double verticalOffset) : base(Thumb.DragStartedEvent)
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

public class DragDeltaEventArgs : RoutedEventArgs
{
    public double HorizontalChange { get; }
    public double VerticalChange { get; }

    public DragDeltaEventArgs(double horizontalChange, double verticalChange) : base(Thumb.DragDeltaEvent)
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

public class DragCompletedEventArgs : RoutedEventArgs
{
    public double HorizontalChange { get; }
    public double VerticalChange { get; }
    public bool Canceled { get; }

    public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled) : base(Thumb.DragCompletedEvent)
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

public class Thumb : Control
{
    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(Thumb), false);

    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        protected set => SetValue(IsDraggingProperty, value);
    }

    public static readonly RoutedEvent DragStartedEvent = RoutedEvent.Register(nameof(DragStarted), RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));
    public static readonly RoutedEvent DragDeltaEvent = RoutedEvent.Register(nameof(DragDelta), RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));
    public static readonly RoutedEvent DragCompletedEvent = RoutedEvent.Register(nameof(DragCompleted), RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    public event DragStartedEventHandler DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public event DragDeltaEventHandler DragDelta
    {
        add => AddHandler(DragDeltaEvent, value);
        remove => RemoveHandler(DragDeltaEvent, value);
    }

    public event DragCompletedEventHandler DragCompleted
    {
        add => AddHandler(DragCompletedEvent, value);
        remove => RemoveHandler(DragCompletedEvent, value);
    }

    private int _originX;
    private int _originY;
    private int _previousX;
    private int _previousY;

    public void CancelDrag()
    {
        if (IsDragging)
        {
            IsDragging = false;
            var root = GetRoot() as TuiWindow;
            root?.ReleaseMouseCapture();

            var e = new DragCompletedEventArgs(_previousX - _originX, _previousY - _originY, true);
            RaiseEvent(e);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!IsDragging)
        {
            IsDragging = true;
            _originX = e.GlobalX;
            _originY = e.GlobalY;
            _previousX = e.GlobalX;
            _previousY = e.GlobalY;

            var root = GetRoot() as TuiWindow;
            root?.CaptureMouse(this);

            var args = new DragStartedEventArgs(e.X, e.Y);
            RaiseEvent(args);
            e.Handled = true;
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsDragging)
        {
            if (e.GlobalX != _previousX || e.GlobalY != _previousY)
            {
                double deltaX = e.GlobalX - _previousX;
                double deltaY = e.GlobalY - _previousY;

                _previousX = e.GlobalX;
                _previousY = e.GlobalY;

                var args = new DragDeltaEventArgs(deltaX, deltaY);
                RaiseEvent(args);
            }
            e.Handled = true;
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (IsDragging)
        {
            IsDragging = false;
            var root = GetRoot() as TuiWindow;
            root?.ReleaseMouseCapture();

            var args = new DragCompletedEventArgs(e.GlobalX - _originX, e.GlobalY - _originY, false);
            RaiseEvent(args);
            e.Handled = true;
        }
    }
}
