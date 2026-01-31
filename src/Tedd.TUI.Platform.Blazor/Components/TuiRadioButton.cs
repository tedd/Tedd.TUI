using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiRadioButton : TuiComponentBase
{
    private class ListeningRadioButton : RadioButton
    {
        private readonly TuiRadioButton _owner;
        public ListeningRadioButton(TuiRadioButton owner) => _owner = owner;
        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == IsCheckedProperty)
            {
                _owner.OnInternalCheckChanged(IsChecked);
            }
        }
    }

    private ListeningRadioButton _radioButton;
    public override UIElement Element => _radioButton;

    public TuiRadioButton()
    {
        _radioButton = new ListeningRadioButton(this);
    }

    [Parameter] public string Content { get; set; } = "";
    [Parameter] public string GroupName { get; set; } = "";
    [Parameter] public bool IsChecked { get; set; }
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _radioButton.Content = Content;
        _radioButton.GroupName = GroupName;
        if (_radioButton.IsChecked != IsChecked)
        {
            _radioButton.IsChecked = IsChecked;
        }
    }

    private void OnInternalCheckChanged(bool newValue)
    {
        if (IsChecked != newValue)
        {
            IsChecked = newValue;
            InvokeAsync(async () => await IsCheckedChanged.InvokeAsync(newValue));
        }
    }
}
