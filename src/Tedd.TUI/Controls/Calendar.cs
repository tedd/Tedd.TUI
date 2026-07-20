using System;
using System.Globalization;

namespace Tedd.TUI.Controls;

/// <summary>
/// A month-view calendar equivalent to the WPF/Avalonia <c>Calendar</c>. Renders a fixed
/// 20x8 cell grid: a header line with <c>&lt;</c>/<c>&gt;</c> month navigation and the
/// centered month name, a weekday abbreviation line, and up to six week rows of day cells.
/// </summary>
/// <remarks>
/// <see cref="DisplayDate"/> is both the displayed month and the keyboard cursor:
/// arrow keys move it by day/week, PageUp/PageDown by month, Home/End to the first/last
/// day of the month, and Enter/Space copy it into <see cref="SelectedDate"/>. Clicking a
/// day selects it; clicking the header arrows changes month. Month and weekday names use
/// the invariant culture so rendering is machine-independent.
/// </remarks>
public class Calendar : UIElement
{
    private const int GridWidth = 20;  // 7 columns x (2 chars + 1 gap) - trailing gap
    private const int GridHeight = 8;  // header + weekdays + 6 week rows
    private const int FirstDayRow = 2;

    public Calendar()
    {
        Focusable = true;
        DisplayDate = DateTime.Today;
    }

    public static readonly DependencyProperty DisplayDateProperty =
        DependencyProperty.Register(nameof(DisplayDate), typeof(DateTime), typeof(Calendar), DateTime.MinValue);

