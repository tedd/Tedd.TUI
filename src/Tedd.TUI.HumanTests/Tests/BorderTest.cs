using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class BorderTest : TestPage
{
    public override string Name => "Border";
    public override string Description => "Standard Border Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var b = new Border
        {
            Width = 20,
            Height = 5,
            BoxStyle = BoxStyle.Double,
            Child = new TextBlock { Text = "Double Border" }
        };

        funcPanel.AddChild(b);
        funcPanel.AddChild(new TextBlock { Text = " " });

        var b2 = new Border
        {
            Width = 20,
            Height = 5,
            BoxStyle = BoxStyle.Single,
            Child = new TextBlock { Text = "Single Border" }
        };
        funcPanel.AddChild(b2);

        AddScenario("Functionality", funcPanel);

        // 2. Padding (if supported)
        // Usually Border wraps child directly.
        // We can simulate padding with margins on child if supported.
        // Or if Border has Padding property.
        // Checking Border.cs: No Padding property usually in simple TUI.
        // But we can check alignment.

        var centered = new Border
        {
            Width = 20,
            Height = 5,
            BoxStyle = BoxStyle.Single,
            Child = new TextBlock
            {
                Text = "Center",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        AddScenario("Alignment", centered);

        // 3. Colors
        var colored = new Border
        {
            Width = 20,
            Height = 5,
            BoxStyle = BoxStyle.Double,
            BorderColor = ConsoleColor.Red,
            Background = ConsoleColor.Blue,
            Child = new TextBlock { Text = "Red/Blue", Foreground = ConsoleColor.Yellow }
        };

        AddScenario("Colors", colored);
    }
}
