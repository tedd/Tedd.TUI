using System;

namespace Tedd.TUI;

/// <summary>
/// A time input equivalent to the MAUI/Avalonia/WinUI <c>TimePicker</c>: an inline
/// segmented 24-hour <c>HH:mm</c> editor (<c>HH:mm:ss</c> with <see cref="ShowSeconds"/>).
/// </summary>
/// <remarks>
/// Left/Right move between the hour/minute/second segments, Up/Down spin the active
/// segment with wrap-around (23→00 hours, 59→00 minutes/seconds) and clicking a segment
/// selects it. While <see cref="SelectedTime"/> is null a <c>__:__</c> placeholder is
/// shown and the first spin initializes the value to midnight. Values are normalized to
/// whole seconds within one day.
/// </remarks>
public class TimePicker : UIElement
{
    private enum TimeSegment
    {
        Hour,
        Minute,
        Second
    }

    private TimeSegment _activeSegment = TimeSegment.Hour;

    public TimePicker()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty SelectedTimeProperty =
        DependencyProperty.Register(nameof(SelectedTime), typeof(TimeSpan?), typeof(TimePicker), null);

    public TimeSpan? SelectedTime
    {
        get => (TimeSpan?)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value.HasValue ? Normalize(value.Value) : null);
    }

    /// <summary>Wraps into 0..24h and truncates to whole seconds.</summary>
    private static TimeSpan Normalize(TimeSpan value)
    {
        long seconds = (long)value.TotalSeconds;
        const long day = 24L * 60 * 60;
        seconds = ((seconds % day) + day) % day;
        return TimeSpan.FromSeconds(seconds);
    }

    public static readonly DependencyProperty ShowSecondsProperty =
        DependencyProperty.Register(nameof(ShowSeconds), typeof(bool), typeof(TimePicker), false);

    /// <summary>Show and edit a seconds segment (<c>HH:mm:ss</c>).</summary>
    public bool ShowSeconds
    {
        get => (bool)GetValue(ShowSecondsProperty);
        set => SetValue(ShowSecondsProperty, value);
    }

    public static readonly RoutedEvent SelectedTimeChangedEvent =
        RoutedEvent.Register("SelectedTimeChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TimePicker));

    public event RoutedEventHandler SelectedTimeChanged
    {
        add => AddHandler(SelectedTimeChangedEvent, value);
        remove => RemoveHandler(SelectedTimeChangedEvent, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register(nameof(FocusedForeground), typeof(TuiColor), typeof(TimePicker), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.Register(nameof(HoverForeground), typeof(TuiColor), typeof(TimePicker), TuiColor.Cyan);

    /// <summary>Time text foreground used while the mouse hovers the control and it is not focused.</summary>
    public TuiColor HoverForeground
    {
        get => (TuiColor)GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public static readonly DependencyProperty PlaceholderColorProperty =
        DependencyProperty.Register(nameof(PlaceholderColor), typeof(TuiColor), typeof(TimePicker), TuiColor.DarkGray);

    /// <summary>Foreground of the <c>__:__</c> placeholder while no time is selected.</summary>
    public TuiColor PlaceholderColor
    {
        get => (TuiColor)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public static readonly DependencyProperty ActiveSegmentBackgroundProperty =
        DependencyProperty.Register(nameof(ActiveSegmentBackground), typeof(TuiColor), typeof(TimePicker), TuiColor.DarkGray);

    /// <summary>Background of the active segment while the picker has focus.</summary>
    public TuiColor ActiveSegmentBackground
    {
        get => (TuiColor)GetValue(ActiveSegmentBackgroundProperty);
        set => SetValue(ActiveSegmentBackgroundProperty, value);
    }

    private TimeSegment LastSegment => ShowSeconds ? TimeSegment.Second : TimeSegment.Minute;

    protected override void OnPropertyChanged(DependencyProperty property)
    {
        base.OnPropertyChanged(property);

        if (property == SelectedTimeProperty)
        {
            RaiseEvent(new RoutedEventArgs(SelectedTimeChangedEvent, this));
        }
        else if (property == ShowSecondsProperty && !ShowSeconds && _activeSegment == TimeSegment.Second)
        {
            _activeSegment = TimeSegment.Minute;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(ShowSeconds ? 8 : 5, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        if (w < 1) return;

        var bg = Background ?? buffer.GetPixel(x, y).Background;
        var selected = SelectedTime;
        var fg = selected.HasValue
            ? (IsFocused ? FocusedForeground : IsMouseOver ? HoverForeground : Foreground)
            : PlaceholderColor;

        string text;
        if (selected.HasValue)
        {
            var t = selected.Value;
            text = $"{t.Hours:D2}:{t.Minutes:D2}";
            if (ShowSeconds) text += $":{t.Seconds:D2}";
        }
        else
        {
            text = ShowSeconds ? "__:__:__" : "__:__";
        }

        for (int i = 0; i < text.Length && i < w; i++)
        {
            var cellBg = bg;
            if (IsFocused && IsInSegment(i, _activeSegment))
            {
                cellBg = ActiveSegmentBackground;
            }
            buffer.SetPixel(x + i, y, text[i], fg, cellBg);
        }
    }

    private static bool IsInSegment(int textIndex, TimeSegment segment) => segment switch
    {
        TimeSegment.Hour => textIndex >= 0 && textIndex <= 1,
        TimeSegment.Minute => textIndex >= 3 && textIndex <= 4,
        TimeSegment.Second => textIndex >= 6 && textIndex <= 7,
        _ => false
    };

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnMouseDown(e);
        Focus();

        if (e.X <= 2)
        {
            _activeSegment = TimeSegment.Hour;
        }
        else if (e.X <= 5 || !ShowSeconds)
        {
            _activeSegment = TimeSegment.Minute;
        }
        else
        {
            _activeSegment = TimeSegment.Second;
        }
        Invalidate();

        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_activeSegment > TimeSegment.Hour)
                {
                    _activeSegment--;
                    Invalidate();
                }
                e.Handled = true;
                break;
            case ConsoleKey.RightArrow:
                if (_activeSegment < LastSegment)
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
        var selected = SelectedTime;
        if (!selected.HasValue)
        {
            // First spin on an empty picker just fills in midnight.
            SelectedTime = TimeSpan.Zero;
            return;
        }

        var t = selected.Value;
        switch (_activeSegment)
        {
            case TimeSegment.Hour:
                SelectedTime = new TimeSpan((t.Hours + direction + 24) % 24, t.Minutes, t.Seconds);
                break;
            case TimeSegment.Minute:
                SelectedTime = new TimeSpan(t.Hours, (t.Minutes + direction + 60) % 60, t.Seconds);
                break;
            case TimeSegment.Second:
                SelectedTime = new TimeSpan(t.Hours, t.Minutes, (t.Seconds + direction + 60) % 60);
                break;
        }
    }
}
