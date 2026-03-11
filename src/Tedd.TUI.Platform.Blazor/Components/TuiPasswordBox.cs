using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiPasswordBox : TuiComponentBase
{
    private class ListeningPasswordBox : PasswordBox
    {
        private readonly TuiPasswordBox _owner;
        public ListeningPasswordBox(TuiPasswordBox owner) => _owner = owner;

        protected override void OnPropertyChanged(DependencyProperty dp)
        {
            base.OnPropertyChanged(dp);
            if (dp == PasswordProperty)
            {
                _owner.OnInternalPasswordChanged(Password);
            }
        }
    }

    private ListeningPasswordBox _passwordBox;
    public override UIElement Element => _passwordBox;

    public TuiPasswordBox()
    {
        _passwordBox = new ListeningPasswordBox(this);
    }

    [Parameter] public string Password { get; set; } = "";
    [Parameter] public EventCallback<string> PasswordChanged { get; set; }

    [Parameter] public char PasswordChar { get; set; } = '*';
    [Parameter] public int Width { get; set; } = -1;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();

        if (_passwordBox.Width != Width && Width != -1) _passwordBox.Width = Width;

        if (_passwordBox.Password != Password)
        {
            _passwordBox.Password = Password;
        }

        if (_passwordBox.PasswordChar != PasswordChar)
        {
            _passwordBox.PasswordChar = PasswordChar;
        }
    }

    private void OnInternalPasswordChanged(string newValue)
    {
        if (Password != newValue)
        {
            Password = newValue;
            InvokeAsync(async () => await PasswordChanged.InvokeAsync(newValue));
        }
    }
}
