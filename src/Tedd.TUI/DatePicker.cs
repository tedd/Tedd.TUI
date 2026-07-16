using System;
using System.Globalization;

namespace Tedd.TUI;

/// <summary>
/// A date input equivalent to the WPF/Avalonia/MAUI <c>DatePicker</c>: an inline segmented
/// <c>yyyy-MM-dd</c> editor plus a dropdown <see cref="Calendar"/> opened from the arrow
/// button (mouse), F4, Alt+Down, Enter or Space.
/// </summary>
/// <remarks>
/// Left/Right move between the year/month/day segments, Up/Down spin the active segment
/// (month and day wrap, the day clamps to the month length) and clicking a segment selects
/// it. While <see cref="SelectedDate"/> is null a <c>____-__-__</c> placeholder is shown
/// and the first spin initializes the value to today. Picking a day in the dropdown
/// calendar commits it and closes the popup.
/// </remarks>
public class DatePicker : UIElement
{
    private enum DateSegment
    {
        Year,
        Month,
        Day
    }

    private const int DateTextWidth = 10; // yyyy-MM-dd

    private DateSegment _activeSegment = DateSegment.Year;
    private Calendar? _popupCalendar;
    private DatePickerPopupBorder? _popupBorder;
    private bool _isDroppedDown;

