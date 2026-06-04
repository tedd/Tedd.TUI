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
                _currentCommand = newCommand;
                if (_currentCommand != null)
                {
                    _currentCommand.CanExecuteChanged += OnCanExecuteChanged;
                }
            }
            UpdateCanExecute();
        }
        else if (dp == CommandParameterProperty)
        {
            UpdateCanExecute();
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
            IsEnabled = canExecute;
        }
        else
        {
            // If command is removed, revert to enabled state
            ClearValue(IsEnabledProperty);
        }
    }

    protected virtual void OnClick()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));

        if (Command != null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

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

        if (IsPressed)
        {
            IsPressed = false;

            // In WPF, a release click only triggers if the mouse is still over the button.
            // For now, in TUI simple environment, we will assume it was released over it if IsPressed was true.
            if (ClickMode == ClickMode.Release)
            {
                OnClick();
            }
        }
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
