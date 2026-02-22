using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class ProgressBarTest : TestPage
{
    public override string Name => "ProgressBar";
    public override string Description => "Standard ProgressBar Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var pb = new ProgressBar { Width = 30, Value = 0, Maximum = 100 };
        var output = new TextBlock { Text = "Value: 0" };

        var btnPlus = new Button { Content = "+" };
        btnPlus.Click += (s, e) =>
        {
            pb.Value = Math.Min(pb.Maximum, pb.Value + 10);
            output.Text = $"Value: {pb.Value}";
        };

        var btnMinus = new Button { Content = "-" };
        btnMinus.Click += (s, e) =>
        {
            pb.Value = Math.Max(0, pb.Value - 10);
            output.Text = $"Value: {pb.Value}";
        };

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        btnPanel.AddChild(btnMinus);
        btnPanel.AddChild(new TextBlock { Text = "  " });
        btnPanel.AddChild(btnPlus);

        funcPanel.AddChild(pb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(btnPanel);
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Styles
        // Assuming LabelMode property exists based on memory/files.
        // Let's verify ProgressBar.cs later if needed, but usually it has it.
        // Assuming default works.

        AddScenario("Default", new ProgressBar { Width = 20, Value = 50 });

        AddBindingScenario("Data Binding",
            new ProgressBar { Width = 20, Maximum = 100 },
            ProgressBar.ValueProperty,
            nameof(TestViewModel.Value),
            vm => vm.Value = (vm.Value + 25) % 100);
    }
}
