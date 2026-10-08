using Microsoft.AspNetCore.Components;
using Tedd.TUI.Controls;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiLabel : TuiComponentBase
{
    private Label _label = new Label();
    public override UIElement Element => _label;

    [Parameter] public string Text { get; set; } = "";
    [Parameter] public ConsoleColor Foreground { get; set; } = ConsoleColor.White;

    [Parameter] public UIElement? Target { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();

        // Ensure TextBlock inside Label has proper foreground inheritance,
        // or just apply it directly to Label which inherits Control,
        // which might not inherit Foreground to TextBlock unless explicitly set.
        // For backwards compatibility with the previous Blazor component,
        // we can set Content to Text and map Foreground to the Label.
        _label.Content = Text;
        _label.Foreground = Foreground;

        if (Target != null)
        {
            _label.Target = Target;
        }
    }
}
