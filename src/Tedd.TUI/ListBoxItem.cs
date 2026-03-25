using System;

namespace Tedd.TUI;

public class ListBoxItem : ContentControl
{
    public static readonly RoutedEvent SelectedEvent =
        RoutedEvent.Register(nameof(Selected), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

    public event RoutedEventHandler Selected
    {
        add => AddHandler(SelectedEvent, value);
        remove => RemoveHandler(SelectedEvent, value);
    }

    public static readonly RoutedEvent UnselectedEvent =
        RoutedEvent.Register(nameof(Unselected), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

    public event RoutedEventHandler Unselected
    {
        add => AddHandler(UnselectedEvent, value);
        remove => RemoveHandler(UnselectedEvent, value);
    }

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(ListBoxItem), false);

    public bool IsSelected
    {
        get => (bool)(GetValue(IsSelectedProperty) ?? false);
        set => SetValue(IsSelectedProperty, value);
    }

    public ListBoxItem()
    {
        Focusable = true;

        Template = new ControlTemplate(parent =>
        {
            var cp = new ContentPresenter
            {
                TemplatedParent = parent
            };
            cp.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = RelativeSource.TemplatedParent });
            cp.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("ContentTemplate") { RelativeSource = RelativeSource.TemplatedParent });

            return cp;
        });

        var selectedTrigger = new Trigger { Property = IsSelectedProperty, Value = true };
        selectedTrigger.Setters.Add(new Setter { Property = BackgroundProperty, Value = ConsoleColor.White });
        selectedTrigger.Setters.Add(new Setter { Property = ForegroundProperty, Value = ConsoleColor.Black });

        Template.Triggers.Add(selectedTrigger);
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
            if (!IsSelected)
            {
                IsSelected = true;
            }
            Focus();
            e.Handled = true;
        }
    }
}
