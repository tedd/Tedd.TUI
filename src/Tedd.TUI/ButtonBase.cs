using System;

namespace Tedd.TUI;

public abstract class ButtonBase : ContentControl
{
    public static readonly RoutedEvent ClickEvent =
        RoutedEvent.Register("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ButtonBase));

    public event RoutedEventHandler Click
    {
        add { AddHandler(ClickEvent, value); }
        remove { RemoveHandler(ClickEvent, value); }
    }

    public static readonly DependencyProperty ClickModeProperty =
        DependencyProperty.Register("ClickMode", typeof(ClickMode), typeof(ButtonBase), ClickMode.Release);

    public ClickMode ClickMode
    {
        get => (ClickMode)GetValue(ClickModeProperty);
        set => SetValue(ClickModeProperty, value);
    }

    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.Register("IsPressed", typeof(bool), typeof(ButtonBase), false);

    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        protected set => SetValue(IsPressedProperty, value);
    }

    protected virtual void OnClick()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (GetRoot() is TuiWindow window)
            window.CaptureMouse(this);

        IsPressed = true;

        if (ClickMode == ClickMode.Press)
        {
            OnClick();
        }
        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        var window = GetRoot() as TuiWindow;
        bool hasCapture = window?.CapturedElement == this;
        bool releasedInside = !hasCapture ||
            RenderSize.Width <= 0 || RenderSize.Height <= 0 ||
            (e.X >= 0 && e.X < RenderSize.Width &&
             e.Y >= 0 && e.Y < RenderSize.Height);

        if (IsPressed)
        {
            IsPressed = false;

            if (ClickMode == ClickMode.Release && releasedInside)
            {
                OnClick();
            }
        }

        if (hasCapture)
            window!.ReleaseMouseCapture();

        e.Handled = true;
    }


    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            IsPressed = true;
            if (ClickMode == ClickMode.Press)
            {
                OnClick();
            }
            e.Handled = true;
        }
    }

    public override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == ConsoleKey.Spacebar || e.Key == ConsoleKey.Enter)
        {
            if (IsPressed)
            {
                IsPressed = false;
                if (ClickMode == ClickMode.Release)
                {
                    OnClick();
                }
            }
            e.Handled = true;
        }
    }
}
