using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class ButtonTest : TestPage
{
    public override string Name => "Button";
    public override string Description => "Standard Button Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var btn = new Button { Content = "Click Me" };
        var output = new TextBlock { Text = "Result: " };
        btn.Click += (s, e) => output.Text = $"Clicked at {DateTime.Now:mm:ss}";

        funcPanel.AddChild(btn);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Standard Layout Tests
        AddStandardScenarios(() => new Button { Content = "Test Button" });

        // 3. Different Styles
        var stylesPanel = new StackPanel { Orientation = Orientation.Vertical };
        stylesPanel.AddChild(new Button { Content = "Single Border", BoxStyle = BoxStyle.Single });
        stylesPanel.AddChild(new Button { Content = "Double Border", BoxStyle = BoxStyle.Double });
        stylesPanel.AddChild(new Button { Content = "Heavy Border", BoxStyle = BoxStyle.Heavy });

        AddScenario("Styles", stylesPanel);

        AddBindingScenario("Data Binding",
            new Button { Content = "Initial" },
            Button.ContentProperty,
            nameof(TestViewModel.Text),
            vm => vm.Text = "Updated via Binding!");
    }
}
