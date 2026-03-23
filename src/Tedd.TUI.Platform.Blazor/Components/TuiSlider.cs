using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiSlider : TuiComponentBase
{
    private Slider _slider;
    public override UIElement Element => _slider;

    public TuiSlider()
    {
        _slider = new Slider();
        _slider.ValueChanged += OnInternalValueChanged;
    }

    [Parameter] public int Minimum { get; set; } = 0;
    [Parameter] public int Maximum { get; set; } = 10;
    [Parameter] public int Value { get; set; } = 0;
    [Parameter] public EventCallback<int> ValueChanged { get; set; }
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();

        // Always apply range before value to avoid clamping issues
        _slider.Minimum = Minimum;
        _slider.Maximum = Maximum;

        if (_slider.Value != Value)
        {
            _slider.Value = Value;
        }

        _slider.Orientation = Orientation;
    }

    private void OnInternalValueChanged(object? sender, RoutedEventArgs e)
    {
        if (Value != _slider.Value)
        {
            Value = _slider.Value;
            InvokeAsync(async () => await ValueChanged.InvokeAsync(Value));
        }
    }
}
