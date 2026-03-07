using System;

namespace Tedd.TUI;

public abstract class ToggleButton : ButtonBase
{
    public ToggleButton()
    {
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register("IsChecked", typeof(bool?), typeof(ToggleButton), (bool?)false);

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public static readonly DependencyProperty IsThreeStateProperty =
        DependencyProperty.Register("IsThreeState", typeof(bool), typeof(ToggleButton), false);

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    public static readonly RoutedEvent CheckedEvent =
        RoutedEvent.Register("Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

    public event RoutedEventHandler Checked
    {
        add { AddHandler(CheckedEvent, value); }
        remove { RemoveHandler(CheckedEvent, value); }
    }

    public static readonly RoutedEvent UncheckedEvent =
        RoutedEvent.Register("Unchecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

    public event RoutedEventHandler Unchecked
    {
        add { AddHandler(UncheckedEvent, value); }
        remove { RemoveHandler(UncheckedEvent, value); }
    }

    public static readonly RoutedEvent IndeterminateEvent =
        RoutedEvent.Register("Indeterminate", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

    public event RoutedEventHandler Indeterminate
    {
        add { AddHandler(IndeterminateEvent, value); }
        remove { RemoveHandler(IndeterminateEvent, value); }
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == IsCheckedProperty)
        {
            if (IsChecked == true)
            {
                RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
            }
            else if (IsChecked == false)
            {
                RaiseEvent(new RoutedEventArgs(UncheckedEvent, this));
            }
            else
            {
                RaiseEvent(new RoutedEventArgs(IndeterminateEvent, this));
            }
        }
    }

    protected virtual void OnToggle()
    {
        if (IsChecked == true)
        {
            IsChecked = IsThreeState ? null : (bool?)false;
        }
        else if (IsChecked == false)
        {
            IsChecked = true;
        }
        else
        {
            IsChecked = false;
        }
    }

    protected override void OnClick()
    {
        OnToggle();
        base.OnClick();
    }
}
