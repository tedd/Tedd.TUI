using System;

namespace Tedd.TUI;

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
}

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
}

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
}

public class Thumb : Control
{
    public static readonly RoutedEvent DragStartedEvent =
        RoutedEvent.Register("DragStarted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent =
        RoutedEvent.Register("DragDelta", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent =
        RoutedEvent.Register("DragCompleted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Thumb));

    public event RoutedEventHandler DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public event RoutedEventHandler DragDelta
    {
        add => AddHandler(DragDeltaEvent, value);
        remove => RemoveHandler(DragDeltaEvent, value);
    }

    public event RoutedEventHandler DragCompleted
    {
        add => AddHandler(DragCompletedEvent, value);
        remove => RemoveHandler(DragCompletedEvent, value);
    }

    public bool IsDragging { get; private set; }

    private Point _origin;
    private Point _previousPosition;

    public Thumb()
    {
        Focusable = false; // By default thumbs usually don't take focus themselves, their parent does
    }

    public void CancelDrag()
    {
        if (IsDragging)
        {
            IsDragging = false;
            if (GetRoot() is TuiWindow window)
            {
                window.ReleaseMouseCapture();
            }

            double horizontalChange = _previousPosition.X - _origin.X;
            double verticalChange = _previousPosition.Y - _origin.Y;

            RaiseEvent(new DragCompletedEventArgs(horizontalChange, verticalChange, true) { Source = this });
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsDragging)
        {
            e.Handled = true;
            IsDragging = true;
            _origin = new Point(e.GlobalX, e.GlobalY);
            _previousPosition = _origin;

            if (GetRoot() is TuiWindow window)
            {
                window.CaptureMouse(this);
            }

            RaiseEvent(new DragStartedEventArgs(0, 0) { Source = this });
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (IsDragging)
        {
            e.Handled = true;
            Point currentPosition = new Point(e.GlobalX, e.GlobalY);

            double horizontalChange = currentPosition.X - _previousPosition.X;
            double verticalChange = currentPosition.Y - _previousPosition.Y;

            if (horizontalChange != 0 || verticalChange != 0)
            {
                _previousPosition = currentPosition;
                RaiseEvent(new DragDeltaEventArgs(horizontalChange, verticalChange) { Source = this });
            }
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (IsDragging)
        {
            e.Handled = true;
            IsDragging = false;

            if (GetRoot() is TuiWindow window)
            {
                window.ReleaseMouseCapture();
            }

            Point currentPosition = new Point(e.GlobalX, e.GlobalY);
            double horizontalChange = currentPosition.X - _origin.X;
            double verticalChange = currentPosition.Y - _origin.Y;

            RaiseEvent(new DragCompletedEventArgs(horizontalChange, verticalChange, false) { Source = this });
        }
    }
}
