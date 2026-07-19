using System;

namespace Tedd.TUI;

public class DragStartedEventArgs : RoutedEventArgs
{
    public double HorizontalOffset { get; }
    public double VerticalOffset { get; }

    public DragStartedEventArgs(double horizontalOffset, double verticalOffset, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }
}

public class DragDeltaEventArgs : RoutedEventArgs
{
    public double HorizontalChange { get; }
    public double VerticalChange { get; }

    public DragDeltaEventArgs(double horizontalChange, double verticalChange, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }
}

public class DragCompletedEventArgs : RoutedEventArgs
{
    public double HorizontalChange { get; }
    public double VerticalChange { get; }
    public bool Canceled { get; }

    public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }
}

public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);
public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

public class Thumb : Control
{
    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.Register("IsDragging", typeof(bool), typeof(Thumb), false);

    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        protected set => SetValue(IsDraggingProperty, value);
    }

    public static readonly RoutedEvent DragStartedEvent =
        RoutedEvent.Register("DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent =
        RoutedEvent.Register("DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent =
        RoutedEvent.Register("DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

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

    // Fractional global coordinates (cell units). Pixel-based hosts report sub-cell
    // positions, so drag deltas can be smaller than one cell; terminal hosts report
    // whole cells and deltas stay integral.
    private double _originXF, _originYF;
    private double _previousXF, _previousYF;

    public Thumb()
    {
        Focusable = false;
    }

    public void CancelDrag()
    {
        if (IsDragging)
        {
            var root = GetRoot() as TuiWindow;
            if (root?.CapturedElement == this)
            {
                root.ReleaseMouseCapture();
            }

            IsDragging = false;
            var args = new DragCompletedEventArgs(
                _previousXF - _originXF,
                _previousYF - _originYF,
                true,
                DragCompletedEvent,
                this);
            RaiseEvent(args);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!e.Handled && !IsDragging)
        {
            var root = GetRoot() as TuiWindow;
            if (root != null)
            {
                e.Handled = true;
                Focus();
                root.CaptureMouse(this);
                IsDragging = true;
                _originXF = e.GlobalXF;
                _originYF = e.GlobalYF;
                _previousXF = _originXF;
                _previousYF = _originYF;

                var args = new DragStartedEventArgs(_originXF, _originYF, DragStartedEvent, this);
                RaiseEvent(args);
            }
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsDragging)
        {
            var root = GetRoot() as TuiWindow;
            if (root?.CapturedElement == this)
            {
                double currentX = e.GlobalXF;
                double currentY = e.GlobalYF;

                if (currentX != _previousXF || currentY != _previousYF)
                {
                    double deltaX = currentX - _previousXF;
                    double deltaY = currentY - _previousYF;

                    _previousXF = currentX;
                    _previousYF = currentY;

                    var args = new DragDeltaEventArgs(deltaX, deltaY, DragDeltaEvent, this);
                    RaiseEvent(args);
                }
                e.Handled = true;
            }
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (IsDragging)
        {
            var root = GetRoot() as TuiWindow;
            if (root?.CapturedElement == this)
            {
                root.ReleaseMouseCapture();
            }

            IsDragging = false;
            e.Handled = true;

            var args = new DragCompletedEventArgs(
                _previousXF - _originXF,
                _previousYF - _originYF,
                false,
                DragCompletedEvent,
                this);
            RaiseEvent(args);
        }
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        if (IsDragging)
        {
            CancelDrag();
        }
    }
}
