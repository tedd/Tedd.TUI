using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiSlider : TuiComponentBase
{
    private class ListeningSlider : Slider
    {
        private readonly TuiSlider _owner;
        public ListeningSlider(TuiSlider owner) => _owner = owner;
        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == ValueProperty)
            {
                _owner.OnInternalValueChanged(Value);
            }
        }
    }

    private ListeningSlider _slider;
    public override UIElement Element => _slider;

    public TuiSlider()
    {
        _slider = new ListeningSlider(this);
    }

    [Parameter] public int Minimum { get; set; } = 0;
    [Parameter] public int Maximum { get; set; } = 10;
    [Parameter] public int Value { get; set; } = 0;
    [Parameter] public EventCallback<int> ValueChanged { get; set; }

    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;
    [Parameter] public int SmallChange { get; set; } = 1;
    [Parameter] public int LargeChange { get; set; } = 5;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _slider.Minimum = Minimum;
        _slider.Maximum = Maximum;

        if (_slider.Value != Value)
        {
            _slider.Value = Value;
        }

        _slider.Orientation = Orientation;
        _slider.SmallChange = SmallChange;
        _slider.LargeChange = LargeChange;
    }

    private void OnInternalValueChanged(int newValue)
    {
        if (Value != newValue)
        {
            Value = newValue;
            InvokeAsync(async () => await ValueChanged.InvokeAsync(newValue));
        }
    }
}
