using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiScrollBar : TuiComponentBase
{
    private ScrollBar _scrollBar = new ScrollBar();
    public override UIElement Element => _scrollBar;

    [Parameter] public int Value { get; set; }
    [Parameter] public EventCallback<int> ValueChanged { get; set; }

    [Parameter] public int Minimum { get; set; } = 0;
    [Parameter] public int Maximum { get; set; } = 100;
    [Parameter] public int SmallChange { get; set; } = 1;
    [Parameter] public int LargeChange { get; set; } = 10;
    [Parameter] public int ViewportSize { get; set; } = 1;
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _scrollBar.ValueChanged += (s, e) =>
        {
            if (Value != _scrollBar.Value)
            {
                Value = _scrollBar.Value;
                ValueChanged.InvokeAsync(Value);
            }
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        if (_scrollBar.Value != Value) _scrollBar.Value = Value;
        _scrollBar.Minimum = Minimum;
        _scrollBar.Maximum = Maximum;
        _scrollBar.SmallChange = SmallChange;
        _scrollBar.LargeChange = LargeChange;
        _scrollBar.ViewportSize = ViewportSize;
        _scrollBar.Orientation = Orientation;
    }
}
