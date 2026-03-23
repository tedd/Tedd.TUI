using System;

namespace Tedd.TUI;

public class PasswordBox : Control
{
    internal TextBox _internalTextBox => _textBox;
    private TextBox _textBox;

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(nameof(Password), typeof(string), typeof(PasswordBox), string.Empty);

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public static readonly DependencyProperty PasswordCharProperty =
        DependencyProperty.Register(nameof(PasswordChar), typeof(char), typeof(PasswordBox), '*');

    public char PasswordChar
    {
        get => (char)GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    public static readonly RoutedEvent PasswordChangedEvent =
        RoutedEvent.Register("PasswordChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PasswordBox));

    public event RoutedEventHandler PasswordChanged
    {
        add => AddHandler(PasswordChangedEvent, value);
        remove => RemoveHandler(PasswordChangedEvent, value);
    }

    public PasswordBox()
    {
        Focusable = true;

        _textBox = new TextBox
        {
            IsPassword = true,
            TemplatedParent = this
        };

        // Bind PasswordChar to TextBox.PasswordChar
        _textBox.SetBinding(TextBox.PasswordCharProperty, new Binding(nameof(PasswordChar)) { Source = this });
        _textBox.SetBinding(Control.BackgroundProperty, new Binding(nameof(Background)) { Source = this });
        _textBox.SetBinding(Control.ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });

        Template = new ControlTemplate((_) => _textBox);
    }

    protected override void OnPropertyChanged(DependencyProperty property)
    {
        base.OnPropertyChanged(property);

        if (property == PasswordProperty)
        {
            if (_textBox.Text != Password)
            {
                _textBox.Text = Password;
            }
            RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, this));
        }
        else if (property == UIElement.IsFocusedProperty)
        {
            // Sync focus state to inner TextBox so it renders the cursor
            _textBox.SetValue(UIElement.IsFocusedProperty, IsFocused);
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        // Forward KeyDown to TextBox
        _textBox.OnKeyDown(e);

        // Sync Password from TextBox Text after key down
        if (Password != _textBox.Text)
        {
            Password = _textBox.Text;
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // Temporarily force focus on the inner TextBox so OnMouseDown calculates cursor position correctly
        bool wasFocused = _textBox.IsFocused;
        _textBox.SetValue(UIElement.IsFocusedProperty, true);

        _textBox.OnMouseDown(e);

        // Restore real focus state (which should now be true anyway because we called Focus() on ourselves,
        // and our OnPropertyChanged synced it down, but just to be safe if that didn't happen synchronously)
        _textBox.SetValue(UIElement.IsFocusedProperty, IsFocused);

        e.Handled = true;
    }
}
