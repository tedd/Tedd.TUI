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

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _progressBar.Value = Value;
        _progressBar.Minimum = Minimum;
        _progressBar.Maximum = Maximum;
    }
}
