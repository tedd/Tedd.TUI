using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiProgressBar : TuiComponentBase
{
    private ProgressBar _progressBar = new ProgressBar();
    public override UIElement Element => _progressBar;

    [Parameter] public int Value { get; set; }
    [Parameter] public int Minimum { get; set; } = 0;
    [Parameter] public int Maximum { get; set; } = 100;

    [Parameter] public ProgressBarLabelMode LabelMode { get; set; } = ProgressBarLabelMode.None;
    [Parameter] public string LabelText { get; set; }
    [Parameter] public int LabelPercentDecimals { get; set; } = 0;

    [Parameter] public ConsoleColor ProgressColor { get; set; } = ConsoleColor.Green;
    [Parameter] public ConsoleColor EmptyColor { get; set; } = ConsoleColor.DarkGray;
    [Parameter] public ConsoleColor LabelFilledColor { get; set; } = ConsoleColor.Black;
    [Parameter] public ConsoleColor? LabelFilledBackground { get; set; }
    [Parameter] public ConsoleColor LabelEmptyColor { get; set; } = ConsoleColor.White;
    [Parameter] public ConsoleColor? LabelEmptyBackground { get; set; }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _progressBar.Value = Value;
        _progressBar.Minimum = Minimum;
        _progressBar.Maximum = Maximum;

        _progressBar.LabelMode = LabelMode;
        _progressBar.LabelText = LabelText;
        _progressBar.LabelPercentDecimals = LabelPercentDecimals;

        _progressBar.ProgressColor = ProgressColor;
        _progressBar.EmptyColor = EmptyColor;
        _progressBar.LabelFilledColor = LabelFilledColor;
        _progressBar.LabelFilledBackground = LabelFilledBackground;
        _progressBar.LabelEmptyColor = LabelEmptyColor;
        _progressBar.LabelEmptyBackground = LabelEmptyBackground;
    }
}
