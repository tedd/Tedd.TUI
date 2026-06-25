using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiButton : TuiComponentBase
{
    private Button _button = new Button();
    public override UIElement Element => _button;

    [Parameter] public string Text { get; set; } = "";
    [Parameter] public BoxStyle BoxStyle { get; set; } = BoxStyle.Single;
    [Parameter] public ButtonShadowStyle ShadowStyle { get; set; } = ButtonShadowStyle.None;
    [Parameter] public ConsoleColor ShadowBackground { get; set; } = ConsoleColor.Black;
    [Parameter] public EventCallback OnClick { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _button.Click += (s, e) =>
        {
            InvokeAsync(async () => await OnClick.InvokeAsync());
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _button.Content = Text;
        _button.BoxStyle = BoxStyle;
        _button.ShadowStyle = ShadowStyle;
        _button.ShadowBackground = ShadowBackground;
    }
}
