using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class DatePickerTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var dp = new DatePicker();
        Assert.Null(dp.SelectedDate);
        Assert.True(dp.Focusable);
        Assert.False(dp.IsDropDownOpen);
    }

    [Fact]
    public void Measure_DateTextPlusArrow()
    {
        var dp = new DatePicker();
        dp.Measure(new Size(100, 100));
        Assert.Equal(12, dp.DesiredSize.Width);
        Assert.Equal(1, dp.DesiredSize.Height);
    }

    [Fact]
    public void Render_NoSelection_ShowsPlaceholder()
    {
        var dp = new DatePicker();
        var host = new ControlTestHost(dp, 12, 1);

        var buffer = host.Render();
        VirtualBufferAssertions.EqualText("____-__-__ v", buffer);
        Assert.Equal(dp.PlaceholderColor, buffer.GetPixel(0, 0).Foreground);
        Assert.Equal(dp.ArrowBackgroundColor, buffer.GetPixel(11, 0).Background);
    }

    [Fact]
    public void Render_SelectedDate_IsoFormatted()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 12) };
        var host = new ControlTestHost(dp, 12, 1);

        VirtualBufferAssertions.EqualText("2026-07-12 v", host.Render());
    }

    [Fact]
    public void Render_ActiveSegmentHighlighted_WhenFocused()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 12) };
        var host = new ControlTestHost(dp, 12, 1);

        // Not focused: no segment highlight
        var before = host.Render();
        Assert.NotEqual(dp.ActiveSegmentBackground, before.GetPixel(0, 0).Background);

        dp.Focus();

        // Default active segment is the year (text cells 0..3)
        var after = host.Render();
        Assert.Equal(dp.ActiveSegmentBackground, after.GetPixel(0, 0).Background);
        Assert.Equal(dp.ActiveSegmentBackground, after.GetPixel(3, 0).Background);
        Assert.NotEqual(dp.ActiveSegmentBackground, after.GetPixel(5, 0).Background);
    }

    [Fact]
    public void MouseClick_OnSegment_SelectsIt()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 12) };
        var host = new ControlTestHost(dp, 12, 1);

        host.MouseDown(8, 0); // day segment (cells 8..9)

        Assert.True(dp.IsFocused);
        var buffer = host.Render();
        Assert.Equal(dp.ActiveSegmentBackground, buffer.GetPixel(8, 0).Background);
        Assert.Equal(dp.ActiveSegmentBackground, buffer.GetPixel(9, 0).Background);
        Assert.NotEqual(dp.ActiveSegmentBackground, buffer.GetPixel(0, 0).Background);

        // Spinning now edits the day
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new DateTime(2026, 7, 13), dp.SelectedDate);
    }

    [Fact]
    public void ArrowKeys_NavigateSegments_AndSpinValues()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 12) };
        var host = new ControlTestHost(dp, 12, 1);
        dp.Focus();

        // Before: year segment active
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new DateTime(2027, 7, 12), dp.SelectedDate);

        host.PressKey(ConsoleKey.RightArrow); // -> month
        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new DateTime(2027, 8, 12), dp.SelectedDate);

        host.PressKey(ConsoleKey.RightArrow); // -> day
        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new DateTime(2027, 8, 11), dp.SelectedDate);

        host.PressKey(ConsoleKey.LeftArrow); // back to month
        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new DateTime(2027, 7, 11), dp.SelectedDate);
    }

    [Fact]
    public void MonthSpin_WrapsWithinYear()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 12, 15) };
        var host = new ControlTestHost(dp, 12, 1);
        dp.Focus();
        host.PressKey(ConsoleKey.RightArrow); // month segment

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new DateTime(2026, 1, 15), dp.SelectedDate);

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new DateTime(2026, 12, 15), dp.SelectedDate);
    }

    [Fact]
    public void MonthSpin_ClampsDayToMonthLength()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 5, 31) };
        var host = new ControlTestHost(dp, 12, 1);
        dp.Focus();
        host.PressKey(ConsoleKey.RightArrow); // month segment

        host.PressKey(ConsoleKey.UpArrow); // May 31 -> June 30

        Assert.Equal(new DateTime(2026, 6, 30), dp.SelectedDate);
    }

    [Fact]
    public void DaySpin_WrapsWithinMonth()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 31) };
        var host = new ControlTestHost(dp, 12, 1);
        dp.Focus();
        host.PressKey(ConsoleKey.RightArrow);
        host.PressKey(ConsoleKey.RightArrow); // day segment

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new DateTime(2026, 7, 1), dp.SelectedDate);

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new DateTime(2026, 7, 31), dp.SelectedDate);
    }

    [Fact]
    public void Spin_FromEmpty_InitializesToToday()
    {
        var dp = new DatePicker();
        var host = new ControlTestHost(dp, 12, 1);
        dp.Focus();

        // Before
        Assert.Null(dp.SelectedDate);

        host.PressKey(ConsoleKey.UpArrow);

        // After: first spin fills in today instead of spinning
        Assert.Equal(DateTime.Today, dp.SelectedDate);
    }

    [Fact]
    public void SelectedDateChanged_RaisedPerChange()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 12) };
        var host = new ControlTestHost(dp, 12, 1);
        dp.Focus();
        int changes = 0;
        dp.SelectedDateChanged += (_, _) => changes++;

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(1, changes);

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void MouseClick_OnArrow_OpensCalendarDropdown()
    {
        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.AddChild(dp);
        var host = new ControlTestHost(panel, 24, 12);

        // Before
        Assert.False(dp.IsDropDownOpen);
        Assert.Null(host.Window.Overlay);

        host.MouseDown(11, 0); // arrow cell

        // After
        Assert.True(dp.IsDropDownOpen);
        var popup = Assert.IsType<DatePicker.DatePickerPopupBorder>(host.Window.Overlay);
        Assert.Same(dp, popup.Owner);

        // The popup renders the selected month below the picker
        var buffer = host.Render();
        VirtualBufferAssertions.EqualText("2026-07-12 v", GetRegion(buffer, 0, 0, 12, 1));
        VirtualBufferAssertions.EqualText("<    July 2026     >", GetRegion(buffer, 1, 2, 20, 1));
    }

    [Fact]
    public void ClickDayInDropdown_CommitsDateAndCloses()
    {
        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.AddChild(dp);
        var host = new ControlTestHost(panel, 24, 12);
        int changes = 0;
        dp.SelectedDateChanged += (_, _) => changes++;

        host.MouseDown(11, 0); // open dropdown
        Assert.True(dp.IsDropDownOpen);

        // Day 15 sits at calendar cell (9..10, 4); calendar starts at (1, 2) inside the border
        host.MouseDown(10, 6);

        Assert.Equal(new DateTime(2026, 7, 15), dp.SelectedDate);
        Assert.Equal(1, changes);
        Assert.False(dp.IsDropDownOpen);
        Assert.Null(host.Window.Overlay);
        Assert.True(dp.IsFocused);
    }

    [Theory]
    [InlineData(ConsoleKey.F4)]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void Key_OpensDropdown(ConsoleKey key)
    {
        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.AddChild(dp);
        var host = new ControlTestHost(panel, 24, 12);
        dp.Focus();

        Assert.False(dp.IsDropDownOpen);

        host.KeyDown(key);

        Assert.True(dp.IsDropDownOpen);
    }

    [Fact]
    public void AltDown_OpensDropdown_EscapeCloses()
    {
        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.AddChild(dp);
        var host = new ControlTestHost(panel, 24, 12);
        dp.Focus();

        host.KeyDown(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Alt);
        Assert.True(dp.IsDropDownOpen);

        // Escape routes to the focused popup calendar and bubbles to the popup border
        host.KeyDown(ConsoleKey.Escape);

        Assert.False(dp.IsDropDownOpen);
        Assert.Null(host.Window.Overlay);
        Assert.True(dp.IsFocused);
        Assert.Equal(new DateTime(2026, 7, 12), dp.SelectedDate); // unchanged
    }

    [Fact]
    public void KeyboardSelectionInDropdown_CommitsDateAndCloses()
    {
        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.AddChild(dp);
        var host = new ControlTestHost(panel, 24, 12);
        dp.Focus();

        host.KeyDown(ConsoleKey.F4);
        host.PressKey(ConsoleKey.RightArrow); // cursor 12 -> 13 in the popup calendar
        host.PressKey(ConsoleKey.Enter);

        Assert.Equal(new DateTime(2026, 7, 13), dp.SelectedDate);
        Assert.False(dp.IsDropDownOpen);
        Assert.True(dp.IsFocused);
    }

    [Fact]
    public void ClickOutsideDropdown_ClosesIt()
    {
        var dp = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var panel = new StackPanel();
        panel.AddChild(dp);
        panel.AddChild(new TextBlock { Text = "x" });
        var host = new ControlTestHost(panel, 40, 16);

        host.MouseDown(11, 0); // open
        Assert.True(dp.IsDropDownOpen);

        host.MouseDown(35, 15); // far away from picker and popup

        Assert.False(dp.IsDropDownOpen);
        Assert.Null(host.Window.Overlay);
        Assert.Equal(new DateTime(2026, 7, 12), dp.SelectedDate); // unchanged
    }

    [Fact]
    public void Disabled_IgnoresMouseAndKeyboard()
    {
        var dp = new DatePicker { SelectedDate = new DateTime(2026, 7, 12), IsEnabled = false };
        var host = new ControlTestHost(dp, 12, 1);

        host.MouseDown(11, 0);
        Assert.False(dp.IsDropDownOpen);
        Assert.False(dp.IsFocused);

        dp.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow });
        Assert.Equal(new DateTime(2026, 7, 12), dp.SelectedDate);
    }

    private static VirtualBuffer GetRegion(VirtualBuffer source, int x, int y, int width, int height)
    {
        var slice = new VirtualBuffer(width, height);
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                var p = source.GetPixel(x + col, y + row);
                slice.SetPixel(col, row, p.Character, p.Foreground, p.Background);
            }
        }
        return slice;
    }
}
