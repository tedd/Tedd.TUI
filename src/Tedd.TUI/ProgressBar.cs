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

    public static readonly DependencyProperty LabelModeProperty =
        DependencyProperty.Register("LabelMode", typeof(ProgressBarLabelMode), typeof(ProgressBar), ProgressBarLabelMode.None);

    public ProgressBarLabelMode LabelMode
    {
        get { return (ProgressBarLabelMode)GetValue(LabelModeProperty); }
        set { SetValue(LabelModeProperty, value); }
    }

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register("LabelText", typeof(string), typeof(ProgressBar), null);

    public string LabelText
    {
        get { return (string)GetValue(LabelTextProperty); }
        set { SetValue(LabelTextProperty, value); }
    }

    public static readonly DependencyProperty LabelPercentDecimalsProperty =
        DependencyProperty.Register("LabelPercentDecimals", typeof(int), typeof(ProgressBar), 0);

    public int LabelPercentDecimals
    {
        get { return (int)GetValue(LabelPercentDecimalsProperty); }
        set { SetValue(LabelPercentDecimalsProperty, value); }
    }

    public static readonly DependencyProperty ProgressColorProperty =
        DependencyProperty.Register("ProgressColor", typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.Green);

    public ConsoleColor ProgressColor
    {
        get { return (ConsoleColor)GetValue(ProgressColorProperty); }
        set { SetValue(ProgressColorProperty, value); }
    }

    public static readonly DependencyProperty EmptyColorProperty =
        DependencyProperty.Register("EmptyColor", typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.DarkGray);

    public ConsoleColor EmptyColor
    {
        get { return (ConsoleColor)GetValue(EmptyColorProperty); }
        set { SetValue(EmptyColorProperty, value); }
    }

    public static readonly DependencyProperty LabelFilledColorProperty =
        DependencyProperty.Register("LabelFilledColor", typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.Black);

    public ConsoleColor LabelFilledColor
    {
        get { return (ConsoleColor)GetValue(LabelFilledColorProperty); }
        set { SetValue(LabelFilledColorProperty, value); }
    }

    public static readonly DependencyProperty LabelFilledBackgroundProperty =
        DependencyProperty.Register("LabelFilledBackground", typeof(ConsoleColor?), typeof(ProgressBar), null);

    public ConsoleColor? LabelFilledBackground
    {
        get { return (ConsoleColor?)GetValue(LabelFilledBackgroundProperty); }
        set { SetValue(LabelFilledBackgroundProperty, value); }
    }

    public static readonly DependencyProperty LabelEmptyColorProperty =
        DependencyProperty.Register("LabelEmptyColor", typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.White);

    public ConsoleColor LabelEmptyColor
    {
        get { return (ConsoleColor)GetValue(LabelEmptyColorProperty); }
        set { SetValue(LabelEmptyColorProperty, value); }
    }

    public static readonly DependencyProperty LabelEmptyBackgroundProperty =
        DependencyProperty.Register("LabelEmptyBackground", typeof(ConsoleColor?), typeof(ProgressBar), null);

    public ConsoleColor? LabelEmptyBackground
    {
        get { return (ConsoleColor?)GetValue(LabelEmptyBackgroundProperty); }
        set { SetValue(LabelEmptyBackgroundProperty, value); }
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

        // Calculate Label
        string text = "";
        if (LabelMode == ProgressBarLabelMode.Percent)
        {
            double percent = range == 0 ? 0 : ((double)val / range) * 100.0;
            text = percent.ToString("F" + LabelPercentDecimals) + "%";
        }
        else if (LabelMode == ProgressBarLabelMode.Text)
        {
            text = LabelText ?? "";
        }

        bool showText = !string.IsNullOrEmpty(text) && text.Length <= w;
        int textStart = 0;
        if (showText)
        {
            textStart = (w - text.Length) / 2;
        }

        for (int i = 0; i < w; i++)
        {
            // Determine if we are rendering text at this position
            bool isTextChar = false;
            char charToRender = ' ';
            if (showText && i >= textStart && i < textStart + text.Length)
            {
                isTextChar = true;
                charToRender = text[i - textStart];
            }

            if (i < filled)
            {
                // Filled section
                if (isTextChar)
                {
                    buffer.SetPixel(x + i, y, charToRender, LabelFilledColor, LabelFilledBackground ?? ProgressColor);
                }
                else
                {
                    buffer.SetPixel(x + i, y, '█', ProgressColor, ConsoleColor.Black);
                }
            }
            else
            {
                // Empty section
                if (isTextChar)
                {
                    buffer.SetPixel(x + i, y, charToRender, LabelEmptyColor, LabelEmptyBackground ?? ConsoleColor.Black);
                }
                else
                {
                    buffer.SetPixel(x + i, y, '░', EmptyColor, ConsoleColor.Black);
                }
            }
        }
    }
}
