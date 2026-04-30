using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiHost : TuiComponentBase
{
    private UIElement _element = default!;

    [Parameter, EditorRequired] public UIElement Component { get; set; } = default!;

    public override UIElement Element => _element;

    protected override void OnInitialized()
    {
        if (Component is null)
        {
            throw new InvalidOperationException($"{nameof(TuiHost)} requires a non-null {nameof(Component)} parameter.");
        }

        _element = Component;
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (Component is null)
        {
            throw new InvalidOperationException($"{nameof(TuiHost)} requires a non-null {nameof(Component)} parameter.");
        }

        if (_element is null)
        {
            _element = Component;
        }
        else if (!ReferenceEquals(_element, Component))
        {
            throw new InvalidOperationException($"{nameof(TuiHost)} does not support changing the {nameof(Component)} parameter after initialization.");
        }

        base.OnParametersSet();
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
    }
}
