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

    public static readonly RoutedEvent SelectedEvent = RoutedEvent.Register(
        "Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

    public static readonly RoutedEvent UnselectedEvent = RoutedEvent.Register(
        "Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

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

    public ListBoxItem()
    {
        Focusable = true;

        Template = new ControlTemplate(parent =>
        {
            var cp = new ContentPresenter();
            cp.TemplatedParent = parent;

            var contentBinding = new Binding("Content");
            contentBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentProperty, contentBinding);

            var templateBinding = new Binding("ContentTemplate");
            templateBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentTemplateProperty, templateBinding);

            return cp;
        });

    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == IsSelectedProperty || dp == IsFocusedProperty)
        {
            UpdateVisualState();
        }

        if (dp == IsSelectedProperty)
        {
            bool isSelected = (bool)GetValue(IsSelectedProperty);
            if (isSelected)
            {
                RaiseEvent(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                RaiseEvent(new RoutedEventArgs(UnselectedEvent, this));
            }
        }
    }

    internal void UpdateVisualState()
    {
        // Try to get colors from Parent ListBox if we are inside one.
        // If not, use defaults.
        var parentListBox = Parent as ListBox;

        // Sometimes parent is not set yet, so search up the visual tree
        if (parentListBox == null)
        {
            var curr = Parent;
            while(curr != null && !(curr is ListBox))
            {
                curr = curr.Parent;
            }
            parentListBox = curr as ListBox;
        }

        ConsoleColor selectedBg = parentListBox?.SelectionBackground ?? ConsoleColor.White;
        ConsoleColor selectedFg = parentListBox?.SelectionForeground ?? ConsoleColor.Black;

        ConsoleColor focusedSelectedBg = parentListBox?.FocusedSelectionBackground ?? ConsoleColor.Blue;
        ConsoleColor focusedSelectedFg = parentListBox?.FocusedSelectionForeground ?? ConsoleColor.White;

        ConsoleColor normalBg = parentListBox?.Background ?? ConsoleColor.Black;
        ConsoleColor normalFg = parentListBox?.Foreground ?? ConsoleColor.Gray;

        bool showSelection = parentListBox?.ShowSelection ?? true;

        if (IsSelected)
        {
            if (IsFocused || (parentListBox != null && parentListBox.IsFocused))
            {
                // Note: WPF ListBoxItem is focused when selected usually.
                // Or if ListBox is focused.
                Background = focusedSelectedBg;
                Foreground = focusedSelectedFg;
            }
            else if (showSelection)
            {
                Background = selectedBg;
                Foreground = selectedFg;
            }
            else
            {
                Background = normalBg;
                Foreground = normalFg;
            }
        }
        else
        {
            Background = normalBg;
            Foreground = normalFg;
        }
    }

    public override void OnGotFocus()
    {
        base.OnGotFocus();
        if (!IsSelected)
        {
            IsSelected = true;
        }
        UpdateVisualState();
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        UpdateVisualState();
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (!IsSelected)
        {
            IsSelected = true;
        }
        e.Handled = true;
    }
}
