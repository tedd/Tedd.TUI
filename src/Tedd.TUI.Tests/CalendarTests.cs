using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class CalendarTests
{
    private static Calendar July2026Calendar() => new Calendar
    {
        DisplayDate = new DateTime(2026, 7, 12)
    };

    [Fact]
    public void Properties_DefaultValues()
    {
        var cal = new Calendar();
        Assert.Equal(DateTime.Today, cal.DisplayDate);
        Assert.Null(cal.SelectedDate);
        Assert.Equal(DayOfWeek.Sunday, cal.FirstDayOfWeek);
        Assert.True(cal.Focusable);
    }

    [Fact]
    public void Measure_FixedMonthGrid()
    {
        var cal = new Calendar();
        cal.Measure(new Size(100, 100));
        Assert.Equal(20, cal.DesiredSize.Width);
        Assert.Equal(8, cal.DesiredSize.Height);
    }

    [Fact]
    public void Render_MonthGrid_SundayFirst()
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);

        VirtualBufferAssertions.EqualText(
            "<    July 2026     >\n" +
            "Su Mo Tu We Th Fr Sa\n" +
            "          1  2  3  4\n" +
            " 5  6  7  8  9 10 11\n" +
            "12 13 14 15 16 17 18\n" +
            "19 20 21 22 23 24 25\n" +
            "26 27 28 29 30 31   \n" +
            "                    ",
            host.Render());
    }

    [Fact]
    public void Render_MonthGrid_MondayFirst()
    {
        var cal = July2026Calendar();
        cal.FirstDayOfWeek = DayOfWeek.Monday;
        var host = new ControlTestHost(cal, 20, 8);

        VirtualBufferAssertions.EqualText(
            "<    July 2026     >\n" +
            "Mo Tu We Th Fr Sa Su\n" +
            "       1  2  3  4  5\n" +
            " 6  7  8  9 10 11 12\n" +
            "13 14 15 16 17 18 19\n" +
            "20 21 22 23 24 25 26\n" +
            "27 28 29 30 31      \n" +
            "                    ",
            host.Render());
    }

    [Fact]
    public void MouseClick_OnDay_SelectsIt()
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);
        int selectionChanges = 0;
        cal.SelectedDateChanged += (_, _) => selectionChanges++;

        // Before
        Assert.Null(cal.SelectedDate);

        // Day 15 sits at column 3, week row 2 -> buffer (9..10, 4)
        var args = host.MouseDown(9, 4);

        // After
        Assert.True(args.Handled);
        Assert.True(cal.IsFocused);
        Assert.Equal(new DateTime(2026, 7, 15), cal.SelectedDate);
        Assert.Equal(new DateTime(2026, 7, 15), cal.DisplayDate);
        Assert.Equal(1, selectionChanges);
    }

    [Fact]
    public void MouseClick_OnEmptyCell_DoesNotSelect()
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);

        // First week row starts with three empty cells (Su/Mo/Tu)
        host.MouseDown(0, 2);

        Assert.Null(cal.SelectedDate);
        Assert.Equal(new DateTime(2026, 7, 12), cal.DisplayDate);
        Assert.True(cal.IsFocused);
    }

    [Fact]
    public void MouseClick_HeaderArrows_NavigateMonths()
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);
        int displayChanges = 0;
        cal.DisplayDateChanged += (_, _) => displayChanges++;

        host.MouseDown(19, 0); // ">"

        Assert.Equal(new DateTime(2026, 8, 12), cal.DisplayDate);
        Assert.Null(cal.SelectedDate);
        Assert.Equal(1, displayChanges);
        VirtualBufferAssertions.EqualText("<   August 2026    >", GetRow(host, 0));

        host.MouseDown(0, 0); // "<"

        Assert.Equal(new DateTime(2026, 7, 12), cal.DisplayDate);
        VirtualBufferAssertions.EqualText("<    July 2026     >", GetRow(host, 0));
    }

    [Fact]
    public void ArrowKeys_MoveCursorByDayAndWeek()
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);
        cal.Focus();

        // Before
        Assert.Equal(new DateTime(2026, 7, 12), cal.DisplayDate);

        host.PressKey(ConsoleKey.RightArrow);
        Assert.Equal(new DateTime(2026, 7, 13), cal.DisplayDate);

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(new DateTime(2026, 7, 20), cal.DisplayDate);

        host.PressKey(ConsoleKey.LeftArrow);
        Assert.Equal(new DateTime(2026, 7, 19), cal.DisplayDate);

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(new DateTime(2026, 7, 12), cal.DisplayDate);

        // Navigation alone never selects
        Assert.Null(cal.SelectedDate);
    }

    [Fact]
    public void ArrowKeys_CrossMonthBoundary()
    {
        var cal = new Calendar { DisplayDate = new DateTime(2026, 7, 31) };
        var host = new ControlTestHost(cal, 20, 8);
        cal.Focus();

        host.PressKey(ConsoleKey.RightArrow);

        Assert.Equal(new DateTime(2026, 8, 1), cal.DisplayDate);
        VirtualBufferAssertions.EqualText("<   August 2026    >", GetRow(host, 0));
    }

    [Fact]
    public void PageKeys_ChangeMonth_ClampingDayOfMonth()
    {
        var cal = new Calendar { DisplayDate = new DateTime(2026, 7, 31) };
        var host = new ControlTestHost(cal, 20, 8);
        cal.Focus();

        host.PressKey(ConsoleKey.PageUp);
        Assert.Equal(new DateTime(2026, 6, 30), cal.DisplayDate); // June has 30 days

        host.PressKey(ConsoleKey.PageDown);
        Assert.Equal(new DateTime(2026, 7, 30), cal.DisplayDate);
    }

    [Fact]
    public void HomeEndKeys_JumpToMonthEdges()
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);
        cal.Focus();

        host.PressKey(ConsoleKey.End);
        Assert.Equal(new DateTime(2026, 7, 31), cal.DisplayDate);

        host.PressKey(ConsoleKey.Home);
        Assert.Equal(new DateTime(2026, 7, 1), cal.DisplayDate);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void EnterOrSpace_SelectsCursorDate(ConsoleKey key)
    {
        var cal = July2026Calendar();
        var host = new ControlTestHost(cal, 20, 8);
        cal.Focus();
        int selectionChanges = 0;
        cal.SelectedDateChanged += (_, _) => selectionChanges++;

        // Before
        Assert.Null(cal.SelectedDate);

        host.PressKey(ConsoleKey.RightArrow); // move cursor to the 13th
        host.PressKey(key);

        // After
        Assert.Equal(new DateTime(2026, 7, 13), cal.SelectedDate);
        Assert.Equal(1, selectionChanges);
    }

    [Fact]
    public void Render_SelectedDay_UsesSelectionColors()
    {
        // Fixed month in the past so "today" highlighting can't interfere
        var cal = new Calendar
        {
            DisplayDate = new DateTime(2024, 3, 1),
            SelectedDate = new DateTime(2024, 3, 15)
        };
        var host = new ControlTestHost(cal, 20, 8);

        var buffer = host.Render();
        // Day 15 sits at column 5, week row 2 -> (15..16, 4)
        Assert.Equal('1', buffer.GetPixel(15, 4).Character);
        Assert.Equal('5', buffer.GetPixel(16, 4).Character);
        Assert.Equal(cal.SelectedBackground, buffer.GetPixel(15, 4).Background);
        Assert.Equal(cal.SelectedForeground, buffer.GetPixel(15, 4).Foreground);
        // A plain day keeps the regular colors
        Assert.NotEqual(cal.SelectedBackground, buffer.GetPixel(0, 4).Background);
    }

    [Fact]
    public void Render_CursorDay_HighlightedWhileFocused()
    {
        var cal = new Calendar { DisplayDate = new DateTime(2024, 3, 20) };
        var host = new ControlTestHost(cal, 20, 8);

        // Not focused: no cursor highlight
        var before = host.Render();
        Assert.NotEqual(cal.FocusedDayBackground, before.GetPixel(9, 5).Background);

        cal.Focus();

        // Day 20 sits at column 3, week row 3 -> (9..10, 5)
        var after = host.Render();
        Assert.Equal('2', after.GetPixel(9, 5).Character);
        Assert.Equal('0', after.GetPixel(10, 5).Character);
        Assert.Equal(cal.FocusedDayBackground, after.GetPixel(9, 5).Background);
        Assert.Equal(cal.FocusedDayForeground, after.GetPixel(9, 5).Foreground);
    }

    [Fact]
    public void Disabled_IgnoresMouseAndKeyboard()
    {
        var cal = July2026Calendar();
        cal.IsEnabled = false;
        var host = new ControlTestHost(cal, 20, 8);

        host.MouseDown(9, 4);
        Assert.Null(cal.SelectedDate);
        Assert.False(cal.IsFocused);

        cal.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        Assert.Equal(new DateTime(2026, 7, 12), cal.DisplayDate);
    }

    private static VirtualBuffer GetRow(ControlTestHost host, int row)
    {
        var full = host.Render();
        var slice = new VirtualBuffer(full.Width, 1);
        for (int x = 0; x < full.Width; x++)
        {
            var p = full.GetPixel(x, row);
            slice.SetPixel(x, 0, p.Character, p.Foreground, p.Background);
        }
        return slice;
    }
}
