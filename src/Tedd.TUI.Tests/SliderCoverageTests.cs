using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class SliderCoverageTests
{
    [Theory]
    [InlineData(Orientation.Horizontal, 10, 1)]
    [InlineData(Orientation.Vertical, 1, 10)]
    public void MeasureOverride_ReturnsDefaultSize(Orientation orientation, int expectedW, int expectedH)
    {
        var slider = new Slider { Orientation = orientation };
        slider.Measure(new Size(100, 100));
        Assert.Equal(new Size(expectedW, expectedH), slider.DesiredSize);
    }

    [Theory]
    [InlineData(Orientation.Horizontal, 11, 1, 5, 5, '-')]
    [InlineData(Orientation.Vertical, 1, 11, 5, 5, '|')]
    public void Render_DrawsTrackAndThumb(Orientation orientation, int w, int h, int value, int thumbOffset, char trackChar)
    {
        var slider = new Slider
        {
            Orientation = orientation,
            Minimum = 0,
            Maximum = 10,
            Value = value,
            Width = w,
            Height = h,
            TrackColor = ConsoleColor.DarkGray,
            ThumbColor = ConsoleColor.White
        };
        slider.Measure(new Size(100, 100));
        slider.Arrange(new Rect(0, 0, w, h));

        var buffer = new VirtualBuffer(w, h);
        slider.Render(buffer, 0, 0);

        // Check Track at origin
        Assert.Equal(trackChar, buffer.GetPixel(0, 0).Character);
        Assert.Equal(ConsoleColor.DarkGray, buffer.GetPixel(0, 0).Foreground);

        // Check Thumb position
        int thumbX = orientation == Orientation.Horizontal ? thumbOffset : 0;
        int thumbY = orientation == Orientation.Vertical ? thumbOffset : 0;

        Assert.Equal('O', buffer.GetPixel(thumbX, thumbY).Character);
        Assert.Equal(ConsoleColor.White, buffer.GetPixel(thumbX, thumbY).Foreground);
    }

    [Fact]
    public void Render_ZeroRange_HandlesDivisionByZero()
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 0, // Range 0
            Value = 0,
            Width = 10
        };
        slider.Measure(new Size(10, 1));
        slider.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        // Should not throw
        slider.Render(buffer, 0, 0);

        // Thumb should be at 0
        Assert.Equal('O', buffer.GetPixel(0, 0).Character);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow, 6)]
    [InlineData(ConsoleKey.UpArrow, 4)]
    public void OnKeyDown_Vertical_AdjustsValue(ConsoleKey key, int expectedValue)
    {
        var slider = new Slider
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 10,
            Value = 5,
            SmallChange = 1
        };

        var args = new KeyEventArgs(UIElement.KeyDownEvent, slider) { Key = key };
        // Use RaiseEvent to simulate properly
        slider.RaiseEvent(args);

        Assert.Equal(expectedValue, slider.Value);
        Assert.True(args.Handled);
    }

    [Theory]
    [InlineData(0, 5, 5)]   // Click middle -> 5
    [InlineData(0, 0, 0)]   // Click top -> 0
    [InlineData(0, 10, 10)] // Click bottom -> 10
    public void OnMouseDown_Vertical_SetsValue(int x, int y, int expectedValue)
    {
        var slider = new Slider
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 10,
            Value = 0,
            Width = 1,
            Height = 11 // Height 11 means indices 0..10 map exactly to 0..10 value
        };
        slider.Measure(new Size(1, 11));
        slider.Arrange(new Rect(0, 0, 1, 11));

        var args = new MouseEventArgs(UIElement.MouseDownEvent, slider) { X = x, Y = y };
        // Use RaiseEvent
        // Note: RaiseEvent uses GlobalX/GlobalY and converts to Local X/Y via PointFromScreen.
        // Since we didn't attach slider to a window or set RenderSize via Arrange, PointFromScreen might rely on RenderSize.
        // RenderSize is set by Arrange.
        // PointFromScreen usually walks up parent chain. No parent -> assumes screen coords = local coords.
        // So we set GlobalX/Y to X/Y for standalone element test.
        args.GlobalX = x;
        args.GlobalY = y;

        slider.RaiseEvent(args);

        Assert.Equal(expectedValue, slider.Value);
        Assert.True(args.Handled);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-10, 0)] // Clamped min
    [InlineData(100, 10)] // Clamped max
    public void Value_Clamping(int input, int expected)
    {
        var slider = new Slider { Minimum = 0, Maximum = 10 };
        slider.Value = input;
        Assert.Equal(expected, slider.Value);
    }
}
