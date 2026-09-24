using System;

namespace Tedd.TUI.Controls;

public class Label : ContentControl
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register("Target", typeof(UIElement), typeof(Label), null);

    public UIElement Target
    {
        get => (UIElement)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }
}
