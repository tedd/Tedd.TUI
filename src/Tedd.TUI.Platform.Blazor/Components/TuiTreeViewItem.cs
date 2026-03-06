using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTreeViewItem : TuiComponentBase
{
    private TreeViewItem _item = new TreeViewItem();
    public override UIElement Element => _item;

    [Parameter] public string? Header { get; set; }
    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public EventCallback<bool> IsSelectedChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _item.Selected += (s, e) => IsSelectedChanged.InvokeAsync(true);
        _item.Unselected += (s, e) => IsSelectedChanged.InvokeAsync(false);
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        if (Header != null) _item.Header = Header;
        _item.IsExpanded = IsExpanded;
        _item.IsSelected = IsSelected;
    }

    public override void AddChild(UIElement child)
    {
        if (child is TreeViewItem tvi)
        {
            _item.Items.Add(tvi);
        }
    }
}
