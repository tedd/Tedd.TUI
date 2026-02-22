using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class ScrollBarTest : TestPage
{
    public override string Name => "ScrollBar";
    public override string Description => "Standard ScrollBar Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var sb = new ScrollBar { Width = 30, Orientation = Orientation.Horizontal, Minimum = 0, Maximum = 100, ViewportSize = 10, Value = 0 };
        var output = new TextBlock { Text = "Value: 0" };

        sb.ValueChanged += (s, e) => output.Text = $"Value: {sb.Value}";

        funcPanel.AddChild(sb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Vertical
        var vPanel = new StackPanel { Orientation = Orientation.Vertical };
        var vSb = new ScrollBar { Height = 10, Orientation = Orientation.Vertical, Minimum = 0, Maximum = 50, ViewportSize = 5 };
        var vOut = new TextBlock { Text = "V Value: 0" };
        vSb.ValueChanged += (s, e) => vOut.Text = $"V Value: {vSb.Value}";

        var vRow = new StackPanel { Orientation = Orientation.Horizontal };
        vRow.AddChild(vSb);
        vRow.AddChild(new TextBlock { Text = "  " });
        vRow.AddChild(vOut);

        AddScenario("Vertical", vRow);
    }
}
