using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTabControl : TuiComponentBase
{
    private TabControl _tabControl = new TabControl();
    public override UIElement Element => _tabControl;

    [Parameter] public int SelectedIndex { get; set; }
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _tabControl.SelectionChanged += (s, e) => SelectedIndexChanged.InvokeAsync(_tabControl.SelectedIndex);
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        if (_tabControl.SelectedIndex != SelectedIndex && SelectedIndex >= 0 && SelectedIndex < _tabControl.Items.Count)
            _tabControl.SelectedIndex = SelectedIndex;
    }

    public void AddTab(TabItem item)
    {
        _tabControl.Items.Add(item);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // We cascade 'this' as TuiTabControl specifically so TuiTabItem can find us.
        // We also render ChildContent.
        // Note: TuiComponentBase usually cascades ITuiContainer. 
        // But since TabControl doesn't accept normal children, we don't strictly need ITuiContainer cascade 
        // UNLESS TuiTabItem relies on it. TuiTabItem will look for TuiTabControl.

        builder.OpenComponent<CascadingValue<TuiTabControl>>(0);
        builder.AddAttribute(1, "Value", this);
        builder.AddAttribute(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
