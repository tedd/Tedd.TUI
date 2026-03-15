using System;

namespace Tedd.TUI;

public class Thumb : Control
{
    public static readonly RoutedEvent DragStartedEvent = RoutedEvent.Register("DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));
    public static readonly RoutedEvent DragDeltaEvent = RoutedEvent.Register("DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));
    public static readonly RoutedEvent DragCompletedEvent = RoutedEvent.Register("DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(Thumb), false);

    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        protected set => SetValue(IsDraggingProperty, value);
    }

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

    public Thumb()
    {
        // Thumb is usually focusable? In WPF it is Focusable=false by default.
        Focusable = false;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!e.Handled && IsEnabled)
        {
            var window = GetRoot() as TuiWindow;
            if (window != null)
            {
                Focus();
                window.CaptureMouse(this);

                IsDragging = true;
                _startX = e.GlobalX;
                _startY = e.GlobalY;
                _lastX = e.GlobalX;
                _lastY = e.GlobalY;

                // Fire DragStarted
                var args = new DragStartedEventArgs(0, 0)
                {
                    Source = this,
                    OriginalSource = this
                };
                RaiseEvent(args);

                e.Handled = true;
            }
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!e.Handled && IsDragging)
        {
            int dx = e.GlobalX - _lastX;
            int dy = e.GlobalY - _lastY;

            if (dx != 0 || dy != 0)
            {
                _lastX = e.GlobalX;
                _lastY = e.GlobalY;

                var args = new DragDeltaEventArgs(dx, dy)
                {
                    Source = this,
                    OriginalSource = this
                };
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
            var window = GetRoot() as TuiWindow;
            if (window != null)
            {
                window.ReleaseMouseCapture();
            }

            IsDragging = false;

            int dx = e.GlobalX - _startX;
            int dy = e.GlobalY - _startY;

            var args = new DragCompletedEventArgs(dx, dy, false)
            {
                Source = this,
                OriginalSource = this
            };
            RaiseEvent(args);

            e.Handled = true;
        }
    }

    public void CancelDrag()
    {
        if (IsDragging)
        {
            var window = GetRoot() as TuiWindow;
            if (window != null)
            {
                window.ReleaseMouseCapture();
            }

            IsDragging = false;

            int dx = _lastX - _startX;
            int dy = _lastY - _startY;

            var args = new DragCompletedEventArgs(dx, dy, true)
            {
                Source = this,
                OriginalSource = this
            };
            RaiseEvent(args);
        }
    }
}
