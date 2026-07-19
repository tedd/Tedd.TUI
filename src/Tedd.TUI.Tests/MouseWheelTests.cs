using System;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

public class MouseWheelTests
{
    private const int Notch = MouseWheelEventArgs.WheelNotch;

    [Fact]
    public void WheelOverScrollBar_ScrollsBySmallChangeTimesWheelLines()
    {
        var scrollBar = new ScrollBar
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            SmallChange = 1,
            Height = 10,
            Width = 1
        };
        var host = new ControlTestHost(new Border { Child = scrollBar }, 5, 14);
        var pos = scrollBar.PointToScreen(new Point(0, 4));

        // Wheel down (negative delta) scrolls toward Maximum, one notch = 3 small changes.
        var args = host.MouseWheel(pos.X, pos.Y, -Notch);
        Assert.True(args.Handled);
        Assert.Equal(53, scrollBar.Value);

        host.MouseWheel(pos.X, pos.Y, Notch);
        Assert.Equal(50, scrollBar.Value);

        // Value stays clamped at the range edges.
        scrollBar.Value = 99;
        host.MouseWheel(pos.X, pos.Y, -Notch);
        Assert.Equal(100, scrollBar.Value);
        host.MouseWheel(pos.X, pos.Y, -Notch);
        Assert.Equal(100, scrollBar.Value);
    }

    [Fact]
    public void WheelOverScrollViewer_ScrollsContentAndScrollBarAlike()
    {
        var stack = new StackPanel();
        for (int i = 0; i < 30; i++)
            stack.AddChild(new TextBlock { Text = $"Line {i}" });
        var sv = new ScrollViewer { Content = stack };
        var host = new ControlTestHost(sv, 20, 8);

        Assert.Equal(0, sv.VerticalOffset);

        // Over the content area.
        host.MouseWheel(5, 4, -Notch);
        Assert.Equal(3, sv.VerticalOffset);

        host.MouseWheel(5, 4, -Notch);
        Assert.Equal(6, sv.VerticalOffset);

        host.MouseWheel(5, 4, Notch);
        Assert.Equal(3, sv.VerticalOffset);

        // Over the vertical scrollbar column (last column) the bar handles the wheel
        // itself; the viewer's offset follows its value.
        host.MouseWheel(19, 4, -Notch);
        Assert.Equal(6, sv.VerticalOffset);
    }

    [Fact]
    public void Wheel_PartialDeltas_AccumulateIntoWholeNotches()
    {
        var stack = new StackPanel();
        for (int i = 0; i < 30; i++)
            stack.AddChild(new TextBlock { Text = $"Line {i}" });
        var sv = new ScrollViewer { Content = stack };
        var host = new ControlTestHost(sv, 20, 8);

        // Trackpads report fractions of a notch; nothing moves until a full notch
        // has accumulated, but the partial events are still consumed (handled).
        var first = host.MouseWheel(5, 4, -40);
        Assert.True(first.Handled);
        Assert.Equal(0, sv.VerticalOffset);

        host.MouseWheel(5, 4, -40);
        Assert.Equal(0, sv.VerticalOffset);

        host.MouseWheel(5, 4, -40);
        Assert.Equal(3, sv.VerticalOffset);
    }

    [Fact]
    public void Wheel_NestedViewerWithoutOverflow_BubblesToOuterViewer()
    {
        var innerStack = new StackPanel();
        innerStack.AddChild(new TextBlock { Text = "inner 1" });
        innerStack.AddChild(new TextBlock { Text = "inner 2" });
        var inner = new ScrollViewer { Content = innerStack, Height = 4 };

        var outerStack = new StackPanel();
        outerStack.AddChild(new TextBlock { Text = "Top" });
        outerStack.AddChild(inner);
        for (int i = 0; i < 20; i++)
            outerStack.AddChild(new TextBlock { Text = $"Row {i}" });
        var outer = new ScrollViewer { Content = outerStack };
        var host = new ControlTestHost(outer, 20, 8);

        // The inner viewer has nothing to scroll, so the wheel bubbles to the outer one.
        host.MouseWheel(3, 2, -Notch);
        Assert.Equal(0, inner.VerticalOffset);
        Assert.Equal(3, outer.VerticalOffset);
    }

    [Fact]
    public void Wheel_NestedViewerWithOverflow_IsHandledByInnerViewer()
    {
        var innerStack = new StackPanel();
        for (int i = 0; i < 10; i++)
            innerStack.AddChild(new TextBlock { Text = $"inner {i}" });
        var inner = new ScrollViewer { Content = innerStack, Height = 4 };

        var outerStack = new StackPanel();
        outerStack.AddChild(new TextBlock { Text = "Top" });
        outerStack.AddChild(inner);
        for (int i = 0; i < 20; i++)
            outerStack.AddChild(new TextBlock { Text = $"Row {i}" });
        var outer = new ScrollViewer { Content = outerStack };
        var host = new ControlTestHost(outer, 20, 8);

        host.MouseWheel(3, 2, -Notch);
        Assert.Equal(3, inner.VerticalOffset);
        Assert.Equal(0, outer.VerticalOffset);
    }

    [Fact]
    public void WheelOverListBoxItems_ScrollsWithoutChangingSelection()
    {
        var listBox = new ListBox { Width = 10, Height = 4 };
        for (int i = 0; i < 10; i++)
            listBox.Items.Add($"Item{i}");
        listBox.SelectedIndex = 0;
        // Border frame + 4 content rows so the ListBox is arranged at exactly Height=4
        // (a taller host would stretch it and show more items than the scroll math uses).
        var host = new ControlTestHost(new Border { Child = listBox }, 14, 6);

        var text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Item0", text);
        Assert.DoesNotContain("Item4", text);

        var pos = listBox.PointToScreen(new Point(2, 1));
        host.MouseWheel(pos.X, pos.Y, -Notch);

        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.DoesNotContain("Item0", text);
        Assert.Contains("Item3", text);
        Assert.Contains("Item6", text);
        Assert.Equal(0, listBox.SelectedIndex);

        host.MouseWheel(pos.X, pos.Y, Notch);
        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Item0", text);
        Assert.DoesNotContain("Item4", text);
    }

    [Fact]
    public void Wheel_HorizontalOnlyViewer_ScrollsHorizontally()
    {
        var wide = new TextBlock { Text = new string('x', 30) + "END" };
        var sv = new ScrollViewer
        {
            Content = wide,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        var host = new ControlTestHost(sv, 20, 4);

        Assert.Equal(0, sv.HorizontalOffset);
        host.MouseWheel(5, 1, -Notch);
        Assert.Equal(3, sv.HorizontalOffset);
        host.MouseWheel(5, 1, Notch);
        Assert.Equal(0, sv.HorizontalOffset);
    }
}
