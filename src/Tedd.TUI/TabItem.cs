using System;

namespace Tedd.TUI;

public class TabItem : HeaderedContentControl
{
    public TabItem()
    {
        // Default style or settings if needed
    }

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(TabItem), false);

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == IsSelectedProperty)
        {
            // Notify parent TabControl?
            // Selector usually manages this property on the container.
            // But if user sets IsSelected = true, TabControl should update.
            // We can raise an event or the parent can bind/listen.
            // For this implementation, we rely on Selector.SelectedIndex driving the state.
            // But strict WPF allows setting IsSelected on TabItem.
            var parent = Parent as Selector;
            if (parent != null)
            {
                bool val = (bool)GetValue(IsSelectedProperty);
                if (val)
                {
                    parent.SelectedItem = this;
                }
                // If false, and this was selected, parent logic should handle unselect?
            }
        }
    }
}
