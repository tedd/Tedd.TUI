using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiMenuItem : TuiComponentBase
{
    private MenuItem _menuItem = new MenuItem();
    public override UIElement Element => _menuItem;

    [Parameter] public string Text { get; set; } = "";
    [Parameter] public EventCallback OnClick { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        if (_menuItem.Header is TextBlock tb)
        {
            tb.Text = Text;
        }
        else if (_menuItem.Header == null && !string.IsNullOrEmpty(Text))
        {
            _menuItem.Header = new TextBlock 
            { 
                Text = Text,
                Foreground = ConsoleColor.Black // Default for menu items usually
            };
        }

        if (OnClick.HasDelegate)
        {
            _menuItem.Command = () => 
            {
                 InvokeAsync(OnClick.InvokeAsync);
            };
        }
    }

    public override void AddChild(UIElement child)
    {
        _menuItem.Items.Add(child);
    }
}
