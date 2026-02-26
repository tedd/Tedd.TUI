using System;

namespace Tedd.TUI;

public class Slider : UIElement
{
    public Slider()
    {
        Focusable = true;
    }

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(int), typeof(Slider), 0);

    public int Minimum
    {
        get { return (int)GetValue(MinimumProperty); }
        set { SetValue(MinimumProperty, value); }
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(int), typeof(Slider), 10);

    public int Maximum
    {
        get { return (int)GetValue(MaximumProperty); }
        set { SetValue(MaximumProperty, value); }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(int), typeof(Slider), 0);

    public int Value
    {
        get { return (int)GetValue(ValueProperty); }
        set
        {
            // Clamp value
            int min = Minimum;
            int max = Maximum;
            if (value < min) value = min;
            if (value > max) value = max;
            SetValue(ValueProperty, value);
        }
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Slider), Orientation.Horizontal);

    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register("SmallChange", typeof(int), typeof(Slider), 1);

    public int SmallChange
    {
        get { return (int)GetValue(SmallChangeProperty); }
        set { SetValue(SmallChangeProperty, value); }
    }

    public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register("LargeChange", typeof(int), typeof(Slider), 5);

    public int LargeChange
    {
        get { return (int)GetValue(LargeChangeProperty); }
        set { SetValue(LargeChangeProperty, value); }
    }

    public static readonly DependencyProperty TrackColorProperty =
        DependencyProperty.Register("TrackColor", typeof(ConsoleColor), typeof(Slider), ConsoleColor.DarkGray);

    public ConsoleColor TrackColor
    {
        get { return (ConsoleColor)GetValue(TrackColorProperty); }
        set { SetValue(TrackColorProperty, value); }
    }

    public static readonly DependencyProperty ThumbColorProperty =
        DependencyProperty.Register("ThumbColor", typeof(ConsoleColor), typeof(Slider), ConsoleColor.White);

    public ConsoleColor ThumbColor
    {
        get { return (ConsoleColor)GetValue(ThumbColorProperty); }
        set { SetValue(ThumbColorProperty, value); }
    }

    public static readonly DependencyProperty FocusedThumbColorProperty =
        DependencyProperty.Register("FocusedThumbColor", typeof(ConsoleColor), typeof(Slider), ConsoleColor.Yellow);

    public ConsoleColor FocusedThumbColor
    {
        get { return (ConsoleColor)GetValue(FocusedThumbColorProperty); }
        set { SetValue(FocusedThumbColorProperty, value); }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Orientation == Orientation.Horizontal)
        {
            return new Size(10, 1);
        }
        else
        {
            return new Size(1, 10);
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        int range = Maximum - Minimum;
        if (range <= 0) range = 1;
        int val = Value - Minimum;

        // Ensure clamped for rendering
        if (val < 0) val = 0;
        if (val > range) val = range;

        ConsoleColor thumbColor = IsFocused ? FocusedThumbColor : ThumbColor;
        ConsoleColor bg = Background ?? ConsoleColor.Black;

        if (Orientation == Orientation.Horizontal)
        {
            // Draw Track
            for (int i = 0; i < w; i++)
            {
                buffer.SetPixel(x + i, y, '-', TrackColor, bg);
            }
            // Draw Thumb
            int thumbPos = (val * (w - 1)) / range;
            buffer.SetPixel(x + thumbPos, y, 'O', thumbColor, bg);
        }
        else
        {
            // Draw Track
            for (int i = 0; i < h; i++)
            {
                buffer.SetPixel(x, y + i, '|', TrackColor, bg);
            }
            // Draw Thumb
            // Usually bottom is min, top is max? Or top is min?
            // Windows standard: Top is min, Bottom is max (y increases downwards).
            // Let's stick to standard Y axis increase.
            int thumbPos = (val * (h - 1)) / range;
            buffer.SetPixel(x, y + thumbPos, 'O', thumbColor, bg);
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Orientation == Orientation.Horizontal)
        {
            if (e.Key == ConsoleKey.LeftArrow)
            {
                Value -= SmallChange;
                e.Handled = true;
            }
            else if (e.Key == ConsoleKey.RightArrow)
            {
                Value += SmallChange;
                e.Handled = true;
            }
        }
        else
        {
            if (e.Key == ConsoleKey.UpArrow)
            {
                Value -= SmallChange;
                e.Handled = true;
            }
            else if (e.Key == ConsoleKey.DownArrow)
            {
                Value += SmallChange;
                e.Handled = true;
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        int range = Maximum - Minimum;
        if (range <= 0) range = 1;

        int newVal = Value;

        if (Orientation == Orientation.Horizontal)
        {
            int w = RenderSize.Width;
            if (w > 1)
            {
                // Map x to value
                // thumbPos = (val * (w - 1)) / range
                // val = (thumbPos * range) / (w - 1)

                // e.X is local
                int clickX = Math.Max(0, Math.Min(w - 1, e.X));
                newVal = Minimum + (clickX * range) / (w - 1);
            }
        }
        else
        {
            int h = RenderSize.Height;
            if (h > 1)
            {
                int clickY = Math.Max(0, Math.Min(h - 1, e.Y));
                newVal = Minimum + (clickY * range) / (h - 1);
            }
        }

        Value = newVal;
        e.Handled = true;
    }
}
