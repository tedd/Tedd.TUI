using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiHost : TuiComponentBase
{
    [Parameter] public UIElement Component { get; set; } = default!;

    public override UIElement Element => Component;

    protected override void ApplyProperties()
    {
        if (Component != null)
        {
            base.ApplyProperties();
        }
    }
}
