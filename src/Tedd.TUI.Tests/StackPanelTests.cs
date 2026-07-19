using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;
using System;

namespace Tedd.TUI.Tests;

public class StackPanelTests
{
    /// <summary>
    /// Records the most recent constraint passed to <see cref="MeasureOverride"/> so
    /// tests can assert which constraint a parent panel forwarded to its children.
    /// </summary>
    private sealed class MeasureSensor : UIElement
    {
        public Size LastMeasureConstraint { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastMeasureConstraint = availableSize;
            return new Size(3, 3);
        }
    }

    [Fact]
    public void MouseClick_VerticallyNestedButtons_HitsOnlyTarget()
    {
        var first = new Button { Content = "First", Width = 10 };
        var second = new Button { Content = "Second", Width = 10 };
        var panel = new StackPanel();
        panel.AddChild(new TextBlock { Text = "Toolbar" });
        panel.AddChild(first);
        panel.AddChild(new TextBlock { Text = "spacer" });
        panel.AddChild(second);
        panel.AddChild(new TextBlock { Text = "footer" });
        var host = new ControlTestHost(new Border { Child = panel }, 18, 14);
        var firstClicks = 0;
        var secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        host.Click(first, 2, 1);
        host.Click(panel, 2, 4);
        host.Click(second, 2, 1);

        Assert.Equal(1, firstClicks);
        Assert.Equal(1, secondClicks);
        Assert.True(second.IsFocused);
    }

    [Fact]
    public void TestVerticalStackPanel()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        var t1 = new TextBlock { Text = "Hello" };
        var t2 = new TextBlock { Text = "World" };

        stack.AddChild(t1);
        stack.AddChild(t2);

        // Measure
        stack.Measure(new Size(100, 100));

        // "Hello" = 5x1, "World" = 5x1. Stack should be 5x2.
        Assert.Equal(5, stack.DesiredSize.Width);
        Assert.Equal(2, stack.DesiredSize.Height);

        // Arrange
        stack.Arrange(new Rect(0, 0, 100, 100));

        // Render
        var buffer = new VirtualBuffer(10, 5);
        stack.Render(buffer, 0, 0);

        Assert.Equal('H', buffer.GetPixel(0, 0).Character);
        Assert.Equal('W', buffer.GetPixel(0, 1).Character);
    }

    [Fact]
    public void Vertical_Measures_Children_With_Infinite_Height()
    {
        // WPF contract: StackPanel passes PositiveInfinity along the stack axis so
        // children report their natural size; Stretch on the stack axis is a no-op.
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        var sensor = new MeasureSensor();
        stack.AddChild(sensor);

        stack.Measure(new Size(40, 25));

        Assert.Equal(40, sensor.LastMeasureConstraint.Width);
        Assert.Equal(int.MaxValue, sensor.LastMeasureConstraint.Height);
    }

    [Fact]
    public void Horizontal_Measures_Children_With_Infinite_Width()
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        var sensor = new MeasureSensor();
        stack.AddChild(sensor);

        stack.Measure(new Size(40, 25));

        Assert.Equal(int.MaxValue, sensor.LastMeasureConstraint.Width);
        Assert.Equal(25, sensor.LastMeasureConstraint.Height);
    }

    [Fact]
    public void Border_Inside_Vertical_Stack_Collapses_To_Content()
    {
        // Regression: previously StackPanel forwarded the parent's bounded height,
        // which let Border.MeasureOverride claim the full available height via
        // min(available, content+2). With WPF-faithful semantics the Border should
        // only desire its content size (+ border thickness), not the full window.
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        var border = new Border { BoxStyle = BoxStyle.Single };
        border.Child = new TextBlock { Text = "Hi" }; // 2x1
        stack.AddChild(border);

        stack.Measure(new Size(40, 25));

        // Content (2x1) + border (2x2) = 4x3, never the panel-supplied 25 rows.
        Assert.Equal(3, border.DesiredSize.Height);
        Assert.Equal(3, stack.DesiredSize.Height);
    }

    [Fact]
    public void DockPanel_With_Top_Menu_And_Border_Renders_Bottom_Border()
    {
        // End-to-end repro of the original MainPage bug: a 1-row "menu" docked to
        // the top with a Border filling the rest must render its bottom border row
        // inside the buffer.
        const int W = 20;
        const int H = 6;

        var root = new DockPanel { LastChildFill = true };

        var menu = new TextBlock { Text = "Menu", Background = ConsoleColor.Gray };
        DockPanel.SetDock(menu, Dock.Top);
        root.AddChild(menu);

        var border = new Border { BoxStyle = BoxStyle.Double, BorderColor = ConsoleColor.White };
        border.Child = new TextBlock { Text = "X" };
        root.AddChild(border);

        root.Measure(new Size(W, H));
        root.Arrange(new Rect(0, 0, W, H));

        // Border arranged below the menu, occupying the remaining H-1 rows.
        Assert.Equal(0, border.RenderSize.X);
        Assert.Equal(1, border.RenderSize.Y);
        Assert.Equal(W, border.RenderSize.Width);
        Assert.Equal(H - 1, border.RenderSize.Height);

        var buffer = new VirtualBuffer(W, H);
        root.Render(buffer, 0, 0);

        // Bottom border row (last row of the buffer) must contain the double-line
        // box-drawing corners. Before the fix, this row was clipped because the
        // Border was arranged at full window height starting at y=1.
        var chars = BoxDrawingChars.Get(BoxStyle.Double);
        Assert.Equal(chars.BottomLeft, buffer.GetPixel(0, H - 1).Character);
        Assert.Equal(chars.BottomRight, buffer.GetPixel(W - 1, H - 1).Character);
        Assert.Equal(chars.Horizontal, buffer.GetPixel(W / 2, H - 1).Character);
    }
}
