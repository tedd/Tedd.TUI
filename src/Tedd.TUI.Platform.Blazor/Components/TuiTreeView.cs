using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTreeView : TuiComponentBase
{
    private TreeView _treeView = new TreeView();
    public override UIElement Element => _treeView;

    [Parameter] public EventCallback<object?> SelectedItemChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _treeView.SelectionChanged += (s, e) => SelectedItemChanged.InvokeAsync(_treeView.SelectedItem);
    }

    public override void AddChild(UIElement child)
    {
        if (child is TreeViewItem tvi)
            _treeView.Items.Add(tvi);
    }
}
