using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiPasswordBox : TuiComponentBase
{
    private PasswordBox _passwordBox;
    public override UIElement Element => _passwordBox;

    public TuiPasswordBox()
    {
        _passwordBox = new PasswordBox();
        _passwordBox.PasswordChanged += OnInternalPasswordChanged;
    }

    [Parameter] public string Password { get; set; } = "";
    [Parameter] public EventCallback<string> PasswordChanged { get; set; }

    [Parameter] public char PasswordChar { get; set; } = '*';

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        if (_passwordBox.Password != Password)
        {
            _passwordBox.Password = Password;
        }
        _passwordBox.PasswordChar = PasswordChar;
    }

    private void OnInternalPasswordChanged(object? sender, RoutedEventArgs e)
    {
        if (Password != _passwordBox.Password)
        {
            Password = _passwordBox.Password;
            InvokeAsync(async () => await PasswordChanged.InvokeAsync(Password));
        }
    }
}
