using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiMenuBar : TuiComponentBase
{
    private MenuBar _menuBar = new MenuBar();
    public override UIElement Element => _menuBar;

    public override void AddChild(UIElement child)
    {
        _menuBar.AddChild(child);
    }
}
