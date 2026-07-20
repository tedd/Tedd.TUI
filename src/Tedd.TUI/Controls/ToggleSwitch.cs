using System;

namespace Tedd.TUI.Controls;

/// <summary>
/// An on/off switch with a sliding knob, equivalent to the MAUI <c>Switch</c> and the
/// Avalonia/WinUI <c>ToggleSwitch</c>. Renders as a small track (<c>[●──]</c> off,
/// <c>[──●]</c> on) followed by the current state label (<see cref="OffContent"/> /
/// <see cref="OnContent"/>) and the optional <see cref="ContentControl.Content"/>.
/// Toggling behavior (mouse click, Space/Enter, <see cref="ToggleButton.Checked"/> /
/// <see cref="ToggleButton.Unchecked"/> events) is inherited from <see cref="ToggleButton"/>.
/// </summary>
public class ToggleSwitch : ToggleButton
{
    // [ + 3 track cells + ]
    private const int TrackWidth = 5;

    public ToggleSwitch()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty OnContentProperty =
        DependencyProperty.Register("OnContent", typeof(object), typeof(ToggleSwitch), "On");

    /// <summary>State label shown next to the track while the switch is on.</summary>
    public object? OnContent
    {
        get => GetValue(OnContentProperty);
        set => SetValue(OnContentProperty, value);
    }

    public static readonly DependencyProperty OffContentProperty =
        DependencyProperty.Register("OffContent", typeof(object), typeof(ToggleSwitch), "Off");

    /// <summary>State label shown next to the track while the switch is off.</summary>
    public object? OffContent
    {
        get => GetValue(OffContentProperty);
        set => SetValue(OffContentProperty, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(TuiColor), typeof(ToggleSwitch), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.Register("HoverForeground", typeof(TuiColor), typeof(ToggleSwitch), TuiColor.Cyan);

    /// <summary>Label foreground used while the mouse hovers the control and it is not focused.</summary>
    public TuiColor HoverForeground
    {
        get => (TuiColor)GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public static readonly DependencyProperty BracketColorProperty =
        DependencyProperty.Register("BracketColor", typeof(TuiColor), typeof(ToggleSwitch), TuiColor.Gray);

    public TuiColor BracketColor
    {
        get => (TuiColor)GetValue(BracketColorProperty);
        set => SetValue(BracketColorProperty, value);
    }

    public static readonly DependencyProperty TrackColorProperty =
        DependencyProperty.Register("TrackColor", typeof(TuiColor), typeof(ToggleSwitch), TuiColor.DarkGray);

    public TuiColor TrackColor
    {
        get => (TuiColor)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public static readonly DependencyProperty KnobColorProperty =
        DependencyProperty.Register("KnobColor", typeof(TuiColor), typeof(ToggleSwitch), TuiColor.White);

    /// <summary>Knob color while the switch is off (or indeterminate).</summary>
    public TuiColor KnobColor
    {
        get => (TuiColor)GetValue(KnobColorProperty);
        set => SetValue(KnobColorProperty, value);
    }

    public static readonly DependencyProperty OnKnobColorProperty =
        DependencyProperty.Register("OnKnobColor", typeof(TuiColor), typeof(ToggleSwitch), TuiColor.Green);

    /// <summary>Knob color while the switch is on.</summary>
    public TuiColor OnKnobColor
    {
        get => (TuiColor)GetValue(OnKnobColorProperty);
        set => SetValue(OnKnobColorProperty, value);
    }

    public static readonly DependencyProperty KnobCharProperty =
        DependencyProperty.Register("KnobChar", typeof(char), typeof(ToggleSwitch), '●');

    public char KnobChar
    {
        get => (char)GetValue(KnobCharProperty);
        set => SetValue(KnobCharProperty, value);
    }

    public static readonly DependencyProperty TrackCharProperty =
        DependencyProperty.Register("TrackChar", typeof(char), typeof(ToggleSwitch), '─');

    public char TrackChar
    {
        get => (char)GetValue(TrackCharProperty);
        set => SetValue(TrackCharProperty, value);
    }

    private string StateLabel => (IsChecked == true ? OnContent : OffContent)?.ToString() ?? string.Empty;

    private int StateLabelWidth
    {
        get
        {
            int on = OnContent?.ToString()?.Length ?? 0;
            int off = OffContent?.ToString()?.Length ?? 0;
            return Math.Max(on, off);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // [●──] Off Content
        int width = TrackWidth;

        int labelWidth = StateLabelWidth;
        if (labelWidth > 0) width += 1 + labelWidth;

        string content = Content?.ToString() ?? string.Empty;
        if (content.Length > 0) width += 1 + content.Length;

        return new Size(width, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        var fg = IsFocused ? FocusedForeground : IsMouseOver ? HoverForeground : Foreground;
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        var isChecked = IsChecked;
        // Knob cell within the 3-cell track: left = off, middle = indeterminate, right = on.
        int knobIndex = isChecked == true ? 2 : isChecked == null ? 1 : 0;
        var knobColor = isChecked == true ? OnKnobColor : KnobColor;

        buffer.SetPixel(x, y, '[', BracketColor, bg);
        for (int i = 0; i < 3; i++)
        {
            if (i == knobIndex)
                buffer.SetPixel(x + 1 + i, y, KnobChar, knobColor, bg);
            else
                buffer.SetPixel(x + 1 + i, y, TrackChar, TrackColor, bg);
        }
        buffer.SetPixel(x + 4, y, ']', BracketColor, bg);

        int textX = x + TrackWidth;

        int labelWidth = StateLabelWidth;
        if (labelWidth > 0)
        {
            string label = StateLabel;
            for (int i = 0; i < label.Length; i++)
            {
                buffer.SetPixel(textX + 1 + i, y, label[i], fg, bg);
            }
            textX += 1 + labelWidth;
        }

        string content = Content?.ToString() ?? string.Empty;
        for (int i = 0; i < content.Length; i++)
        {
            buffer.SetPixel(textX + 1 + i, y, content[i], fg, bg);
        }
    }
}
