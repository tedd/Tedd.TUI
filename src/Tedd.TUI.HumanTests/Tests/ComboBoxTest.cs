using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class ComboBoxTest : TestPage
{
    public override string Name => "ComboBox";
    public override string Description => "ComboBox with Dropdown";

    protected override void AddScenarios()
    {
        // 1. Functionality
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var cb = new ComboBox { Width = 20 };
        cb.Items.Add("Item 1");
        cb.Items.Add("Item 2");
        cb.Items.Add("Item 3");
        cb.SelectedItem = "Item 1";

        var output = new TextBlock { Text = "Selected: Item 1" };

        // ComboBox usually has SelectionChanged event?
        // Checking ComboBox.cs... assuming it has PropertyChanged or similar.
        // We'll add a button to check state if event is missing.
        // But let's assume standard behavior.

        var btnCheck = new Button { Content = "Check Selection" };
        btnCheck.Click += (s, e) => output.Text = $"Selected: {cb.SelectedItem}";

        funcPanel.AddChild(cb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(btnCheck);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Many Items (Scrolling)
        var manyCb = new ComboBox { Width = 20 };
        for (int i = 1; i <= 50; i++) manyCb.Items.Add($"Item {i}");
        manyCb.SelectedItem = "Item 1";
        AddScenario("Many Items", manyCb);

        // 3. Constrained
        var constrained = new Border
        {
            Width = 15,
            Height = 5,
            BoxStyle = BoxStyle.Single,
            Child = new ComboBox { Width = 10, Items = { "A", "B", "C" }, SelectedItem = "A" }
        };
        AddScenario("Constrained", constrained);

        // 4. Bottom of Screen (Test Dropdown Direction)
        // We simulate this by placing it in a container that pushes it down.
        var bottomPanel = new StackPanel { Orientation = Orientation.Vertical };
        bottomPanel.AddChild(new TextBlock { Text = "Spacer", Height = 15 }); // Push down
        var bottomCb = new ComboBox { Width = 20, Items = { "Up?", "Down?" }, SelectedItem = "Up?" };
        bottomPanel.AddChild(bottomCb);

        // Note: In a real TuiWindow, if it's at the bottom, does it open up?
        // ComboBox implementation usually checks available space.

        AddScenario("Bottom Placement", bottomPanel);
    }
}
