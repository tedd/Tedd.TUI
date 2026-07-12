using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class TimePickerTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var tp = new TimePicker();
        Assert.Null(tp.SelectedTime);
        Assert.False(tp.ShowSeconds);
        Assert.True(tp.Focusable);
    }

    [Fact]
    public void Measure_WithAndWithoutSeconds()
    {
        var tp = new TimePicker();
        tp.Measure(new Size(100, 100));
        Assert.Equal(5, tp.DesiredSize.Width);
        Assert.Equal(1, tp.DesiredSize.Height);

        var tps = new TimePicker { ShowSeconds = true };
        tps.Measure(new Size(100, 100));
        Assert.Equal(8, tps.DesiredSize.Width);
    }

    [Fact]
    public void Render_NoSelection_ShowsPlaceholder()
    {
        var tp = new TimePicker();
        var host = new ControlTestHost(tp, 5, 1);

        var buffer = host.Render();
        VirtualBufferAssertions.EqualText("__:__", buffer);
        Assert.Equal(tp.PlaceholderColor, buffer.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void Render_SelectedTime_TwentyFourHourFormat()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 0) };
        var host = new ControlTestHost(tp, 5, 1);

        VirtualBufferAssertions.EqualText("13:45", host.Render());
    }

    [Fact]
    public void Render_WithSeconds()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 7), ShowSeconds = true };
        var host = new ControlTestHost(tp, 8, 1);

        VirtualBufferAssertions.EqualText("13:45:07", host.Render());
    }

    [Fact]
    public void Render_ActiveSegmentHighlighted_WhenFocused()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 0) };
        var host = new ControlTestHost(tp, 5, 1);

        // Not focused: no segment highlight
        var before = host.Render();
        Assert.NotEqual(tp.ActiveSegmentBackground, before.GetPixel(0, 0).Background);

        tp.Focus();

        // Default active segment is the hour (cells 0..1)
        var after = host.Render();
        Assert.Equal(tp.ActiveSegmentBackground, after.GetPixel(0, 0).Background);
        Assert.Equal(tp.ActiveSegmentBackground, after.GetPixel(1, 0).Background);
        Assert.NotEqual(tp.ActiveSegmentBackground, after.GetPixel(3, 0).Background);
    }

    [Fact]
    public void MouseClick_OnSegment_SelectsIt()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 0) };
        var host = new ControlTestHost(tp, 5, 1);

        // Before
        Assert.Equal(new TimeSpan(13, 45, 0), tp.SelectedTime);

        host.MouseDown(4, 0); // minute segment (cells 3..4)

        Assert.True(tp.IsFocused);
        var buffer = host.Render();
        Assert.Equal(tp.ActiveSegmentBackground, buffer.GetPixel(3, 0).Background);
        Assert.NotEqual(tp.ActiveSegmentBackground, buffer.GetPixel(0, 0).Background);

        // Spinning now edits the minute
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new TimeSpan(13, 46, 0), tp.SelectedTime);
        VirtualBufferAssertions.EqualText("13:46", host.Render());
    }

    [Fact]
    public void ArrowKeys_NavigateSegments_AndSpinValues()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 30), ShowSeconds = true };
        var host = new ControlTestHost(tp, 8, 1);
        tp.Focus();

        // Before: hour segment active
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new TimeSpan(14, 45, 30), tp.SelectedTime);

        host.PressKey(ConsoleKey.RightArrow); // -> minute
        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new TimeSpan(14, 44, 30), tp.SelectedTime);

        host.PressKey(ConsoleKey.RightArrow); // -> second
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new TimeSpan(14, 44, 31), tp.SelectedTime);

        host.PressKey(ConsoleKey.LeftArrow); // back to minute
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new TimeSpan(14, 45, 31), tp.SelectedTime);
    }

    [Fact]
    public void RightArrow_WithoutSeconds_StopsAtMinute()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 0) };
        var host = new ControlTestHost(tp, 5, 1);
        tp.Focus();

        host.PressKey(ConsoleKey.RightArrow); // -> minute
        host.PressKey(ConsoleKey.RightArrow); // no seconds segment: stays on minute
        host.PressKey(ConsoleKey.UpArrow);

        Assert.Equal(new TimeSpan(13, 46, 0), tp.SelectedTime);
    }

    [Fact]
    public void HourSpin_WrapsAroundMidnight()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(23, 15, 0) };
        var host = new ControlTestHost(tp, 5, 1);
        tp.Focus();

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new TimeSpan(0, 15, 0), tp.SelectedTime);
        VirtualBufferAssertions.EqualText("00:15", host.Render());

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new TimeSpan(23, 15, 0), tp.SelectedTime);
    }

    [Fact]
    public void MinuteSpin_WrapsWithoutCarryingIntoHour()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 59, 0) };
        var host = new ControlTestHost(tp, 5, 1);
        tp.Focus();
        host.PressKey(ConsoleKey.RightArrow); // minute segment

        host.PressKey(ConsoleKey.UpArrow);

        Assert.Equal(new TimeSpan(13, 0, 0), tp.SelectedTime);
    }

    [Fact]
    public void Spin_FromEmpty_InitializesToMidnight()
    {
        var tp = new TimePicker();
        var host = new ControlTestHost(tp, 5, 1);
        tp.Focus();

        // Before
        Assert.Null(tp.SelectedTime);
        VirtualBufferAssertions.EqualText("__:__", host.Render());

        host.PressKey(ConsoleKey.UpArrow);

        // After: first spin fills in midnight instead of spinning
        Assert.Equal(TimeSpan.Zero, tp.SelectedTime);
        VirtualBufferAssertions.EqualText("00:00", host.Render());
    }

    [Fact]
    public void SelectedTime_NormalizedIntoOneDay()
    {
        var tp = new TimePicker();

        tp.SelectedTime = new TimeSpan(25, 30, 0);
        Assert.Equal(new TimeSpan(1, 30, 0), tp.SelectedTime);

        tp.SelectedTime = TimeSpan.FromHours(-1);
        Assert.Equal(new TimeSpan(23, 0, 0), tp.SelectedTime);

        tp.SelectedTime = new TimeSpan(0, 12, 0, 1, 900);
        Assert.Equal(new TimeSpan(12, 0, 1), tp.SelectedTime); // milliseconds truncated
    }

    [Fact]
    public void SelectedTimeChanged_RaisedPerChange()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 0) };
        var host = new ControlTestHost(tp, 5, 1);
        tp.Focus();
        int changes = 0;
        tp.SelectedTimeChanged += (_, _) => changes++;

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(1, changes);

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Disabled_IgnoresMouseAndKeyboard()
    {
        var tp = new TimePicker { SelectedTime = new TimeSpan(13, 45, 0), IsEnabled = false };
        var host = new ControlTestHost(tp, 5, 1);

        host.MouseDown(0, 0);
        Assert.False(tp.IsFocused);

        tp.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow });
        Assert.Equal(new TimeSpan(13, 45, 0), tp.SelectedTime);
    }
}
