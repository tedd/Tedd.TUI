using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class DialogBoxTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var db = new DialogBox();
        Assert.Equal(string.Empty, db.Title);
        Assert.True(db.IsModal);
        Assert.Equal(BoxStyle.Double, db.BoxStyle);
        Assert.Null(db.Content);
    }

    [Fact]
    public void Show_AddsToWindowOverlay()
    {
        var window = new TuiWindow();
        var dialog = new DialogBox { Title = "Test Dialog" };

        // Usage: Attach first then Show (to layout)
        window.PushOverlay(dialog);
        Assert.Equal(dialog, window.Overlay);

        // Give window a size
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        dialog.Show();
        // Show() sets Visibility=true and centers it.
        Assert.Equal(Visibility.Visible, dialog.Visibility);

        Assert.True(dialog.RenderSize.Width > 0);
        Assert.True(dialog.RenderSize.Height > 0);
    }

    [Fact]
    public void Hide_RemovesFromWindowOverlay()
    {
        var window = new TuiWindow();
        var dialog = new DialogBox();

        window.PushOverlay(dialog);
        Assert.Equal(dialog, window.Overlay);

        dialog.Hide();

        Assert.Null(window.Overlay);
        Assert.Equal(Visibility.Collapsed, dialog.Visibility);
    }

    [Fact]
    public void Stacking_Overlays()
    {
        var window = new TuiWindow();
        var dialog1 = new DialogBox { Title = "D1" };
        var dialog2 = new DialogBox { Title = "D2" };

        window.PushOverlay(dialog1);
        Assert.Equal(dialog1, window.Overlay);

        window.PushOverlay(dialog2);
        Assert.Equal(dialog2, window.Overlay);

        // Hide top dialog (D2)
        dialog2.Hide();
        Assert.Equal(dialog1, window.Overlay); // D1 should remain

        // Hide D1
        dialog1.Hide();
        Assert.Null(window.Overlay);
    }

    [Fact]
    public void MouseClick_DialogOverlay_BlocksBackgroundAndInvokesOnlyChosenButton()
    {
        var backgroundButton = new Button { Content = "Background", Width = 12 };
        var backgroundClicks = 0;
        backgroundButton.Click += (_, _) => backgroundClicks++;
        var background = new StackPanel();
        background.AddChild(new TextBlock { Text = "main surface" });
        background.AddChild(backgroundButton);
        var host = new ControlTestHost(new Border { Child = background }, 30, 15);

        var ok = new Button { Content = "OK", Width = 10 };
        var cancel = new Button { Content = "Cancel", Width = 10 };
        var okClicks = 0;
        var cancelClicks = 0;
        ok.Click += (_, _) => okClicks++;
        cancel.Click += (_, _) => cancelClicks++;
        var dialogContent = new StackPanel();
        dialogContent.AddChild(ok);
        dialogContent.AddChild(new TextBlock { Text = "dialog surface" });
        dialogContent.AddChild(cancel);
        // 11 rows leave a 7-row content viewport after the border and the default
        // 1-char padding: two 3-row buttons plus the text line fit exactly.
        var dialog = new DialogBox
        {
            Title = "Confirm",
            Width = 18,
            Height = 11,
            Content = dialogContent
        };
        host.Window.PushOverlay(dialog);
        dialog.Show();

        host.Click(backgroundButton, 2, 1);
        Assert.Equal(0, backgroundClicks);
        Assert.Equal(0, okClicks);
        Assert.Equal(0, cancelClicks);

        var okClick = host.Click(ok, 2, 1);

        Assert.True(okClick.Down.Handled);
        Assert.Equal(1, okClicks);
        Assert.Equal(0, cancelClicks);
        Assert.Equal(0, backgroundClicks);
        Assert.True(ok.IsFocused);
        Assert.Same(dialog, host.Window.Overlay);

        var cancelClick = host.Click(cancel, 2, 1);

        Assert.True(cancelClick.Down.Handled);
        Assert.Equal(1, okClicks);
        Assert.Equal(1, cancelClicks);
        Assert.Equal(0, backgroundClicks);
        Assert.False(ok.IsFocused);
        Assert.True(cancel.IsFocused);
        Assert.Same(dialog, host.Window.Overlay);
    }
}
