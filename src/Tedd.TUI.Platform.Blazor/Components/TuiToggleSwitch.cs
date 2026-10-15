using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using Tedd.TUI.Controls;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiToggleSwitch : TuiComponentBase
{
    private class ListeningToggleSwitch : ToggleSwitch
    {
        private readonly TuiToggleSwitch _owner;
        public ListeningToggleSwitch(TuiToggleSwitch owner) => _owner = owner;
        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == IsCheckedProperty)
            {
                _owner.OnInternalCheckChanged(IsChecked);
            }
        }
    }

    private ListeningToggleSwitch _toggleSwitch;
    public override UIElement Element => _toggleSwitch;

    public TuiToggleSwitch()
    {
        _toggleSwitch = new ListeningToggleSwitch(this);
    }

    [Parameter] public object? OnContent { get; set; }
    [Parameter] public object? OffContent { get; set; }
    [Parameter] public bool? IsChecked { get; set; }
    [Parameter] public EventCallback<bool?> IsCheckedChanged { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        if (_toggleSwitch.OnContent != OnContent) _toggleSwitch.OnContent = OnContent;
        if (_toggleSwitch.OffContent != OffContent) _toggleSwitch.OffContent = OffContent;
        if (_toggleSwitch.IsChecked != IsChecked)
        {
            _toggleSwitch.IsChecked = IsChecked;
        }
    }

    private void OnInternalCheckChanged(bool? newValue)
    {
        if (IsChecked != newValue)
        {
            IsChecked = newValue;
            InvokeAsync(async () => await IsCheckedChanged.InvokeAsync(newValue));
        }
    }
}
