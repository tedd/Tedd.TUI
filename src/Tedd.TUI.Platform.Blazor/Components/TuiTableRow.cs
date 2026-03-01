using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTableRow : TuiComponentBase
{
    private TableRow _row = new TableRow();
    public override UIElement Element => _row;

    // TuiTableRow is a TuiComponentBase.
    // Any child added to it via ChildContent (TuiLabel, TuiButton) will call AddChild.

    public override void AddChild(UIElement child)
    {
        _row.AddCell(child);
    }
}
