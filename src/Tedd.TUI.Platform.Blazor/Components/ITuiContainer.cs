using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public interface ITuiContainer
{
    void AddChild(UIElement child);
}
