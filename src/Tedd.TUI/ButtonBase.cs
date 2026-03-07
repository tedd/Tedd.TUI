using System;

namespace Tedd.TUI;

public abstract class ButtonBase : ContentControl
{
    public ButtonBase()
    {
        Focusable = true;
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

    public static readonly RoutedEvent ClickEvent =
        RoutedEvent.Register("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ButtonBase));

    public event RoutedEventHandler Click
    {
        add { AddHandler(ClickEvent, value); }
        remove { RemoveHandler(ClickEvent, value); }
    }

    protected virtual void OnClick()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        IsPressed = true;

        var root = GetRoot() as TuiWindow;
        root?.CaptureMouse(this);

        if (ClickMode == ClickMode.Press)
        {
            OnClick();
        }

        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (IsPressed)
        {
            IsPressed = false;
            var root = GetRoot() as TuiWindow;
            root?.ReleaseMouseCapture();
            if (ClickMode == ClickMode.Release)
            {
                OnClick();
            }
            e.Handled = true;
        }
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        if (IsPressed)
        {
            IsPressed = false;
            var root = GetRoot() as TuiWindow;
            root?.ReleaseMouseCapture();
        }
    }

    private bool _isHovering;

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (ClickMode == ClickMode.Hover && !_isHovering)
        {
            _isHovering = true;
            OnClick();
            e.Handled = true;
        }
    }

    // TUI doesn't have OnMouseLeave/OnMouseEnter yet,
    // but when _isHovering is true, we need a way to reset it.
    // For now, WPF ClickMode.Hover triggers on MouseEnter. We will simulate it with MouseMove.

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
                e.Handled = true;
            }
        }
    }
}
