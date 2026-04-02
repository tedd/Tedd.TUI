using System;

namespace Tedd.TUI;

public class ListBoxItem : ContentControl
{
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(ListBoxItem), false);

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly RoutedEvent SelectedEvent =
        RoutedEvent.Register("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

    public static readonly RoutedEvent UnselectedEvent =
        RoutedEvent.Register("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

    public event RoutedEventHandler Selected
    {
        add => AddHandler(SelectedEvent, value);
        remove => RemoveHandler(SelectedEvent, value);
    }

    public event RoutedEventHandler Unselected
    {
        add => AddHandler(UnselectedEvent, value);
        remove => RemoveHandler(UnselectedEvent, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == IsSelectedProperty)
        {
            if (IsSelected)
            {
                RaiseEvent(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                RaiseEvent(new RoutedEventArgs(UnselectedEvent, this));
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!e.Handled)
        {
            IsSelected = true;
            e.Handled = true;
        }
    }
}
