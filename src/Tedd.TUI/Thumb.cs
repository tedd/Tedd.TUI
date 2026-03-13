using System;

namespace Tedd.TUI;

public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
public delegate void DragDeltaEventHandler(object sender, DragDeltaEventArgs e);
public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);

public class Thumb : Control
{
    private bool _isDragging;
    private int _startX;
    private int _startY;
    private int _lastX;
    private int _lastY;

    public static readonly RoutedEvent DragStartedEvent =
        RoutedEvent.Register(nameof(DragStarted), RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public event DragStartedEventHandler DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public static readonly RoutedEvent DragDeltaEvent =
        RoutedEvent.Register(nameof(DragDelta), RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public event DragDeltaEventHandler DragDelta
    {
        add => AddHandler(DragDeltaEvent, value);
        remove => RemoveHandler(DragDeltaEvent, value);
    }

    public static readonly RoutedEvent DragCompletedEvent =
        RoutedEvent.Register(nameof(DragCompleted), RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    public event DragCompletedEventHandler DragCompleted
    {
        add => AddHandler(DragCompletedEvent, value);
        remove => RemoveHandler(DragCompletedEvent, value);
    }

    public bool IsDragging => _isDragging;

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!e.Handled)
        {
            _isDragging = true;
            _startX = e.GlobalX;
            _startY = e.GlobalY;
            _lastX = e.GlobalX;
            _lastY = e.GlobalY;

            if (GetRoot() is TuiWindow root)
            {
                root.CaptureMouse(this);
            }

            var args = new DragStartedEventArgs(_startX, _startY, DragStartedEvent, this);
            RaiseEvent(args);
            e.Handled = true;
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDragging)
        {
            int deltaX = e.GlobalX - _lastX;
            int deltaY = e.GlobalY - _lastY;

            if (deltaX != 0 || deltaY != 0)
            {
                _lastX = e.GlobalX;
                _lastY = e.GlobalY;

                var args = new DragDeltaEventArgs(deltaX, deltaY, DragDeltaEvent, this);
                RaiseEvent(args);
                e.Handled = true;
            }
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isDragging)
        {
            _isDragging = false;

            if (GetRoot() is TuiWindow root)
            {
                root.ReleaseMouseCapture();
            }

            int changeX = e.GlobalX - _startX;
            int changeY = e.GlobalY - _startY;

            var args = new DragCompletedEventArgs(changeX, changeY, false, DragCompletedEvent, this);
            RaiseEvent(args);
            e.Handled = true;
        }
    }
}
