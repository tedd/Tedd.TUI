using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTextBox : TuiComponentBase
{
    private class ListeningTextBox : TextBox
    {
        private readonly TuiTextBox _owner;
        public ListeningTextBox(TuiTextBox owner) => _owner = owner;
        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == TextProperty)
            {
                _owner.OnInternalTextChanged(Text);
            }
        }
    }

    private ListeningTextBox _textBox;
    public override UIElement Element => _textBox;

    public TuiTextBox()
    {
        _textBox = new ListeningTextBox(this);
    }

    [Parameter] public string Text { get; set; } = "";
    [Parameter] public EventCallback<string> TextChanged { get; set; }
    [Parameter] public bool IsPassword { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        // Avoid overwriting if same to prevent cursor jump or loops
        if (_textBox.Text != Text)
        {
            _textBox.Text = Text;
        }
        _textBox.IsPassword = IsPassword;
    }

    private void OnInternalTextChanged(string newValue)
    {
        if (Text != newValue)
        {
            Text = newValue;
            InvokeAsync(async () => await TextChanged.InvokeAsync(newValue));
        }
    }
}
