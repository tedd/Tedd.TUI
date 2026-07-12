using System;

namespace Tedd.TUI;

/// <summary>
/// A numeric spinner equivalent to the Avalonia <c>NumericUpDown</c>, WinUI <c>NumberBox</c>
/// (spin-button mode) and MAUI <c>Stepper</c>. Renders as <c>[-]  42 [+]</c>: clicking the
/// minus/plus buttons or pressing Up/Down (or the '+'/'-' keys) changes <see cref="Value"/>
/// by <see cref="Increment"/>, clamped to <see cref="Minimum"/>..<see cref="Maximum"/>.
/// </summary>
public class NumericUpDown : UIElement
{
    // "[-]" and "[+]" spin buttons, 3 cells each.
    private const int ButtonWidth = 3;

    public NumericUpDown()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumericUpDown), 0);

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericUpDown), 100);

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty IncrementProperty =
        DependencyProperty.Register(nameof(Increment), typeof(int), typeof(NumericUpDown), 1);

    /// <summary>Step applied per spin (button click or Up/Down key).</summary>
    public int Increment
    {
        get => (int)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericUpDown), 0);

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set
        {
            int min = Minimum;
            int max = Maximum;
            if (value < min) value = min;
            if (value > max) value = max;

            if (Value != value)
            {
                SetValue(ValueProperty, value);
            }
        }
    }

    public static readonly RoutedEvent ValueChangedEvent =
        RoutedEvent.Register("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NumericUpDown));

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public static readonly DependencyProperty ButtonColorProperty =
        DependencyProperty.Register(nameof(ButtonColor), typeof(TuiColor), typeof(NumericUpDown), TuiColor.Gray);

    /// <summary>Color of the <c>[-]</c> / <c>[+]</c> spin buttons.</summary>
    public TuiColor ButtonColor
    {
        get => (TuiColor)GetValue(ButtonColorProperty);
        set => SetValue(ButtonColorProperty, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register(nameof(FocusedForeground), typeof(TuiColor), typeof(NumericUpDown), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty property)
    {
        base.OnPropertyChanged(property);

        if (property == ValueProperty)
        {
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        }
        else if (property == MinimumProperty || property == MaximumProperty)
        {
            // Re-clamp through the property setter so a now-out-of-range value snaps back.
            Value = Value;
        }
    }

    private int ValueFieldWidth =>
        Math.Max(Minimum.ToString().Length, Maximum.ToString().Length);

    protected override Size MeasureOverride(Size availableSize)
    {
        // [-] 42 [+] -> buttons + one space padding on each side of the value field
        return new Size(ButtonWidth + 1 + ValueFieldWidth + 1 + ButtonWidth, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        if (w < ButtonWidth * 2 + 1) return;

        var bg = Background ?? buffer.GetPixel(x, y).Background;
        var buttonColor = ButtonColor;
        var valueColor = IsFocused ? FocusedForeground : Foreground;

        buffer.SetPixel(x, y, '[', buttonColor, bg);
        buffer.SetPixel(x + 1, y, '-', buttonColor, bg);
        buffer.SetPixel(x + 2, y, ']', buttonColor, bg);

        buffer.SetPixel(x + w - 3, y, '[', buttonColor, bg);
        buffer.SetPixel(x + w - 2, y, '+', buttonColor, bg);
        buffer.SetPixel(x + w - 1, y, ']', buttonColor, bg);

        // Value right-aligned in the field between the buttons, one cell of padding each side.
        string text = Value.ToString();
        int fieldStart = x + ButtonWidth + 1;
        int fieldEnd = x + w - ButtonWidth - 2; // inclusive
        int textStart = fieldEnd - text.Length + 1;
        for (int cx = fieldStart; cx <= fieldEnd; cx++)
        {
            char c = cx >= textStart && cx - textStart < text.Length ? text[cx - textStart] : ' ';
            buffer.SetPixel(cx, y, c, valueColor, bg);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnMouseDown(e);
        Focus();

        int w = RenderSize.Width;
        if (e.X < ButtonWidth)
        {
            Value -= Increment;
        }
        else if (e.X >= Math.Max(ButtonWidth, w - ButtonWidth))
        {
            Value += Increment;
        }

        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled) return;
        base.OnKeyDown(e);

        if (e.Key == ConsoleKey.UpArrow || e.Key == ConsoleKey.Add || e.KeyChar == '+')
        {
            Value += Increment;
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow || e.Key == ConsoleKey.Subtract || e.KeyChar == '-')
        {
            Value -= Increment;
            e.Handled = true;
        }
    }
}
