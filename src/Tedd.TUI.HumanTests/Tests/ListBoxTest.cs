using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class ListBoxTest : TestPage
{
    public override string Name => "ListBox";
    public override string Description => "Standard ListBox Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var lb = new ListBox { Width = 30, Height = 10 };
        for (int i = 1; i <= 20; i++) lb.Items.Add($"Item {i}");
        lb.SelectedIndex = 0;

        var output = new TextBlock { Text = "Selected: Item 1" };

        lb.SelectionChanged += (s, e) =>
        {
            if (lb.SelectedIndex >= 0 && lb.SelectedIndex < lb.Items.Count)
                output.Text = $"Selected: {lb.Items[lb.SelectedIndex]}";
            else
                output.Text = "Selected: None";
        };

        funcPanel.AddChild(lb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Standard Layout Tests
        AddStandardScenarios(() => new ListBox { Width = 20, Height = 5, Items = { "A", "B", "C" } });

        // 3. Selection Styles
        var selectionPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var lb1 = new ListBox { Width = 15, Height = 5, ShowSelection = true };
        lb1.Items.Add("Always Visible");
        lb1.Items.Add("Item 2");
        lb1.SelectedIndex = 0;

        var lb2 = new ListBox { Width = 15, Height = 5, ShowSelection = false };
        lb2.Items.Add("Only Focused");
        lb2.Items.Add("Item 2");
        lb2.SelectedIndex = 0;

        selectionPanel.AddChild(lb1);
        selectionPanel.AddChild(new TextBlock { Text = "  " });
        selectionPanel.AddChild(lb2);

        AddScenario("Selection Modes", selectionPanel);
    }
}
