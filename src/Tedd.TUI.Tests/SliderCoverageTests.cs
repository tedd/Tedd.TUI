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

    [Theory]
    [InlineData(Orientation.Horizontal, 10, 1, 0, 0)]
    [InlineData(Orientation.Vertical, 1, 10, 0, 0)]
    public void Render_ZeroRange_HandlesDivisionByZero(Orientation orientation, int width, int height, int expectedThumbX, int expectedThumbY)
    {
        var slider = new Slider
        {
            Orientation = orientation,
            Minimum = 0,
            Maximum = 0, // Range 0
            Value = 0,
            Width = width,
            Height = height
        };
        slider.Measure(new Size(width, height));
        slider.Arrange(new Rect(0, 0, width, height));

        var buffer = new VirtualBuffer(width, height);
        // Should not throw
        slider.Render(buffer, 0, 0);

        // Thumb should be at expected coordinates
        Assert.Equal('O', buffer.GetPixel(expectedThumbX, expectedThumbY).Character);
    }

    [Theory]
    [InlineData(Orientation.Vertical, ConsoleKey.DownArrow, 6)]
    [InlineData(Orientation.Vertical, ConsoleKey.UpArrow, 4)]
    [InlineData(Orientation.Horizontal, ConsoleKey.RightArrow, 6)]
    [InlineData(Orientation.Horizontal, ConsoleKey.LeftArrow, 4)]
    public void OnKeyDown_AdjustsValue(Orientation orientation, ConsoleKey key, int expectedValue)
    {
        var slider = new Slider
        {
            Orientation = orientation,
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
    [InlineData(Orientation.Vertical, 0, 5, 5)]   // Click middle -> 5
    [InlineData(Orientation.Vertical, 0, 0, 0)]   // Click top -> 0
    [InlineData(Orientation.Vertical, 0, 10, 10)] // Click bottom -> 10
    [InlineData(Orientation.Horizontal, 5, 0, 5)] // Click middle -> 5
    [InlineData(Orientation.Horizontal, 0, 0, 0)] // Click left -> 0
    [InlineData(Orientation.Horizontal, 10, 0, 10)] // Click right -> 10
    public void OnMouseDown_SetsValue(Orientation orientation, int x, int y, int expectedValue)
    {
        var w = orientation == Orientation.Horizontal ? 11 : 1;
        var h = orientation == Orientation.Vertical ? 11 : 1;
        var slider = new Slider
        {
            Orientation = orientation,
            Minimum = 0,
            Maximum = 10,
            Value = 0,
            Width = w,
            Height = h
        };
        slider.Measure(new Size(w, h));
        slider.Arrange(new Rect(0, 0, w, h));

        var args = new MouseEventArgs(UIElement.MouseDownEvent, slider) { X = x, Y = y };
        args.GlobalX = x;
        args.GlobalY = y;

        slider.RaiseEvent(args);

        Assert.Equal(expectedValue, slider.Value);
        Assert.True(args.Handled);
    }

    [Theory]
    [InlineData(Orientation.Horizontal, 5, 0)]
    [InlineData(Orientation.Vertical, 0, 5)]
    public void OnMouseDown_ZeroRange_HandlesDivisionByZero(Orientation orientation, int clickX, int clickY)
    {
        var w = orientation == Orientation.Horizontal ? 10 : 1;
        var h = orientation == Orientation.Vertical ? 10 : 1;
        var slider = new Slider
        {
            Orientation = orientation,
            Minimum = 0,
            Maximum = 0,
            Width = w,
            Height = h
        };
        slider.Measure(new Size(w, h));
        slider.Arrange(new Rect(0, 0, w, h));

        var args = new MouseEventArgs(UIElement.MouseDownEvent, slider) { X = clickX, Y = clickY };
        args.GlobalX = clickX;
        args.GlobalY = clickY;

        slider.RaiseEvent(args);
        Assert.Equal(0, slider.Value);
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

    [Theory]
    [InlineData(5, 6, 0, 0)]   // val < 0 rendered (forced via Minimum change)
    [InlineData(10, 0, 8, 9)]  // val > range rendered (forced via Maximum change)
    public void Render_ClampsValue_ExceedsRange(int initialValue, int minimumSet, int maximumSet, int expectedThumbX)
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 10,
            Width = 10,
            Height = 1
        };

        slider.Value = initialValue;
        if (minimumSet != 0) slider.Minimum = minimumSet;
        if (maximumSet != 10) slider.Maximum = maximumSet;

        slider.Measure(new Size(10, 1));
        slider.Arrange(new Rect(0, 0, 10, 1));
        var buffer = new VirtualBuffer(10, 1);
        slider.Render(buffer, 0, 0);

        Assert.Equal('O', buffer.GetPixel(expectedThumbX, 0).Character);
    }

    [Theory]
    [InlineData(10, ConsoleColor.Green)]
    [InlineData(20, ConsoleColor.Red)]
    public void Properties_GetSet(int largeChange, ConsoleColor focusedThumbColor)
    {
        var slider = new Slider();

        slider.LargeChange = largeChange;
        Assert.Equal(largeChange, slider.LargeChange);

        slider.FocusedThumbColor = focusedThumbColor;
        Assert.Equal(focusedThumbColor, slider.FocusedThumbColor);
    }

    [Theory]
    [InlineData(ConsoleColor.Green, ConsoleColor.White)]
    public void Render_FocusedThumbColor(ConsoleColor focusedColor, ConsoleColor thumbColor)
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 10,
            Value = 5,
            Width = 11,
            Height = 1,
            FocusedThumbColor = focusedColor,
            ThumbColor = thumbColor
        };
        slider.Measure(new Size(11, 1));
        slider.Arrange(new Rect(0, 0, 11, 1));

        var window = new TuiWindow();
        window.Content = slider;
        slider.Focus();

        var buffer = new VirtualBuffer(11, 1);
        slider.Render(buffer, 0, 0);

        Assert.Equal(focusedColor, buffer.GetPixel(5, 0).Foreground);
    }

    [Theory]
    [InlineData(5, 8)]
    public void EventHandlers_AddRemove(int valueSet1, int valueSet2)
    {
        var slider = new Slider();
        bool eventRaised = false;

        RoutedEventHandler handler = (s, e) => { eventRaised = true; };
        slider.ValueChanged += handler;
        slider.Value = valueSet1;
        Assert.True(eventRaised);

        eventRaised = false;
        slider.ValueChanged -= handler;
        slider.Value = valueSet2;
        Assert.False(eventRaised);
    }
}
