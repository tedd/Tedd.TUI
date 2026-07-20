using System;

namespace Tedd.TUI.Controls;

public class PasswordBox : Control
{
    internal TextBox? _internalTextBox;

    public PasswordBox()
    {
        Focusable = true;

        Template = new ControlTemplate(parent =>
        {
            var pb = (PasswordBox)parent;

            var tb = new TextBox { IsPassword = true };
            tb.TemplatedParent = pb;

            // Forward appearance properties
            tb.SetBinding(UIElement.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            tb.SetBinding(UIElement.ForegroundProperty, new Binding("Foreground") { RelativeSource = RelativeSource.TemplatedParent });

            // Forward Password -> TextBox.Text
            // Since TUI bindings are OneWay by default and don't robustly support TwoWay back to DependencyProperties without INotifyPropertyChanged,
            // we will manually sync keystrokes in OnKeyDown.
            tb.SetBinding(TextBox.TextProperty, new Binding("Password") { RelativeSource = RelativeSource.TemplatedParent });

            tb.SetBinding(TextBox.PasswordCharProperty, new Binding("PasswordChar") { RelativeSource = RelativeSource.TemplatedParent });

            pb._internalTextBox = tb;
            return tb;
        });
    }

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register("Password", typeof(string), typeof(PasswordBox), string.Empty, bindsTwoWayByDefault: true);

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public static readonly DependencyProperty PasswordCharProperty =
        DependencyProperty.Register("PasswordChar", typeof(char), typeof(PasswordBox), '*');

    public char PasswordChar
    {
        get => (char)GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == IsFocusedProperty)
        {
            if (_internalTextBox != null)
            {
                _internalTextBox.IsFocused = IsFocused;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (_internalTextBox != null)
        {
            string oldText = _internalTextBox.Text ?? "";

            _internalTextBox.IsFocused = true;
            _internalTextBox.OnKeyDown(e);
            _internalTextBox.IsFocused = IsFocused;

            if (_internalTextBox.Text != oldText)
            {
                Password = _internalTextBox.Text ?? "";
            }
        }
        else
        {
            base.OnKeyDown(e);
        }
    }

    // Bug: Clicking inside PasswordBox causes redundant focus calls and handler invocation.
    // Root cause: PasswordBox.OnMouseDown unconditionally invokes base, focuses parent, and manually delegates to child TextBox.
    // Fix: Return early if the mouse down event has already been handled (e.g. by the internal TextBox during bubbling).
    // Regression: Covered by FocusOverlayTests & general focus routing
    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Handled) return;
        base.OnMouseDown(e);
        Focus();
        if (_internalTextBox != null)
        {
            _internalTextBox.IsFocused = true;
            _internalTextBox.OnMouseDown(e);
            _internalTextBox.IsFocused = IsFocused;
        }
        // Let e.Handled state persist from base and internal textbox logic
    }
}
