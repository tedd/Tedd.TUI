using System;

namespace Tedd.TUI;

/// <summary>
/// Which buttons a <see cref="MessageDialog"/> offers.
/// </summary>
public enum MessageDialogButtons
{
    Ok,
    OkCancel,
    YesNo,
    YesNoCancel
}

/// <summary>
/// The button a <see cref="MessageDialog"/> was dismissed with.
/// <see cref="None"/> when closed via Escape or the title-bar close button.
/// </summary>
public enum MessageDialogResult
{
    None,
    Ok,
    Cancel,
    Yes,
    No
}

/// <summary>
/// A simple modal message box: a text message plus a standard button row
/// (OK, OK/Cancel, Yes/No or Yes/No/Cancel). The pressed button is exposed via
/// <see cref="Result"/> after <see cref="Window.Closed"/> fires.
/// </summary>
public class MessageDialog : Dialog
{
    /// <summary>
    /// Message text shown in the dialog body (word-wrapped).
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(MessageDialog), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Which buttons are offered. Default is <see cref="MessageDialogButtons.Ok"/>.
    /// </summary>
    public MessageDialogButtons Buttons { get; set; } = MessageDialogButtons.Ok;

    /// <summary>
    /// The button used to dismiss the dialog, or <see cref="MessageDialogResult.None"/>
    /// while open or when dismissed without choosing a button.
    /// </summary>
    public MessageDialogResult Result { get; private set; } = MessageDialogResult.None;

    public MessageDialog()
    {
        CanResize = false;
    }

    /// <summary>
    /// Creates and shows a message dialog on <paramref name="host"/>.
    /// <paramref name="onClosed"/> (optional) receives the <see cref="Result"/>
    /// when the dialog is dismissed.
    /// </summary>
    public static MessageDialog Show(TuiWindow host, string text, string title = "",
        MessageDialogButtons buttons = MessageDialogButtons.Ok,
        Action<MessageDialogResult>? onClosed = null)
    {
        var dialog = new MessageDialog
        {
            Text = text,
            Title = title,
            Buttons = buttons
        };
        if (onClosed != null)
        {
            dialog.Closed += (s, e) => onClosed(dialog.Result);
        }
        dialog.ShowDialog(host);
        return dialog;
    }

    /// <summary>
    /// Rebuilds the dialog content from the current <see cref="Text"/> and
    /// <see cref="Buttons"/>. Called automatically when the dialog is shown.
    /// </summary>
    public void BuildContent()
    {
        var message = new TextBlock
        {
            Text = Text ?? string.Empty,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0, 1, 0)
        };
        message.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = this });

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0)
        };

        foreach (var (label, result) in GetButtonDefinitions())
        {
            var button = new Button
            {
                Content = label,
                Name = label + "Button",
                Margin = new Thickness(1, 0, 1, 0)
            };
            var captured = result;
            button.Click += (s, e) =>
            {
                Result = captured;
                Close(captured is MessageDialogResult.Ok or MessageDialogResult.Yes);
            };
            buttonRow.Children.Add(button);
        }

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(message);
        stack.Children.Add(buttonRow);
        Content = stack;
    }

    private (string Label, MessageDialogResult Result)[] GetButtonDefinitions() => Buttons switch
    {
        MessageDialogButtons.OkCancel =>
            [("OK", MessageDialogResult.Ok), ("Cancel", MessageDialogResult.Cancel)],
        MessageDialogButtons.YesNo =>
            [("Yes", MessageDialogResult.Yes), ("No", MessageDialogResult.No)],
        MessageDialogButtons.YesNoCancel =>
            [("Yes", MessageDialogResult.Yes), ("No", MessageDialogResult.No), ("Cancel", MessageDialogResult.Cancel)],
        _ => [("OK", MessageDialogResult.Ok)]
    };

    public override void Show()
    {
        Result = MessageDialogResult.None;
        BuildContent();
        base.Show();
    }
}
