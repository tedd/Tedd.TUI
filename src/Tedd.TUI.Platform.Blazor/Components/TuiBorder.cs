using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiBorder : TuiComponentBase
{
    private Border _border = new Border();
    public override UIElement Element => _border;

    [Parameter] public ConsoleColor BorderColor { get; set; } = ConsoleColor.White;
    [Parameter] public BoxStyle BoxStyle { get; set; } = BoxStyle.Single;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _border.BorderColor = BorderColor;
        _border.BoxStyle = BoxStyle;
    }

    public override void AddChild(UIElement child)
    {
        if (_border.Child != null)
        {
            throw new InvalidOperationException("Border can only have one child.");
        }
        _border.Child = child;
    }
}
