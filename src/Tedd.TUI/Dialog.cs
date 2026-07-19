using System;

namespace Tedd.TUI;

/// <summary>
/// Base class for dialog windows: a <see cref="Window"/> that is modal by default,
/// carries a <see cref="DialogResult"/>, closes on Escape (as cancel) and raises
/// <see cref="Window.Closed"/> when dismissed. Concrete dialogs
/// (<see cref="MessageDialog"/>, <see cref="OpenFileDialog"/>, ...) build their
/// content on top of this.
/// </summary>
public class Dialog : Window, IModalOverlay
{
    /// <summary>
    /// Gets or sets whether the dialog blocks input to elements below it.
    /// Default is true.
    /// </summary>
    public static readonly DependencyProperty IsModalProperty =
        DependencyProperty.Register("IsModal", typeof(bool), typeof(Dialog), true);

    public bool IsModal
    {
        get => (bool)GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
    }

    /// <summary>
    /// The result the dialog was closed with: true = accepted (OK), false =
    /// cancelled (Cancel/Escape/close button), null = still open.
    /// </summary>
    public bool? DialogResult { get; protected set; }

    /// <summary>
    /// Pushes this dialog as an overlay on <paramref name="host"/> and shows it.
    /// The dialog result is reset; subscribe to <see cref="Window.Closed"/> to
    /// observe the outcome.
    /// </summary>
    public void ShowDialog(TuiWindow host)
    {
        DialogResult = null;
        Show(host);
    }

    /// <summary>
    /// Shows the dialog on the host it is already attached to. The dialog result is reset.
    /// </summary>
    public void ShowDialog()
    {
        DialogResult = null;
        Show();
    }

    /// <summary>
    /// Closes the dialog with the given result.
    /// </summary>
    public void Close(bool? dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    /// <summary>
    /// Closes the dialog. When no result has been set (title-bar close button),
    /// the dialog counts as cancelled.
    /// </summary>
    public override void Close()
    {
        DialogResult ??= false;
        base.Close();
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == ConsoleKey.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }
}
