using System;

namespace Tedd.TUI;

public class Thumb : Control
{
    private int _previousOriginX;
    private int _previousOriginY;
    private int _originX;
    private int _originY;

    public Thumb()
    {
        Focusable = true;
    }

    public static readonly RoutedEvent DragStartedEvent = RoutedEvent.Register(
        "DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent = RoutedEvent.Register(
        "DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent = RoutedEvent.Register(
        "DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
        nameof(IsDragging), typeof(bool), typeof(Thumb), false);

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

    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        protected set => SetValue(IsDraggingProperty, value);
    }

    public void CancelDrag()
    {
        if (IsDragging)
        {
            if (GetRoot() is TuiWindow root)
            {
                root.ReleaseMouseCapture();
            }

            IsDragging = false;
            RaiseEvent(new DragCompletedEventArgs(DragCompletedEvent, this, _previousOriginX - _originX, _previousOriginY - _originY, true));
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsDragging)
        {
            Focus();
            if (GetRoot() is TuiWindow root)
            {
                root.CaptureMouse(this);
            }

            IsDragging = true;
            _originX = e.X;
            _originY = e.Y;
            _previousOriginX = e.X;
            _previousOriginY = e.Y;

            RaiseEvent(new DragStartedEventArgs(DragStartedEvent, this, _originX, _originY));
            e.Handled = true;
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (IsDragging)
        {
            if (e.X != _previousOriginX || e.Y != _previousOriginY)
            {
                int deltaX = e.X - _previousOriginX;
                int deltaY = e.Y - _previousOriginY;

                _previousOriginX = e.X;
                _previousOriginY = e.Y;

                RaiseEvent(new DragDeltaEventArgs(DragDeltaEvent, this, deltaX, deltaY));
            }
            e.Handled = true;
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (IsDragging)
        {
            if (GetRoot() is TuiWindow root)
            {
                root.ReleaseMouseCapture();
            }

            IsDragging = false;
            RaiseEvent(new DragCompletedEventArgs(DragCompletedEvent, this, _previousOriginX - _originX, _previousOriginY - _originY, false));
            e.Handled = true;
        }
    }
}
