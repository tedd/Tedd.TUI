using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiScrollViewer : TuiComponentBase
{
    private ScrollViewer _scrollViewer = new ScrollViewer();
    public override UIElement Element => _scrollViewer;

    [Parameter] public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; } = ScrollBarVisibility.Disabled;
    [Parameter] public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Visible;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _scrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
        _scrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
    }

    public override void AddChild(UIElement child)
    {
        // ScrollViewer has a single Content property
        if (_scrollViewer.Content != null)
        {
            throw new InvalidOperationException("ScrollViewer can only have one child.");
        }
        _scrollViewer.Content = child;
    }
}