    public DatePicker()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(DatePicker), null);

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value?.Date);
    }

    public static readonly RoutedEvent SelectedDateChangedEvent =
        RoutedEvent.Register("SelectedDateChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DatePicker));

    public event RoutedEventHandler SelectedDateChanged
    {
        add => AddHandler(SelectedDateChangedEvent, value);
        remove => RemoveHandler(SelectedDateChangedEvent, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register(nameof(FocusedForeground), typeof(TuiColor), typeof(DatePicker), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty PlaceholderColorProperty =
        DependencyProperty.Register(nameof(PlaceholderColor), typeof(TuiColor), typeof(DatePicker), TuiColor.DarkGray);

    /// <summary>Foreground of the <c>____-__-__</c> placeholder while no date is selected.</summary>
    public TuiColor PlaceholderColor
    {
        get => (TuiColor)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public static readonly DependencyProperty ActiveSegmentBackgroundProperty =
        DependencyProperty.Register(nameof(ActiveSegmentBackground), typeof(TuiColor), typeof(DatePicker), TuiColor.DarkGray);

    /// <summary>Background of the active segment while the picker has focus.</summary>
    public TuiColor ActiveSegmentBackground
    {
        get => (TuiColor)GetValue(ActiveSegmentBackgroundProperty);
        set => SetValue(ActiveSegmentBackgroundProperty, value);
    }

    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(nameof(ArrowColor), typeof(TuiColor), typeof(DatePicker), TuiColor.Black);

    public TuiColor ArrowColor
    {
        get => (TuiColor)GetValue(ArrowColorProperty);
        set => SetValue(ArrowColorProperty, value);
    }

    public static readonly DependencyProperty ArrowBackgroundColorProperty =
        DependencyProperty.Register(nameof(ArrowBackgroundColor), typeof(TuiColor), typeof(DatePicker), TuiColor.Gray);

    public TuiColor ArrowBackgroundColor
    {
        get => (TuiColor)GetValue(ArrowBackgroundColorProperty);
        set => SetValue(ArrowBackgroundColorProperty, value);
    }

    public static readonly DependencyProperty PopupBackgroundProperty =
        DependencyProperty.Register(nameof(PopupBackground), typeof(TuiColor), typeof(DatePicker), TuiColor.Black);

    public TuiColor PopupBackground
    {
        get => (TuiColor)GetValue(PopupBackgroundProperty);
        set => SetValue(PopupBackgroundProperty, value);
    }

    public static readonly DependencyProperty PopupBorderColorProperty =
        DependencyProperty.Register(nameof(PopupBorderColor), typeof(TuiColor), typeof(DatePicker), TuiColor.White);

    public TuiColor PopupBorderColor
    {
        get => (TuiColor)GetValue(PopupBorderColorProperty);
        set => SetValue(PopupBorderColorProperty, value);
    }

    /// <summary>True while the dropdown calendar overlay is open.</summary>
    public bool IsDropDownOpen => _isDroppedDown;

    protected override void OnPropertyChanged(DependencyProperty property)
    {
        base.OnPropertyChanged(property);

        if (property == SelectedDateProperty)
        {
            RaiseEvent(new RoutedEventArgs(SelectedDateChangedEvent, this));
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // yyyy-MM-dd + space + dropdown arrow
        return new Size(Width > 0 ? Width : DateTextWidth + 2, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        if (w < 2) return;

        var bg = Background ?? TuiColor.Black;
        var selected = SelectedDate;
        var fg = selected.HasValue
            ? (IsFocused ? FocusedForeground : Foreground)
            : PlaceholderColor;

        string text = selected.HasValue
            ? selected.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "____-__-__";

        // Text area fills everything but the arrow cell.
        for (int i = 0; i < w - 1; i++)
        {
            char c = i < text.Length ? text[i] : ' ';
            var cellBg = bg;
            if (IsFocused && IsInSegment(i, _activeSegment))
            {
                cellBg = ActiveSegmentBackground;
            }
            buffer.SetPixel(x + i, y, c, fg, cellBg);
        }

        buffer.SetPixel(x + w - 1, y, 'v', ArrowColor, ArrowBackgroundColor);
    }

    private static bool IsInSegment(int textIndex, DateSegment segment) => segment switch
    {
        DateSegment.Year => textIndex >= 0 && textIndex <= 3,
        DateSegment.Month => textIndex >= 5 && textIndex <= 6,
        DateSegment.Day => textIndex >= 8 && textIndex <= 9,
        _ => false
    };

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnMouseDown(e);
        Focus();

        if (e.X == RenderSize.Width - 1)
        {
            ToggleDropdown();
        }
        else if (e.X <= 4)
        {
            _activeSegment = DateSegment.Year;
            Invalidate();
        }
        else if (e.X <= 7)
        {
            _activeSegment = DateSegment.Month;
            Invalidate();
        }
        else if (e.X <= DateTextWidth - 1)
        {
            _activeSegment = DateSegment.Day;
            Invalidate();
        }

        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnKeyDown(e);

        if (e.Key == ConsoleKey.F4 ||
            (e.Key == ConsoleKey.DownArrow && e.Modifiers.HasFlag(ConsoleModifiers.Alt)) ||
            e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            ToggleDropdown();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_activeSegment > DateSegment.Year)
                {
                    _activeSegment--;
                    Invalidate();
                }
                e.Handled = true;
                break;
            case ConsoleKey.RightArrow:
                if (_activeSegment < DateSegment.Day)
                {
                    _activeSegment++;
                    Invalidate();
                }
                e.Handled = true;
                break;
            case ConsoleKey.UpArrow:
                SpinSegment(1);
                e.Handled = true;
                break;
            case ConsoleKey.DownArrow:
                SpinSegment(-1);
                e.Handled = true;
                break;
        }
    }

    private void SpinSegment(int direction)
    {
        var selected = SelectedDate;
        if (!selected.HasValue)
        {
            // First spin on an empty picker just fills in today.
            SelectedDate = DateTime.Today;
            return;
        }

        var date = selected.Value;
        switch (_activeSegment)
        {
            case DateSegment.Year:
                try
                {
                    SelectedDate = date.AddYears(direction);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Spinning past DateTime.MinValue/MaxValue is ignored.
                }
                break;
            case DateSegment.Month:
                {
                    int month = date.Month + direction;
                    if (month > 12) month = 1;
                    if (month < 1) month = 12;
                    int day = Math.Min(date.Day, DateTime.DaysInMonth(date.Year, month));
                    SelectedDate = new DateTime(date.Year, month, day);
                    break;
                }
            case DateSegment.Day:
                {
                    int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                    int day = date.Day + direction;
                    if (day > daysInMonth) day = 1;
                    if (day < 1) day = daysInMonth;
                    SelectedDate = new DateTime(date.Year, date.Month, day);
                    break;
                }
        }
    }

    private void ToggleDropdown()
    {
        if (_isDroppedDown)
        {
            CloseDropdown();
        }
        else if (GetRoot() is TuiWindow root)
        {
            OpenDropdown(root);
        }
    }

    private void OpenDropdown(TuiWindow root)
    {
        _isDroppedDown = true;

        // Position relative to the window, compensating ancestor offsets and scrolling
        // (same walk the ComboBox popup uses).
        int absX = RenderSize.X;
        int absY = RenderSize.Y + RenderSize.Height;

        var current = Parent;
        while (current != null && current != root)
        {
            absX += current.RenderSize.X;
            absY += current.RenderSize.Y;
            if (current is ScrollViewer sv)
            {
                absX -= sv.HorizontalOffset;
                absY -= sv.VerticalOffset;
            }
            current = current.Parent;
        }

        _popupCalendar = new Calendar
        {
            DisplayDate = SelectedDate ?? DateTime.Today,
            SelectedDate = SelectedDate,
            Background = PopupBackground
        };
        _popupCalendar.SelectedDateChanged += PopupCalendar_SelectedDateChanged;

        int popupWidth = 20 + 2;
        int popupHeight = 8 + 2;

        // Drop up when there is no room below but enough above.
        int spaceBelow = root.RenderSize.Height - absY;
        if (spaceBelow < popupHeight && absY - RenderSize.Height - popupHeight >= 0)
        {
            absY = absY - RenderSize.Height - popupHeight;
        }

        _popupBorder = new DatePickerPopupBorder
        {
            Width = popupWidth,
            Height = popupHeight,
            Child = _popupCalendar,
            BorderColor = PopupBorderColor,
            Background = PopupBackground,
            BoxStyle = BoxStyle.Single,
            Owner = this
        };

        _popupBorder.Measure(new Size(popupWidth, popupHeight));
        _popupBorder.Arrange(new Rect(absX, absY, popupWidth, popupHeight));

        root.PushOverlay(_popupBorder);
        root.SetFocus(_popupCalendar);
    }

    private void PopupCalendar_SelectedDateChanged(object? sender, RoutedEventArgs e)
    {
        if (_popupCalendar?.SelectedDate is DateTime picked)
        {
            SelectedDate = picked;
        }
        CloseDropdown();
    }

    public void CloseDropdown(bool restoreFocus = true)
    {
        if (GetRoot() is TuiWindow root)
        {
            if (_popupBorder != null)
            {
                root.RemoveOverlay(_popupBorder);
                _popupBorder = null;
            }
            if (restoreFocus)
            {
                root.SetFocus(this);
            }
        }

        if (_popupCalendar != null)
        {
            _popupCalendar.SelectedDateChanged -= PopupCalendar_SelectedDateChanged;
            _popupCalendar = null;
        }
        _isDroppedDown = false;
    }

    internal class DatePickerPopupBorder : Border
    {
        public required DatePicker Owner { get; set; }

        public override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;

            if (e.Key == ConsoleKey.Escape)
            {
                Owner.CloseDropdown();
                e.Handled = true;
            }
        }
    }
}
