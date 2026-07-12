using Microsoft.AspNetCore.Components.Web;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

public class BlazorInputManagerTests
{
    [Fact]
    public void QueueMouse_RoutesPreviewFocusPressAndClickThroughWindow()
    {
        var button = new Button { Content = "OK" };
        var window = new TuiWindow { Content = button };
        window.Measure(new Size(4, 3));
        window.Arrange(new Rect(0, 0, 4, 3));

        var manager = new BlazorInputManager(window)
        {
            CharWidth = 1,
            CharHeight = 1
        };
        var previews = 0;
        var clicks = 0;
        window.AddHandler(
            UIElement.PreviewMouseDownEvent,
            new RoutedEventHandler((_, _) => previews++));
        button.Click += (_, _) => clicks++;

        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = 1,
            OffsetY = 1,
            Button = 0
        }, "mousedown");
        manager.ProcessInput();

        Assert.Equal(1, previews);
        Assert.True(button.IsFocused);
        Assert.True(button.IsPressed);
        Assert.Equal(0, clicks);

        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = 1,
            OffsetY = 1,
            Button = 0
        }, "mouseup");
        manager.ProcessInput();

        Assert.False(button.IsPressed);
        Assert.Equal(1, clicks);
    }
}

