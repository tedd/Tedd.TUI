using System;

namespace Tedd.TUI.Controls;

/// <summary>
/// Represents the text label for a control and provides support for access keys.
/// </summary>
public class Label : ContentControl
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register("Target", typeof(UIElement), typeof(Label), null);

    /// <summary>
    /// Gets or sets the UIElement that receives focus when the user presses the label's access key.
    /// </summary>
    public UIElement? Target
    {
        get => (UIElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public Label()
    {
        // By default, labels should not receive focus
        Focusable = false;

        // Ensure default styling matches typical WPF/XAML behavior
        // e.g. using a TextBlock to render content.
    }
}
