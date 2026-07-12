using System;

namespace Tedd.TUI;

/// <summary>
/// A button that raises its <see cref="ButtonBase.Click"/> event repeatedly from the time
/// it is pressed until it is released, equivalent to the WPF/Avalonia <c>RepeatButton</c>
/// primitive (used for spinner buttons, scroll arrows, etc.).
/// </summary>
/// <remarks>
/// The default <see cref="ButtonBase.ClickMode"/> is <see cref="ClickMode.Press"/>, so the
/// first click fires on mouse-down / key-down. While the mouse button is held, a timer
/// raises further clicks after <see cref="Delay"/> milliseconds and then every
/// <see cref="Interval"/> milliseconds; timer clicks are raised on a thread-pool thread,
/// like other off-thread invalidation sources in the framework. Keyboard repeat relies on
/// the terminal's own key auto-repeat, which re-sends KeyDown while a key is held.
/// </remarks>
public class RepeatButton : Button
{
    private System.Threading.Timer? _repeatTimer;
    private readonly System.Threading.Lock _timerLock = new();

    public RepeatButton()
    {
        ClickMode = ClickMode.Press;
    }

    public static readonly DependencyProperty DelayProperty =
        DependencyProperty.Register("Delay", typeof(int), typeof(RepeatButton), 500);

    /// <summary>Milliseconds the button waits, while pressed, before it starts repeating clicks.</summary>
    public int Delay
    {
        get => (int)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public static readonly DependencyProperty IntervalProperty =
        DependencyProperty.Register("Interval", typeof(int), typeof(RepeatButton), 100);

    /// <summary>Milliseconds between repeated clicks once repeating has started.</summary>
    public int Interval
    {
        get => (int)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (IsPressed)
        {
            StartRepeating();
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        StopRepeating();
        base.OnMouseUp(e);
    }

    public override void OnLostFocus()
    {
        StopRepeating();
        base.OnLostFocus();
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == IsEnabledProperty && !IsEnabled)
        {
            StopRepeating();
        }
    }

    private void StartRepeating()
    {
        lock (_timerLock)
        {
            _repeatTimer?.Dispose();
            _repeatTimer = new System.Threading.Timer(
                static state => ((RepeatButton)state!).OnRepeatTick(),
                this,
                Math.Max(0, Delay),
                Math.Max(1, Interval));
        }
    }

    private void StopRepeating()
    {
        lock (_timerLock)
        {
            _repeatTimer?.Dispose();
            _repeatTimer = null;
        }
    }

    /// <summary>
    /// One repeat-timer beat: raises a click while the button is still pressed and enabled.
    /// Internal so tests can drive repeating deterministically without real timing.
    /// </summary>
    internal void OnRepeatTick()
    {
        if (!IsPressed || !IsEnabled) return;
        OnClick();
    }
}
