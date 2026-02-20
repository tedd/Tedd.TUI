using System;
using Xunit;
using Tedd.TUI;

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
        Assert.True(dialog.Visibility);

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
        Assert.False(dialog.Visibility);
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
}
