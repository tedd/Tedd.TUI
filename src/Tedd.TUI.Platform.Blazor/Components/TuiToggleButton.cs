using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiToggleButton : TuiComponentBase
{
    private class ListeningToggleButton : ToggleButton
    {
        private readonly TuiToggleButton _owner;
        public ListeningToggleButton(TuiToggleButton owner) => _owner = owner;
        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == IsCheckedProperty)
            {
                _owner.OnInternalCheckChanged(IsChecked);
            }
        }
    }

    private ListeningToggleButton _toggleButton;
    public override UIElement Element => _toggleButton;

    public TuiToggleButton()
    {
        _toggleButton = new ListeningToggleButton(this);
    }

    [Parameter] public string Content { get; set; } = "";
    [Parameter] public bool? IsChecked { get; set; }
    [Parameter] public bool IsThreeState { get; set; }
    [Parameter] public EventCallback<bool?> IsCheckedChanged { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _toggleButton.Click += (s, e) =>
        {
            InvokeAsync(async () => await OnClick.InvokeAsync());
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _toggleButton.Content = Content;
        _toggleButton.IsThreeState = IsThreeState;
        if (_toggleButton.IsChecked != IsChecked)
        {
            _toggleButton.IsChecked = IsChecked;
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
