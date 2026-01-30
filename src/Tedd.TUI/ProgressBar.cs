using System;

namespace Tedd.TUI;

public class ProgressBar : UIElement
{
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(int), typeof(ProgressBar), 0);

    public int Minimum
    {
        get { return (int)GetValue(MinimumProperty); }
        set { SetValue(MinimumProperty, value); }
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(int), typeof(ProgressBar), 100);

    public int Maximum
    {
        get { return (int)GetValue(MaximumProperty); }
        set { SetValue(MaximumProperty, value); }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(int), typeof(ProgressBar), 0);

    public int Value
    {
        get { return (int)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Default width if not specified
        return new Size(20, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;

        int range = Maximum - Minimum;
        if (range <= 0) range = 1;
        int val = Value - Minimum;
        if (val < 0) val = 0;
        if (val > range) val = range;

        int filled = (val * w) / range;

        for (int i = 0; i < w; i++)
        {
            if (i < filled)
            {
                buffer.SetPixel(x + i, y, '█', ConsoleColor.Green, ConsoleColor.Black);
            }
            else
            {
                buffer.SetPixel(x + i, y, '░', ConsoleColor.DarkGray, ConsoleColor.Black);
            }
        }
    }
}
