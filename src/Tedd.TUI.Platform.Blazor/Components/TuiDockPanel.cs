using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiDockPanel : TuiComponentBase
{
    private DockPanel _dockPanel = new DockPanel();
    public override UIElement Element => _dockPanel;

    [Parameter] public bool LastChildFill { get; set; } = true;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _dockPanel.LastChildFill = LastChildFill;
    }

    public override void AddChild(UIElement child)
    {
        _dockPanel.AddChild(child);
    }
}
