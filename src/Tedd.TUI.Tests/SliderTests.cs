using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class SliderTests
{
    [Fact]
    public void Slider_Value_ShouldClampToMinMax()
    {
        var slider = new Slider { Minimum = 0, Maximum = 10 };
        slider.Value = 15;

        Assert.Equal(10, slider.Value);

        slider.Value = -5;
        Assert.Equal(0, slider.Value);
    }

    [Fact]
    public void Slider_KeyDown_ShouldChangeValue_Horizontal()
    {
        var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5, Orientation = Orientation.Horizontal };

        var args = new KeyEventArgs(UIElement.KeyDownEvent, slider)
        {
            Key = ConsoleKey.RightArrow
        };
        slider.OnKeyDown(args);

        Assert.Equal(6, slider.Value);
        Assert.True(args.Handled);

        args = new KeyEventArgs(UIElement.KeyDownEvent, slider)
        {
            Key = ConsoleKey.LeftArrow
        };
        slider.OnKeyDown(args);

        Assert.Equal(5, slider.Value);
    }

    [Fact]
    public void Slider_MouseDown_ShouldChangeValue()
    {
        var slider = new Slider { Minimum = 0, Maximum = 10, Value = 0, Width = 11, Height = 1 };
        slider.Measure(new Size(100, 100));
        slider.Arrange(new Rect(0, 0, 11, 1));

        // Width = 11. Range = 10.
        // val = (clickX * 10) / 10 = clickX.

        var args = new MouseEventArgs(UIElement.MouseDownEvent, slider)
        {
            X = 5, // Click at 5 (center)
            Y = 0
        };
        slider.OnMouseDown(args);

        Assert.Equal(5, slider.Value);
        Assert.True(args.Handled);
    }

    [Fact]
    public void MouseClick_NestedSliders_UsesLocalCoordinatesWithoutChangingSibling()
    {
        var horizontal = new Slider
        {
            Width = 11,
            Height = 1,
            Minimum = 0,
            Maximum = 10
        };
        var vertical = new Slider
        {
            Width = 1,
            Height = 6,
            Minimum = 0,
            Maximum = 10,
            Orientation = Orientation.Vertical
        };
        var horizontalRow = new StackPanel { Orientation = Orientation.Horizontal };
        horizontalRow.AddChild(horizontal);
        horizontalRow.AddChild(new TextBlock { Text = " surface" });
        var verticalRow = new StackPanel { Orientation = Orientation.Horizontal };
        verticalRow.AddChild(vertical);
        verticalRow.AddChild(new TextBlock { Text = " surface" });
        var controls = new StackPanel();
        controls.AddChild(new TextBlock { Text = "horizontal" });
        controls.AddChild(horizontalRow);
        controls.AddChild(new TextBlock { Text = "-----------" });
        controls.AddChild(new TextBlock { Text = "vertical" });
        controls.AddChild(verticalRow);
        var surface = new Border { Child = controls, BoxStyle = BoxStyle.Double, Padding = new Thickness(0) };
        var host = new ControlTestHost(surface, 15, 12);

        var horizontalClick = host.Click(horizontal, 7, 0);

        Assert.True(horizontalClick.Down.Handled);
        Assert.True(horizontal.IsFocused);
        Assert.False(vertical.IsFocused);
        Assert.Equal(7, horizontal.Value);
        Assert.Equal(0, vertical.Value);

        host.Click(controls.GetVisualChild(2), 5, 0);
        Assert.Equal(7, horizontal.Value);
        Assert.Equal(0, vertical.Value);

        var verticalClick = host.Click(vertical, 0, 4);

        Assert.True(verticalClick.Down.Handled);
        Assert.False(horizontal.IsFocused);
        Assert.True(vertical.IsFocused);
        Assert.Equal(7, horizontal.Value);
        Assert.Equal(8, vertical.Value);
    }
}
