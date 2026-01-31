using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTabItem : ComponentBase, ITuiContainer, IDisposable
{
    [CascadingParameter] public TuiTabControl? Parent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Header { get; set; } = "";

    private Tedd.TUI.TabItem _tabItem = new Tedd.TUI.TabItem();

    protected override void OnInitialized()
    {
        _tabItem.Header = Header;
        if (Parent == null)
        {
            throw new InvalidOperationException("TuiTabItem must be placed inside a TuiTabControl.");
        }
        Parent.AddTab(_tabItem);
    }

    protected override void OnParametersSet()
    {
        if (_tabItem.Header.ToString() != Header)
        {
            _tabItem.Header = Header;
        }
    }

    public void AddChild(UIElement child)
    {
        // TabItem content is a single object (usually UIElement)
        _tabItem.Content = child;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent != null)
        {
            builder.OpenComponent<CascadingValue<ITuiContainer>>(0);
            builder.AddAttribute(1, "Value", this);
            builder.AddAttribute(2, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
    }

    public void Dispose()
    {
        // Removal logic if needed
    }
}
