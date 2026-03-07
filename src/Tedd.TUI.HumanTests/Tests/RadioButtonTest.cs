using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class RadioButtonTest : TestPage
{
    public override string Name => "RadioButton";
    public override string Description => "Standard RadioButton Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var groupName = "TestGroup";

        var rb1 = new RadioButton { Content = "Option 1", GroupName = groupName, IsChecked = true };
        var rb2 = new RadioButton { Content = "Option 2", GroupName = groupName };
        var rb3 = new RadioButton { Content = "Option 3", GroupName = groupName };

        var output = new TextBlock { Text = "Selected: Option 1" };

        var btnCheck = new Button { Content = "Check Selection" };
        btnCheck.Click += (s, e) =>
        {
            if (rb1.IsChecked == true) output.Text = "Selected: Option 1";
            else if (rb2.IsChecked == true) output.Text = "Selected: Option 2";
            else if (rb3.IsChecked == true) output.Text = "Selected: Option 3";
        };

        funcPanel.AddChild(rb1);
        funcPanel.AddChild(rb2);
        funcPanel.AddChild(rb3);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(btnCheck);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Standard Layout Tests
        AddStandardScenarios(() => new RadioButton { Content = "Test RadioButton" });
    }
}
