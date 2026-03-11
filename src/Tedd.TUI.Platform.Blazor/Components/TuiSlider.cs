using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiSlider : TuiComponentBase
{
    private Slider _slider = new Slider();
    public override UIElement Element => _slider;

    [Parameter] public int Value { get; set; }
    [Parameter] public EventCallback<int> ValueChanged { get; set; }

    [Parameter] public int Minimum { get; set; } = 0;
    [Parameter] public int Maximum { get; set; } = 10;
    [Parameter] public int SmallChange { get; set; } = 1;
    [Parameter] public int LargeChange { get; set; } = 5;
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;
    [Parameter] public int Width { get; set; } = -1;
    [Parameter] public int Height { get; set; } = -1;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _slider.ValueChanged += (s, e) =>
        {
            if (Value != _slider.Value)
            {
                Value = _slider.Value;
                InvokeAsync(async () => await ValueChanged.InvokeAsync(Value));
            }
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();

        if (_slider.Width != Width && Width != -1) _slider.Width = Width;
        if (_slider.Height != Height && Height != -1) _slider.Height = Height;

        _slider.Minimum = Minimum;
        _slider.Maximum = Maximum;

        if (_slider.Value != Value) _slider.Value = Value;
        _slider.SmallChange = SmallChange;
        _slider.LargeChange = LargeChange;
        _slider.Orientation = Orientation;
    }
}