    /// <summary>
    /// The date the calendar displays and the keyboard cursor rests on. Defaults to today.
    /// </summary>
    public DateTime DisplayDate
    {
        get => (DateTime)GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value.Date);
    }

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(Calendar), null, bindsTwoWayByDefault: true);

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value?.Date);
    }

    public static readonly DependencyProperty FirstDayOfWeekProperty =
        DependencyProperty.Register(nameof(FirstDayOfWeek), typeof(DayOfWeek), typeof(Calendar), DayOfWeek.Sunday);

    public DayOfWeek FirstDayOfWeek
    {
        get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);
        set => SetValue(FirstDayOfWeekProperty, value);
    }

    public static readonly RoutedEvent SelectedDateChangedEvent =
        RoutedEvent.Register("SelectedDateChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Calendar));

    public event RoutedEventHandler SelectedDateChanged
    {
        add => AddHandler(SelectedDateChangedEvent, value);
        remove => RemoveHandler(SelectedDateChangedEvent, value);
    }

    public static readonly RoutedEvent DisplayDateChangedEvent =
        RoutedEvent.Register("DisplayDateChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Calendar));

    public event RoutedEventHandler DisplayDateChanged
    {
        add => AddHandler(DisplayDateChangedEvent, value);
        remove => RemoveHandler(DisplayDateChangedEvent, value);
    }

    public static readonly DependencyProperty HeaderColorProperty =
        DependencyProperty.Register(nameof(HeaderColor), typeof(TuiColor), typeof(Calendar), TuiColor.White);

    public TuiColor HeaderColor
    {
        get => (TuiColor)GetValue(HeaderColorProperty);
        set => SetValue(HeaderColorProperty, value);
    }

    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(nameof(ArrowColor), typeof(TuiColor), typeof(Calendar), TuiColor.Gray);

    /// <summary>Color of the <c>&lt;</c>/<c>&gt;</c> month navigation arrows.</summary>
    public TuiColor ArrowColor
    {
        get => (TuiColor)GetValue(ArrowColorProperty);
        set => SetValue(ArrowColorProperty, value);
    }

    public static readonly DependencyProperty WeekdayColorProperty =
        DependencyProperty.Register(nameof(WeekdayColor), typeof(TuiColor), typeof(Calendar), TuiColor.DarkGray);

    public TuiColor WeekdayColor
    {
        get => (TuiColor)GetValue(WeekdayColorProperty);
        set => SetValue(WeekdayColorProperty, value);
    }

    public static readonly DependencyProperty TodayColorProperty =
        DependencyProperty.Register(nameof(TodayColor), typeof(TuiColor), typeof(Calendar), TuiColor.Cyan);

    /// <summary>Foreground used for today's date when it is neither selected nor the cursor.</summary>
    public TuiColor TodayColor
    {
        get => (TuiColor)GetValue(TodayColorProperty);
        set => SetValue(TodayColorProperty, value);
    }

    public static readonly DependencyProperty SelectedForegroundProperty =
        DependencyProperty.Register(nameof(SelectedForeground), typeof(TuiColor), typeof(Calendar), TuiColor.Black);

    public TuiColor SelectedForeground
    {
        get => (TuiColor)GetValue(SelectedForegroundProperty);
        set => SetValue(SelectedForegroundProperty, value);
    }

    public static readonly DependencyProperty SelectedBackgroundProperty =
        DependencyProperty.Register(nameof(SelectedBackground), typeof(TuiColor), typeof(Calendar), TuiColor.Gray);

    public TuiColor SelectedBackground
    {
        get => (TuiColor)GetValue(SelectedBackgroundProperty);
        set => SetValue(SelectedBackgroundProperty, value);
    }

    public static readonly DependencyProperty FocusedDayForegroundProperty =
        DependencyProperty.Register(nameof(FocusedDayForeground), typeof(TuiColor), typeof(Calendar), TuiColor.Yellow);

    /// <summary>Foreground of the cursor day (<see cref="DisplayDate"/>) while the calendar has focus.</summary>
    public TuiColor FocusedDayForeground
    {
        get => (TuiColor)GetValue(FocusedDayForegroundProperty);
        set => SetValue(FocusedDayForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedDayBackgroundProperty =
        DependencyProperty.Register(nameof(FocusedDayBackground), typeof(TuiColor), typeof(Calendar), TuiColor.DarkGray);

    /// <summary>Background of the cursor day (<see cref="DisplayDate"/>) while the calendar has focus.</summary>
    public TuiColor FocusedDayBackground
    {
        get => (TuiColor)GetValue(FocusedDayBackgroundProperty);
        set => SetValue(FocusedDayBackgroundProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty property)
    {
        base.OnPropertyChanged(property);

        if (property == SelectedDateProperty)
        {
            RaiseEvent(new RoutedEventArgs(SelectedDateChangedEvent, this));
        }
        else if (property == DisplayDateProperty)
        {
            RaiseEvent(new RoutedEventArgs(DisplayDateChangedEvent, this));
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(GridWidth, GridHeight);
    }

    /// <summary>Cell index (0-based, row-major over the 7-column grid) of day 1.</summary>
    private int FirstDayOffset(DateTime month)
    {
        var firstOfMonth = new DateTime(month.Year, month.Month, 1);
        return ((int)firstOfMonth.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;
        if (w < GridWidth || h < GridHeight) return;

        var bg = Background ?? buffer.GetPixel(x, y).Background;
        var display = DisplayDate;
        var selected = SelectedDate;
        var today = DateTime.Today;

        // Header: "<     July 2026    >"
        buffer.FillRect(x, y, GridWidth, 1, ' ', HeaderColor, bg);
        buffer.SetPixel(x, y, '<', ArrowColor, bg);
        buffer.SetPixel(x + GridWidth - 1, y, '>', ArrowColor, bg);
        string header = display.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        if (header.Length > GridWidth - 2) header = header.Substring(0, GridWidth - 2);
        int headerStart = x + 1 + (GridWidth - 2 - header.Length) / 2;
        for (int i = 0; i < header.Length; i++)
        {
            buffer.SetPixel(headerStart + i, y, header[i], HeaderColor, bg);
        }

        // Weekday row: "Su Mo Tu We Th Fr Sa"
        var dayNames = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames;
        for (int col = 0; col < 7; col++)
        {
            var dow = (DayOfWeek)(((int)FirstDayOfWeek + col) % 7);
            string abbrev = dayNames[(int)dow];
            int cx = x + col * 3;
            buffer.SetPixel(cx, y + 1, abbrev.Length > 0 ? abbrev[0] : ' ', WeekdayColor, bg);
            buffer.SetPixel(cx + 1, y + 1, abbrev.Length > 1 ? abbrev[1] : ' ', WeekdayColor, bg);
            if (col < 6) buffer.SetPixel(cx + 2, y + 1, ' ', WeekdayColor, bg);
        }

        // Day grid
        int offset = FirstDayOffset(display);
        int daysInMonth = DateTime.DaysInMonth(display.Year, display.Month);
        for (int row = 0; row < 6; row++)
        {
            int cy = y + FirstDayRow + row;
            for (int col = 0; col < 7; col++)
            {
                int cx = x + col * 3;
                int day = row * 7 + col - offset + 1;

                char c1 = ' ', c2 = ' ';
                var fg = Foreground;
                var cellBg = bg;

                if (day >= 1 && day <= daysInMonth)
                {
                    string text = day.ToString(CultureInfo.InvariantCulture).PadLeft(2);
                    c1 = text[0];
                    c2 = text[1];

                    bool isCursor = IsFocused && day == display.Day;
                    bool isSelected = selected.HasValue &&
                        selected.Value.Year == display.Year &&
                        selected.Value.Month == display.Month &&
                        selected.Value.Day == day;
                    bool isToday = today.Year == display.Year && today.Month == display.Month && today.Day == day;

                    if (isCursor)
                    {
                        fg = FocusedDayForeground;
                        cellBg = FocusedDayBackground;
                    }
                    else if (isSelected)
                    {
                        fg = SelectedForeground;
                        cellBg = SelectedBackground;
                    }
                    else if (isToday)
                    {
                        fg = TodayColor;
                    }
                }

                buffer.SetPixel(cx, cy, c1, fg, cellBg);
                buffer.SetPixel(cx + 1, cy, c2, fg, cellBg);
                if (col < 6) buffer.SetPixel(cx + 2, cy, ' ', fg, bg);
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case ConsoleKey.LeftArrow:
                MoveDisplayDate(d => d.AddDays(-1));
                e.Handled = true;
                break;
            case ConsoleKey.RightArrow:
                MoveDisplayDate(d => d.AddDays(1));
                e.Handled = true;
                break;
            case ConsoleKey.UpArrow:
                MoveDisplayDate(d => d.AddDays(-7));
                e.Handled = true;
                break;
            case ConsoleKey.DownArrow:
                MoveDisplayDate(d => d.AddDays(7));
                e.Handled = true;
                break;
            case ConsoleKey.PageUp:
                MoveDisplayDate(d => d.AddMonths(-1));
                e.Handled = true;
                break;
            case ConsoleKey.PageDown:
                MoveDisplayDate(d => d.AddMonths(1));
                e.Handled = true;
                break;
            case ConsoleKey.Home:
                MoveDisplayDate(d => new DateTime(d.Year, d.Month, 1));
                e.Handled = true;
                break;
            case ConsoleKey.End:
                MoveDisplayDate(d => new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month)));
                e.Handled = true;
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                SelectedDate = DisplayDate;
                e.Handled = true;
                break;
        }
    }

    private void MoveDisplayDate(Func<DateTime, DateTime> transform)
    {
        try
        {
            DisplayDate = transform(DisplayDate);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Navigation past DateTime.MinValue/MaxValue is ignored.
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnMouseDown(e);
        Focus();

        if (e.Y == 0)
        {
            // Header: month navigation arrows (2-cell click targets at each edge)
            if (e.X <= 1)
            {
                MoveDisplayDate(d => d.AddMonths(-1));
            }
            else if (e.X >= GridWidth - 2)
            {
                MoveDisplayDate(d => d.AddMonths(1));
            }
        }
        else if (e.Y >= FirstDayRow && e.Y < FirstDayRow + 6)
        {
            int col = e.X / 3;
            if (col >= 0 && col < 7)
            {
                var display = DisplayDate;
                int offset = FirstDayOffset(display);
                int day = (e.Y - FirstDayRow) * 7 + col - offset + 1;
                if (day >= 1 && day <= DateTime.DaysInMonth(display.Year, display.Month))
                {
                    var date = new DateTime(display.Year, display.Month, day);
                    DisplayDate = date;
                    SelectedDate = date;
                }
            }
        }

        e.Handled = true;
    }
}
