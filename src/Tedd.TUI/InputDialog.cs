using System;

namespace Tedd.TUI;

/// <summary>
/// A modal prompt dialog: a message, a single-line text input and OK/Cancel
/// buttons. Enter in the input accepts; the entered text is in <see cref="Input"/>
/// when <see cref="Dialog.DialogResult"/> is true.
/// </summary>
public class InputDialog : Dialog
{
    /// <summary>Prompt message shown above the input box.</summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(string), typeof(InputDialog), string.Empty);

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// The entered text. Set before showing to pre-fill the input box.
    /// </summary>
    public string Input { get; set; } = string.Empty;

    protected TextBox InputBox { get; private set; } = null!;

    public InputDialog()
    {
        Width = 40;
        CanResize = false;
    }

    /// <summary>
    /// Creates and shows an input dialog on <paramref name="host"/>.
    /// <paramref name="onClosed"/> receives the entered text, or null when cancelled.
    /// </summary>
    public static InputDialog Show(TuiWindow host, string message, string title = "",
        string initialInput = "", Action<string?>? onClosed = null)
    {
        var dialog = new InputDialog
        {
            Message = message,
            Title = title,
            Input = initialInput
        };
        if (onClosed != null)
        {
            dialog.Closed += (s, e) => onClosed(dialog.DialogResult == true ? dialog.Input : null);
        }
        dialog.ShowDialog(host);
        return dialog;
    }

    /// <summary>Rebuilds the dialog UI. Called automatically by <see cref="Show()"/>.</summary>
    protected virtual void BuildContent()
    {
        var message = new TextBlock
        {
            Text = Message ?? string.Empty,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0, 1, 0)
        };
        message.SetBinding(TextBlock.TextProperty, new Binding("Message") { Source = this });

        InputBox = new TextBox { Name = "InputBox", Text = Input, Margin = new Thickness(1, 0, 1, 0) };

        var okButton = new Button { Name = "OkButton", Content = "OK", Margin = new Thickness(1, 0, 1, 0) };
        okButton.Click += (s, e) => Accept();
        var cancelButton = new Button { Name = "CancelButton", Content = "Cancel", Margin = new Thickness(1, 0, 1, 0) };
        cancelButton.Click += (s, e) => Close(false);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0)
        };
        buttonRow.Children.Add(okButton);
        buttonRow.Children.Add(cancelButton);

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(message);
        stack.Children.Add(InputBox);
        stack.Children.Add(buttonRow);
        Content = stack;
    }

    /// <summary>Accepts the dialog with the current input text.</summary>
    public void Accept()
    {
        Input = InputBox.Text ?? string.Empty;
        Close(true);
    }

    public override void Show()
    {
        BuildContent();
        base.Show();
        // Focus goes straight to the input box.
        (GetRoot() as TuiWindow)?.SetFocus(InputBox);
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == ConsoleKey.Enter && ReferenceEquals(e.Source, InputBox))
        {
            e.Handled = true;
            Accept();
            return;
        }
        base.OnKeyDown(e);
    }
}
