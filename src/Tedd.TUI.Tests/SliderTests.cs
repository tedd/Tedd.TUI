using System;
using Xunit;
using Tedd.TUI;

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
}
