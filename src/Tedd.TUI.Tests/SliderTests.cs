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

    [Fact]
    public void Slider_ValueChangedEvent_Fired()
    {
        var slider = new Slider { Minimum = 0, Maximum = 10, Value = 0 };
        int eventCount = 0;
        object? eventSender = null;
        RoutedEventArgs? eventArgs = null;

        slider.ValueChanged += (s, e) =>
        {
            eventCount++;
            eventSender = s;
            eventArgs = e;
        };

        // Act: change value within bounds
        slider.Value = 5;

        // Assert: event fired once
        Assert.Equal(1, eventCount);
        Assert.Same(slider, eventSender);
        Assert.NotNull(eventArgs);
        Assert.Equal(Slider.ValueChangedEvent, eventArgs.RoutedEvent);
        Assert.Same(slider, eventArgs.Source);

        // Act: change value out of bounds (should clamp to 10)
        slider.Value = 15;

        // Assert: event fired twice
        Assert.Equal(2, eventCount);
        Assert.Equal(10, slider.Value);

        // Act: change value to same (10)
        slider.Value = 10;

        // Assert: event should not fire again
        Assert.Equal(2, eventCount);

        // Act: change value out of bounds (should clamp to 0)
        slider.Value = -5;

        // Assert: event fired third time
        Assert.Equal(3, eventCount);
        Assert.Equal(0, slider.Value);
    }

    [Fact]
    public void Slider_SetValueDirectly_ClampsAndFiresEvent()
    {
        var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5 };
        int eventCount = 0;
        slider.ValueChanged += (s, e) => eventCount++;

        // Act: set value directly via SetValue (simulates binding update, bypassing the property setter)
        slider.SetValue(Slider.ValueProperty, 15);

        // Assert: out-of-range value should be clamped to max
        Assert.Equal(10, slider.Value);
        Assert.Equal(1, eventCount);

        // Act: set another out-of-range value
        slider.SetValue(Slider.ValueProperty, -5);

        // Assert: clamped to min
        Assert.Equal(0, slider.Value);
        Assert.Equal(2, eventCount);

        // Act: same value again
        slider.SetValue(Slider.ValueProperty, 0);

        // Assert: no event when value doesn't change
        Assert.Equal(0, slider.Value);
        Assert.Equal(2, eventCount);
    }
}
