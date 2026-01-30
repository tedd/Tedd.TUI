using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiStackPanel : TuiComponentBase
{
    private StackPanel _stackPanel = new StackPanel();
    public override UIElement Element => _stackPanel;

    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _stackPanel.Orientation = Orientation;
    }

    public override void AddChild(UIElement child)
    {
        _stackPanel.AddChild(child);
    }
}
