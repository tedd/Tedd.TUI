using System;

namespace Tedd.TUI;

public class ProgressBar : UIElement
{
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(int), typeof(ProgressBar), 0);

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(int), typeof(ProgressBar), 100);

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(int), typeof(ProgressBar), 0);

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty LabelModeProperty =
        DependencyProperty.Register("LabelMode", typeof(ProgressBarLabelMode), typeof(ProgressBar), ProgressBarLabelMode.None);

    public ProgressBarLabelMode LabelMode
    {
        get => (ProgressBarLabelMode)GetValue(LabelModeProperty);
        set => SetValue(LabelModeProperty, value);
    }

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register("LabelText", typeof(string), typeof(ProgressBar), null);

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public static readonly DependencyProperty LabelPercentDecimalsProperty =
        DependencyProperty.Register("LabelPercentDecimals", typeof(int), typeof(ProgressBar), 0);

    public int LabelPercentDecimals
    {
        get => (int)GetValue(LabelPercentDecimalsProperty);
        set => SetValue(LabelPercentDecimalsProperty, value);
    }

    public static readonly DependencyProperty ProgressColorProperty =
        DependencyProperty.Register("ProgressColor", typeof(TuiColor), typeof(ProgressBar), TuiColor.Green);

    public TuiColor ProgressColor
    {
        get => (TuiColor)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public static readonly DependencyProperty EmptyColorProperty =
        DependencyProperty.Register("EmptyColor", typeof(TuiColor), typeof(ProgressBar), TuiColor.DarkGray);

    public TuiColor EmptyColor
    {
        get => (TuiColor)GetValue(EmptyColorProperty);
        set => SetValue(EmptyColorProperty, value);
    }

    public static readonly DependencyProperty LabelFilledColorProperty =
        DependencyProperty.Register("LabelFilledColor", typeof(TuiColor), typeof(ProgressBar), TuiColor.Black);

    public TuiColor LabelFilledColor
    {
        get => (TuiColor)GetValue(LabelFilledColorProperty);
        set => SetValue(LabelFilledColorProperty, value);
    }

    public static readonly DependencyProperty LabelFilledBackgroundProperty =
        DependencyProperty.Register("LabelFilledBackground", typeof(TuiColor?), typeof(ProgressBar), null);

    public TuiColor? LabelFilledBackground
    {
        get => (TuiColor?)GetValue(LabelFilledBackgroundProperty);
        set => SetValue(LabelFilledBackgroundProperty, value);
    }

    public static readonly DependencyProperty LabelEmptyColorProperty =
        DependencyProperty.Register("LabelEmptyColor", typeof(TuiColor), typeof(ProgressBar), TuiColor.White);

    public TuiColor LabelEmptyColor
    {
        get => (TuiColor)GetValue(LabelEmptyColorProperty);
        set => SetValue(LabelEmptyColorProperty, value);
    }

    public static readonly DependencyProperty LabelEmptyBackgroundProperty =
        DependencyProperty.Register("LabelEmptyBackground", typeof(TuiColor?), typeof(ProgressBar), null);

    public TuiColor? LabelEmptyBackground
    {
        get => (TuiColor?)GetValue(LabelEmptyBackgroundProperty);
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
                    buffer.SetPixel(x + i, y, '█', ProgressColor, TuiColor.Black);
                }
            }
            else
            {
                // Empty section
                if (isTextChar)
                {
                    buffer.SetPixel(x + i, y, charToRender, LabelEmptyColor, LabelEmptyBackground ?? TuiColor.Black);
                }
                else
                {
                    buffer.SetPixel(x + i, y, '░', EmptyColor, TuiColor.Black);
                }
            }
        }
    }
}
