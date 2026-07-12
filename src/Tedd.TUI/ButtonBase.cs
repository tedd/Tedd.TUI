using System;
using System.Windows.Input;

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

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(ButtonBase), null);

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register("CommandParameter", typeof(object), typeof(ButtonBase), null);

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private ICommand? _currentCommand;
    private bool? _preCommandIsEnabled;

    protected override void OnParentChanged()
    {
        base.OnParentChanged();
        if (Parent == null)
        {
            if (_currentCommand != null)
            {
                _currentCommand.CanExecuteChanged -= OnCanExecuteChanged;
            }
        }
        else
        {
            if (_currentCommand != null)
            {
                _currentCommand.CanExecuteChanged += OnCanExecuteChanged;
            }
        }
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == CommandProperty)
        {
            var newCommand = Command;
            if (_currentCommand != newCommand)
            {
                if (_currentCommand != null)
                {
                    _currentCommand.CanExecuteChanged -= OnCanExecuteChanged;
                }
                else
                {
                    // Command is being set, snapshot the current local value of IsEnabled if we haven't already
                    if (!_preCommandIsEnabled.HasValue)
                    {
                        _preCommandIsEnabled = IsEnabled;
                    }
                }

                _currentCommand = newCommand;

                if (_currentCommand != null)
                {
                    if (Parent != null) // Only hook if attached to logical tree
                    {
                        _currentCommand.CanExecuteChanged += OnCanExecuteChanged;
                    }
                }
                else
                {
                    // Command is removed, restore the snapshot if it exists
                    if (_preCommandIsEnabled.HasValue)
                    {
                        IsEnabled = _preCommandIsEnabled.Value;
                        _preCommandIsEnabled = null;
                    }
                    else
                    {
                        ClearValue(IsEnabledProperty);
                    }
                }
            }
            if (_currentCommand != null)
            {
                UpdateCanExecute();
            }
        }
        else if (dp == CommandParameterProperty)
        {
            UpdateCanExecute();
        }
        else if (dp == IsEnabledProperty && _currentCommand == null)
        {
            // If IsEnabled changes while NO command is active, update our future snapshot potential
            _preCommandIsEnabled = null;
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        if (Command != null)
        {
            bool canExecute = Command.CanExecute(CommandParameter);

            // If the user explicitly disabled the button BEFORE applying the command, it should stay disabled.
            // WPF coerces IsEnabled. We emulate this by checking the base snapshot.
            bool baseEnabled = _preCommandIsEnabled ?? true;

            IsEnabled = baseEnabled && canExecute;
        }
    }

    protected virtual void OnClick()
    {
        // Don't act clickable if disabled
        if (!IsEnabled)
            return;

        RaiseEvent(new RoutedEventArgs(ClickEvent, this));

        if (Command != null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEnabled) return;
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
        if (!IsEnabled) return;
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
        if (!IsEnabled) return;
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
        if (!IsEnabled) return;
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
