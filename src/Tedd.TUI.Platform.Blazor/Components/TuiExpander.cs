using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiExpander : TuiComponentBase
{
    private Expander _expander = new Expander();
    public override UIElement Element => _expander;

    [Parameter] public string? Header { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public EventCallback<bool> IsExpandedChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _expander.Expanded += (s, e) => IsExpandedChanged.InvokeAsync(true);
        _expander.Collapsed += (s, e) => IsExpandedChanged.InvokeAsync(false);
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _expander.Header = Header;
        _expander.IsExpanded = IsExpanded;
    }

    public override void AddChild(UIElement child)
    {
        _expander.Content = child;
    }
}
