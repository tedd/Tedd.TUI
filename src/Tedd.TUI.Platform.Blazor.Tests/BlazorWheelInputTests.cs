using Microsoft.AspNetCore.Components.Web;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

/// <summary>
/// The Blazor host originally forwarded no wheel events at all, so nothing in the browser
/// could be scrolled with the wheel. These cover the delta normalization: browsers report
/// wheel movement in pixels, lines or pages depending on device and engine, and all three
/// have to land on the host-wide convention of ±120 per notch, positive away from the user.
/// </summary>
public class BlazorWheelInputTests
{
    private static (BlazorInputManager Manager, ScrollViewer Scroller) BuildScrollableWindow()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        for (int i = 0; i < 40; i++)
            stack.AddChild(new TextBlock { Text = "line " + i });

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Content = stack
        };

        var window = new TuiWindow { Content = scroller };
        window.Measure(new Size(10, 10));
        window.Arrange(new Rect(0, 0, 10, 10));

        var manager = new BlazorInputManager(window) { CharWidth = 1, CharHeight = 1 };
        return (manager, scroller);
    }

    private static void Wheel(BlazorInputManager manager, double deltaY, long deltaMode)
    {
        manager.QueueWheel(new WheelEventArgs
        {
            OffsetX = 2,
            OffsetY = 2,
            DeltaY = deltaY,
            DeltaMode = deltaMode
        });
        manager.ProcessInput();
    }

    [Fact]
    public void QueueWheel_PixelDelta_ScrollsDownByWheelScrollLines()
    {
        var (manager, scroller) = BuildScrollableWindow();
        Assert.Equal(0, scroller.VerticalOffset);

        // Chrome/Edge report ~100 CSS px for one notch.
        Wheel(manager, deltaY: 100, deltaMode: 0);

        Assert.Equal(ScrollViewer.WheelScrollLines, scroller.VerticalOffset);
    }

    [Fact]
    public void QueueWheel_LineDelta_ScrollsSameAsOneNotch()
    {
        var (manager, scroller) = BuildScrollableWindow();

        // Firefox commonly reports deltaMode=1 with 3 lines per notch.
        Wheel(manager, deltaY: 3, deltaMode: 1);

        Assert.Equal(ScrollViewer.WheelScrollLines, scroller.VerticalOffset);
    }

    [Fact]
    public void QueueWheel_PageDelta_ScrollsSameAsOneNotch()
    {
        var (manager, scroller) = BuildScrollableWindow();

        Wheel(manager, deltaY: 1, deltaMode: 2);

        Assert.Equal(ScrollViewer.WheelScrollLines, scroller.VerticalOffset);
    }

    [Fact]
    public void QueueWheel_NegativeDelta_ScrollsBackUp()
    {
        var (manager, scroller) = BuildScrollableWindow();
        Wheel(manager, deltaY: 300, deltaMode: 0);
        int scrolledDown = scroller.VerticalOffset;
        Assert.True(scrolledDown > 0);

        Wheel(manager, deltaY: -300, deltaMode: 0);

        Assert.Equal(0, scroller.VerticalOffset);
        Assert.NotEqual(scrolledDown, scroller.VerticalOffset);
    }

    [Fact]
    public void QueueWheel_TrackpadFractionsAccumulateIntoAScroll()
    {
        var (manager, scroller) = BuildScrollableWindow();

        // A high-resolution trackpad sends a fraction of a notch per event. Truncating
        // each one to zero notches would make the surface ignore trackpads entirely, so
        // the deltas have to accumulate until they cross a full notch.
        for (int i = 0; i < 10; i++)
            Wheel(manager, deltaY: 10, deltaMode: 0);

        Assert.True(scroller.VerticalOffset > 0);
    }
}
