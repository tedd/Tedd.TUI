using System;

namespace Tedd.TUI;

public class ProgressBar : UIElement
{
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(ProgressBar), 0);

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(ProgressBar), 100);

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(ProgressBar), 0);

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty LabelModeProperty =
        DependencyProperty.Register(nameof(LabelMode), typeof(ProgressBarLabelMode), typeof(ProgressBar), ProgressBarLabelMode.None);

    public ProgressBarLabelMode LabelMode
    {
        get => (ProgressBarLabelMode)GetValue(LabelModeProperty);
        set => SetValue(LabelModeProperty, value);
    }

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(ProgressBar), null);

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public static readonly DependencyProperty LabelPercentDecimalsProperty =
        DependencyProperty.Register(nameof(LabelPercentDecimals), typeof(int), typeof(ProgressBar), 0);

    public int LabelPercentDecimals
    {
        get => (int)GetValue(LabelPercentDecimalsProperty);
        set => SetValue(LabelPercentDecimalsProperty, value);
    }

    public static readonly DependencyProperty ProgressColorProperty =
        DependencyProperty.Register(nameof(ProgressColor), typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.Green);

    public ConsoleColor ProgressColor
    {
        get => (ConsoleColor)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public static readonly DependencyProperty EmptyColorProperty =
        DependencyProperty.Register(nameof(EmptyColor), typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.DarkGray);

    public ConsoleColor EmptyColor
    {
        get => (ConsoleColor)GetValue(EmptyColorProperty);
        set => SetValue(EmptyColorProperty, value);
    }

    public static readonly DependencyProperty LabelFilledColorProperty =
        DependencyProperty.Register(nameof(LabelFilledColor), typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.Black);

    public ConsoleColor LabelFilledColor
    {
        get => (ConsoleColor)GetValue(LabelFilledColorProperty);
        set => SetValue(LabelFilledColorProperty, value);
    }

    public static readonly DependencyProperty LabelFilledBackgroundProperty =
        DependencyProperty.Register(nameof(LabelFilledBackground), typeof(ConsoleColor?), typeof(ProgressBar), null);

    public ConsoleColor? LabelFilledBackground
    {
        get => (ConsoleColor?)GetValue(LabelFilledBackgroundProperty);
        set => SetValue(LabelFilledBackgroundProperty, value);
    }

    public static readonly DependencyProperty LabelEmptyColorProperty =
        DependencyProperty.Register(nameof(LabelEmptyColor), typeof(ConsoleColor), typeof(ProgressBar), ConsoleColor.White);

    public ConsoleColor LabelEmptyColor
    {
        get => (ConsoleColor)GetValue(LabelEmptyColorProperty);
        set => SetValue(LabelEmptyColorProperty, value);
    }

    public static readonly DependencyProperty LabelEmptyBackgroundProperty =
        DependencyProperty.Register(nameof(LabelEmptyBackground), typeof(ConsoleColor?), typeof(ProgressBar), null);

    public ConsoleColor? LabelEmptyBackground
    {
        get => (ConsoleColor?)GetValue(LabelEmptyBackgroundProperty);
        set => SetValue(LabelEmptyBackgroundProperty, value);
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
