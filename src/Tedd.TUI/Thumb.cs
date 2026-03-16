using System;

namespace Tedd.TUI;

public class Thumb : Control
{
    // Dependency Properties
    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(Thumb), false);

    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        protected set => SetValue(IsDraggingProperty, value);
    }

    // Routed Events
    public static readonly RoutedEvent DragStartedEvent =
        RoutedEvent.Register(nameof(DragStarted), RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent =
        RoutedEvent.Register(nameof(DragDelta), RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent =
        RoutedEvent.Register(nameof(DragCompleted), RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    // CLR Event Wrappers
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

    private int _startX;
    private int _startY;
    private int _lastX;
    private int _lastY;

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsDragging)
        {
            e.Handled = true;
            (this.GetRoot() as TuiWindow)?.CaptureMouse(this);
            IsDragging = true;

            _startX = e.GlobalX;
            _startY = e.GlobalY;
            _lastX = e.GlobalX;
            _lastY = e.GlobalY;

            var args = new DragStartedEventArgs(DragStartedEvent, this, 0, 0); // initial offset is 0 relative to where mouse went down
            RaiseEvent(args);
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (IsDragging)
        {
            e.Handled = true;

            int dx = e.GlobalX - _lastX;
            int dy = e.GlobalY - _lastY;

            if (dx != 0 || dy != 0)
            {
                var args = new DragDeltaEventArgs(DragDeltaEvent, this, dx, dy);
                RaiseEvent(args);

                _lastX = e.GlobalX;
                _lastY = e.GlobalY;
            }
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (IsDragging)
        {
            e.Handled = true;
            (this.GetRoot() as TuiWindow)?.ReleaseMouseCapture();
            IsDragging = false;

            int totalDx = e.GlobalX - _startX;
            int totalDy = e.GlobalY - _startY;

            var args = new DragCompletedEventArgs(DragCompletedEvent, this, totalDx, totalDy, false);
            RaiseEvent(args);
        }
    }
}
