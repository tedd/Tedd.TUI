using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class StackPanelTest : TestPage
{
    public override string Name => "StackPanel";
    public override string Description => "Standard StackPanel Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test (Vertical)
        var vPanel = new StackPanel { Orientation = Orientation.Vertical };
        vPanel.AddChild(new Button { Content = "Top" });
        vPanel.AddChild(new TextBlock { Text = "Middle" });
        vPanel.AddChild(new Button { Content = "Bottom" });

        AddScenario("Vertical", vPanel);

        // 2. Horizontal
        var hPanel = new StackPanel { Orientation = Orientation.Horizontal };
        hPanel.AddChild(new Button { Content = "Left" });
        hPanel.AddChild(new TextBlock { Text = " Middle " });
        hPanel.AddChild(new Button { Content = "Right" });

        AddScenario("Horizontal", hPanel);

        // 3. Nested
        var nested = new StackPanel { Orientation = Orientation.Vertical };
        nested.AddChild(new TextBlock { Text = "Header" });

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.AddChild(new Button { Content = "A" });
        row.AddChild(new TextBlock { Text = " " });
        row.AddChild(new Button { Content = "B" });

        nested.AddChild(row);
        nested.AddChild(new TextBlock { Text = "Footer" });

        AddScenario("Nested", nested);
    }
}
