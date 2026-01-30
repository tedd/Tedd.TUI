using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiLabel : TuiComponentBase
{
    private TextBlock _textBlock = new TextBlock();
    public override UIElement Element => _textBlock;

    [Parameter] public string Text { get; set; } = "";
    [Parameter] public ConsoleColor Foreground { get; set; } = ConsoleColor.White;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _textBlock.Text = Text;
        _textBlock.Foreground = Foreground;
    }
}
