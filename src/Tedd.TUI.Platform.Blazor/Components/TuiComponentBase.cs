using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public abstract class TuiComponentBase : ComponentBase, ITuiContainer, IDisposable
{
    [CascadingParameter] public ITuiContainer? ParentContainer { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public int Width { get; set; } = -1;
    [Parameter] public int Height { get; set; } = -1;
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    [Parameter] public ConsoleColor? Background { get; set; }
    [Parameter] public bool Visible { get; set; } = true;

    public abstract UIElement Element { get; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ApplyProperties();

        if (ParentContainer != null)
        {
            ParentContainer.AddChild(Element);
        }
    }

    protected override void OnParametersSet()
    {
        ApplyProperties();
    }

    protected virtual void ApplyProperties()
    {
        Element.Width = Width;
        Element.Height = Height;
        Element.HorizontalAlignment = HorizontalAlignment;
        Element.VerticalAlignment = VerticalAlignment;
        Element.Background = Background;
        Element.Visibility = Visible;
    }

    public virtual void AddChild(UIElement child)
    {
        throw new NotSupportedException($"{this.GetType().Name} does not support children.");
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        if (ChildContent != null)
        {
            builder.OpenComponent<CascadingValue<ITuiContainer>>(0);
            builder.AddAttribute(1, "Value", this);
            builder.AddAttribute(2, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
    }

    public virtual void Dispose()
    {
    }
}
