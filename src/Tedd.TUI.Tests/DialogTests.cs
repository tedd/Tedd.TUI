using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class DialogTests
{
    private static TuiWindow CreateHost(int width = 80, int height = 25)
    {
        var host = new TuiWindow();
        host.Measure(new Size(width, height));
        host.Arrange(new Rect(0, 0, width, height));
        return host;
    }

    [Fact]
    public void Defaults()
    {
        var dialog = new Dialog();
        Assert.True(dialog.IsModal);
        Assert.Null(dialog.DialogResult);
    }

    [Fact]
    public void ShowDialog_ResetsResult_AndShows()
    {
        var host = CreateHost();
        var dialog = new Dialog { Width = 20, Height = 5 };
        dialog.ShowDialog(host);

        Assert.Equal(dialog, host.Overlay);
        Assert.True(dialog.Visibility);
        Assert.Null(dialog.DialogResult);
    }

    [Fact]
    public void Close_WithResult_SetsDialogResult()
    {
        var host = CreateHost();
        var dialog = new Dialog { Width = 20, Height = 5 };
        bool closed = false;
        dialog.Closed += (s, e) => closed = true;
        dialog.ShowDialog(host);

        dialog.Close(true);

        Assert.True(closed);
        Assert.True(dialog.DialogResult);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void Close_NoResult_CountsAsCancelled()
    {
        var host = CreateHost();
        var dialog = new Dialog { Width = 20, Height = 5 };
        dialog.ShowDialog(host);

        dialog.Close();

        Assert.False(dialog.DialogResult);
    }

    [Fact]
    public void EscapeKey_ClosesAsCancelled()
    {
        var host = CreateHost();
        var dialog = new Dialog { Width = 20, Height = 5 };
        var button = new Button { Content = "OK" };
        dialog.Content = button;
        dialog.ShowDialog(host);

        // Focus lands on the button; Escape bubbles up to the dialog.
        host.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, button) { Key = ConsoleKey.Escape });

        Assert.False(dialog.Visibility);
        Assert.False(dialog.DialogResult);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void ModalDialog_BlocksInputToContentBelow()
    {
        var host = CreateHost();
        int backgroundClicks = 0;
        var backgroundButton = new Button
        {
            Content = "BG",
            Width = 10,
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        backgroundButton.Click += (s, e) => backgroundClicks++;
        host.Content = backgroundButton;
        host.Measure(new Size(80, 25));
        host.Arrange(new Rect(0, 0, 80, 25));

        var dialog = new Dialog { Width = 20, Height = 5, Left = 40, Top = 10 };
        dialog.ShowDialog(host);

        // Click on the background button (top-left) - must be blocked by the modal dialog.
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 2, GlobalY = 0 });
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 2, GlobalY = 0 });

        Assert.Equal(0, backgroundClicks);

        // After closing, the click goes through.
        dialog.Close();
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 2, GlobalY = 0 });
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 2, GlobalY = 0 });

        Assert.Equal(1, backgroundClicks);
    }

    [Fact]
    public void NonModalDialog_AllowsInputBelow()
    {
        var host = CreateHost();
        int backgroundClicks = 0;
        var backgroundButton = new Button
        {
            Content = "BG",
            Width = 10,
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        backgroundButton.Click += (s, e) => backgroundClicks++;
        host.Content = backgroundButton;
        host.Measure(new Size(80, 25));
        host.Arrange(new Rect(0, 0, 80, 25));

        var dialog = new Dialog { Width = 20, Height = 5, Left = 40, Top = 10, IsModal = false };
        dialog.ShowDialog(host);

        host.ProcessMouse(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 2, GlobalY = 0 });
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 2, GlobalY = 0 });

        Assert.Equal(1, backgroundClicks);
    }

    [Fact]
    public void Dialog_IsMoveable_LikeWindow()
    {
        var host = CreateHost();
        var dialog = new Dialog { Width = 20, Height = 5, Left = 10, Top = 5 };
        dialog.ShowDialog(host);

        host.ProcessMouse(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 15, GlobalY = 5 });
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 25, GlobalY = 9 });
        host.ProcessMouse(new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 25, GlobalY = 9 });

        Assert.Equal(20, dialog.RenderSize.X);
        Assert.Equal(9, dialog.RenderSize.Y);
    }
}

public class MessageDialogTests
{
    private static TuiWindow CreateHost(int width = 80, int height = 25)
    {
        var host = new TuiWindow();
        host.Measure(new Size(width, height));
        host.Arrange(new Rect(0, 0, width, height));
        return host;
    }

    private static Button GetButton(MessageDialog dialog, string label)
    {
        var button = dialog.FindName(label + "Button") as Button;
        Assert.NotNull(button);
        return button;
    }

    [Fact]
    public void Show_BuildsButtonsForButtonSet()
    {
        var host = CreateHost();
        var dialog = MessageDialog.Show(host, "Continue?", "Question", MessageDialogButtons.YesNoCancel);

        Assert.NotNull(dialog.FindName("YesButton"));
        Assert.NotNull(dialog.FindName("NoButton"));
        Assert.NotNull(dialog.FindName("CancelButton"));
        Assert.Null(dialog.FindName("OKButton"));
    }

    [Fact]
    public void OkButton_SetsResultAndAccepts()
    {
        var host = CreateHost();
        MessageDialogResult? callbackResult = null;
        var dialog = MessageDialog.Show(host, "Hello", "Info", MessageDialogButtons.Ok,
            r => callbackResult = r);

        GetButton(dialog, "OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent, dialog));

        Assert.Equal(MessageDialogResult.Ok, dialog.Result);
        Assert.Equal(MessageDialogResult.Ok, callbackResult);
        Assert.True(dialog.DialogResult);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void CancelButton_SetsResultAndCancels()
    {
        var host = CreateHost();
        var dialog = MessageDialog.Show(host, "Sure?", "Confirm", MessageDialogButtons.OkCancel);

        GetButton(dialog, "Cancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent, dialog));

        Assert.Equal(MessageDialogResult.Cancel, dialog.Result);
        Assert.False(dialog.DialogResult);
    }

    [Fact]
    public void NoButton_SetsResultNo()
    {
        var host = CreateHost();
        var dialog = MessageDialog.Show(host, "Sure?", "Confirm", MessageDialogButtons.YesNo);

        GetButton(dialog, "No").RaiseEvent(new RoutedEventArgs(Button.ClickEvent, dialog));

        Assert.Equal(MessageDialogResult.No, dialog.Result);
        Assert.False(dialog.DialogResult);
    }

    [Fact]
    public void Escape_ClosesWithNoneResult()
    {
        var host = CreateHost();
        var dialog = MessageDialog.Show(host, "Hello", "Info", MessageDialogButtons.OkCancel);

        var okButton = GetButton(dialog, "OK");
        host.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, okButton) { Key = ConsoleKey.Escape });

        Assert.Equal(MessageDialogResult.None, dialog.Result);
        Assert.False(dialog.DialogResult);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void Show_FocusesFirstButton()
    {
        var host = CreateHost();
        var dialog = MessageDialog.Show(host, "Hello", "Info", MessageDialogButtons.OkCancel);

        Assert.True(GetButton(dialog, "OK").IsFocused);
    }
}
