using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiCheckBox : TuiComponentBase
{
    private class ListeningCheckBox : CheckBox
    {
        private readonly TuiCheckBox _owner;
        public ListeningCheckBox(TuiCheckBox owner) => _owner = owner;
        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == IsCheckedProperty)
            {
                _owner.OnInternalCheckChanged(IsChecked);
            }
        }
    }

    private ListeningCheckBox _checkBox;
    public override UIElement Element => _checkBox;

    public TuiCheckBox()
    {
        _checkBox = new ListeningCheckBox(this);
    }

    [Parameter] public string Content { get; set; } = "";
    [Parameter] public bool IsChecked { get; set; }
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _checkBox.Content = Content;
        if (_checkBox.IsChecked != IsChecked)
        {
            _checkBox.IsChecked = IsChecked;
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
