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
            base.InvokeEventHandler(genericHandler, target);
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
            base.InvokeEventHandler(genericHandler, target);
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
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}

public class Thumb : Control
{
    public static readonly RoutedEvent DragStartedEvent = RoutedEvent.Register(
        "DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent = RoutedEvent.Register(
        "DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent = RoutedEvent.Register(
        "DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

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

    public bool IsDragging { get; private set; }

    private int _startX;
    private int _startY;
    private int _lastX;
    private int _lastY;

    public Thumb()
    {
        Focusable = false; // Usually thumbs are not focusable directly
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsDragging)
        {
            IsDragging = true;
            _startX = e.GlobalX;
            _startY = e.GlobalY;
            _lastX = e.GlobalX;
            _lastY = e.GlobalY;

            var window = GetRoot() as TuiWindow;
            window?.CaptureMouse(this);

            var args = new DragStartedEventArgs(_startX, _startY) { Source = this };
            RaiseEvent(args);

            e.Handled = true;
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (IsDragging)
        {
            int dx = e.GlobalX - _lastX;
            int dy = e.GlobalY - _lastY;

            if (dx != 0 || dy != 0)
            {
                var args = new DragDeltaEventArgs(dx, dy) { Source = this };
                RaiseEvent(args);

                _lastX = e.GlobalX;
                _lastY = e.GlobalY;
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

            var window = GetRoot() as TuiWindow;
            window?.ReleaseMouseCapture();

            int dx = e.GlobalX - _startX;
            int dy = e.GlobalY - _startY;

            var args = new DragCompletedEventArgs(dx, dy, false) { Source = this };
            RaiseEvent(args);

            e.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(1, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        // Render something visible to make it testable/visible
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        buffer.SetPixel(x, y, '░', Foreground, Background ?? ConsoleColor.Black);
    }
}
