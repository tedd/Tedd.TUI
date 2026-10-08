using System;

namespace Tedd.TUI.Controls;

public class Label : ContentControl
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register("Target", typeof(UIElement), typeof(Label), null);

    public UIElement? Target
    {
        get => (UIElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public Label()
    {
        Focusable = true;
    }

    public override void OnGotFocus()
    {
        base.OnGotFocus();

        var target = Target;
        if (target != null && target.Focusable && target.IsEnabled && target.Visibility)
        {
            target.Focus();
        }
    }
}
