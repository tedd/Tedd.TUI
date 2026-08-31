using Microsoft.AspNetCore.Components.Web;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

public class BlazorInputManagerTests
{
    [Fact]
    public void QueueWheel_CoalescesPendingMovementIntoOneRoutedEvent()
    {
        var window = new TuiWindow { Content = new TextBlock { Text = "content" } };
        window.Measure(new Size(10, 3));
        window.Arrange(new Rect(0, 0, 10, 3));
        var manager = new BlazorInputManager(window) { CharWidth = 1, CharHeight = 1 };
        int events = 0;
        int delta = 0;
        int signals = 0;
        window.AddHandler(UIElement.MouseWheelEvent, new RoutedEventHandler((_, e) =>
        {
            events++;
            delta += ((MouseWheelEventArgs)e).Delta;
        }), handledEventsToo: true);
        manager.InputAvailable += () => signals++;

        manager.QueueWheel(new WheelEventArgs { OffsetX = 1, OffsetY = 1, DeltaY = 100 });
        manager.QueueWheel(new WheelEventArgs { OffsetX = 2, OffsetY = 1, DeltaY = 100 });
        manager.QueueWheel(new WheelEventArgs { OffsetX = 3, OffsetY = 1, DeltaY = -50 });
        manager.ProcessInput();

        Assert.Equal(1, signals);
        Assert.Equal(1, events);
        Assert.Equal(-180, delta);
    }

    [Fact]
    public void QueueWheel_OppositePendingMovementCancelsWithoutRoutingStaleEvents()
    {
        var window = new TuiWindow { Content = new TextBlock { Text = "content" } };
        window.Measure(new Size(10, 3));
        window.Arrange(new Rect(0, 0, 10, 3));
        var manager = new BlazorInputManager(window) { CharWidth = 1, CharHeight = 1 };
        int events = 0;
        window.AddHandler(UIElement.MouseWheelEvent,
            new RoutedEventHandler((_, _) => events++), handledEventsToo: true);

        manager.QueueWheel(new WheelEventArgs { OffsetX = 1, OffsetY = 1, DeltaY = 100 });
        manager.QueueWheel(new WheelEventArgs { OffsetX = 1, OffsetY = 1, DeltaY = -100 });
        manager.ProcessInput();

        Assert.Equal(0, events);
    }

    [Fact]
    public void QueueWheel_DoesNotCoalesceAcrossAnotherInputKind()
    {
        var window = new TuiWindow { Content = new TextBlock { Text = "content" } };
        window.Measure(new Size(10, 3));
        window.Arrange(new Rect(0, 0, 10, 3));
        var manager = new BlazorInputManager(window) { CharWidth = 1, CharHeight = 1 };
        var order = new List<string>();
        window.AddHandler(UIElement.MouseWheelEvent,
            new RoutedEventHandler((_, _) => order.Add("wheel")), handledEventsToo: true);
        window.AddHandler(UIElement.MouseMoveEvent,
            new RoutedEventHandler((_, _) => order.Add("move")), handledEventsToo: true);

        manager.QueueWheel(new WheelEventArgs { OffsetX = 1, OffsetY = 1, DeltaY = 100 });
        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = 1,
            OffsetY = 1
        }, "mousemove");
        manager.QueueWheel(new WheelEventArgs { OffsetX = 1, OffsetY = 1, DeltaY = 100 });
        manager.ProcessInput();

        Assert.Equal(new[] { "wheel", "move", "wheel" }, order);
    }

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

    /// <summary>
    /// A bare move must reach the window: hover state is driven entirely by mouse events, so
    /// nothing that reacts to <see cref="UIElement.IsMouseOver"/> works until one arrives.
    /// </summary>
    [Fact]
    public void QueueMouse_MoveUpdatesHover()
    {
        var button = new Button { Content = "OK" };
        var window = new TuiWindow { Content = button };
        window.Measure(new Size(4, 3));
        window.Arrange(new Rect(0, 0, 4, 3));

        var manager = new BlazorInputManager(window) { CharWidth = 1, CharHeight = 1 };

        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = 1,
            OffsetY = 1
        }, "mousemove");
        manager.ProcessInput();

        Assert.True(button.IsMouseOver);

        // Parking the pointer far outside is how the host reports the pointer leaving the
        // surface; the hit test misses and the highlight has to clear.
        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = -1e6,
            OffsetY = -1e6
        }, "mousemove");
        manager.ProcessInput();

        Assert.False(button.IsMouseOver);
    }

    /// <summary>
    /// The browser reports pixels, and <see cref="ScrollBar"/> maps a drag through the
    /// fractional cell coordinates. Dropping them leaves every event pinned to the centre of
    /// its cell, so a drag can only ever move in whole-cell jumps.
    /// </summary>
    [Fact]
    public void QueueMouse_CarriesSubCellPrecision()
    {
        var window = new TuiWindow { Content = new Button { Content = "OK" } };
        window.Measure(new Size(8, 4));
        window.Arrange(new Rect(0, 0, 8, 4));

        double capturedXF = 0, capturedYF = 0;
        int capturedX = 0, capturedY = 0;
        window.AddHandler(UIElement.MouseMoveEvent, new RoutedEventHandler((_, e) =>
        {
            var me = (Tedd.TUI.MouseEventArgs)e;
            capturedXF = me.GlobalXF;
            capturedYF = me.GlobalYF;
            capturedX = me.GlobalX;
            capturedY = me.GlobalY;
        }), handledEventsToo: true);

        var manager = new BlazorInputManager(window) { CharWidth = 10, CharHeight = 20 };

        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = 25,  // 2.5 cells across
            OffsetY = 30   // 1.5 cells down
        }, "mousemove");
        manager.ProcessInput();

        Assert.Equal(2, capturedX);
        Assert.Equal(1, capturedY);
        Assert.Equal(2.5, capturedXF, 3);
        Assert.Equal(1.5, capturedYF, 3);
    }

    /// <summary>
    /// Modifiers drive the standard list gestures (Shift extends, Control toggles), so a host
    /// that drops them leaves multi-selection unreachable with the mouse.
    /// </summary>
    [Fact]
    public void QueueMouse_CarriesModifiers()
    {
        var window = new TuiWindow { Content = new Button { Content = "OK" } };
        window.Measure(new Size(4, 3));
        window.Arrange(new Rect(0, 0, 4, 3));

        ConsoleModifiers captured = 0;
        window.AddHandler(UIElement.MouseDownEvent, new RoutedEventHandler((_, e) =>
            captured = ((Tedd.TUI.MouseEventArgs)e).Modifiers), handledEventsToo: true);

        var manager = new BlazorInputManager(window) { CharWidth = 1, CharHeight = 1 };

        manager.QueueMouse(new Microsoft.AspNetCore.Components.Web.MouseEventArgs
        {
            OffsetX = 1,
            OffsetY = 1,
            Button = 0,
            ShiftKey = true,
            CtrlKey = true
        }, "mousedown");
        manager.ProcessInput();

        Assert.True(captured.HasFlag(ConsoleModifiers.Shift));
        Assert.True(captured.HasFlag(ConsoleModifiers.Control));
        Assert.False(captured.HasFlag(ConsoleModifiers.Alt));
    }
}
