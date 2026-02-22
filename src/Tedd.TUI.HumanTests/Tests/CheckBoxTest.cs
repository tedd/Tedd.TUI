using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class CheckBoxTest : TestPage
{
    public override string Name => "CheckBox";
    public override string Description => "Standard CheckBox Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var cb = new CheckBox { Content = "Check Me", IsChecked = false };
        var output = new TextBlock { Text = "Result: Unchecked" };

        // Assuming CheckBox has Checked/Unchecked events or Click
        cb.IsChecked = false;

        // CheckBox does not expose Click event in this version.
        // It toggles on click internally.

        var btnCheck = new Button { Content = "Check Status" };
        btnCheck.Click += (s, e) =>
        {
            output.Text = $"Result: {(cb.IsChecked ? "Checked" : "Unchecked")}";
        };

        funcPanel.AddChild(cb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(btnCheck);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Standard Layout Tests
        AddStandardScenarios(() => new CheckBox { Content = "Test CheckBox" });

        AddBindingScenario("Data Binding",
            new CheckBox { Content = "Bound CheckBox" },
            CheckBox.IsCheckedProperty,
            nameof(TestViewModel.IsChecked),
            vm => vm.IsChecked = !vm.IsChecked);
    }
}
