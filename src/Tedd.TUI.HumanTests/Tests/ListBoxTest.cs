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
        // Two list boxes side-by-side comparing ShowSelection true vs false.
        // Click any item in either list to update the corresponding output label.
        var lb1 = new ListBox { Width = 18, Height = 5, ShowSelection = true };
        lb1.Items.Add("Always Visible");
        lb1.Items.Add("Item 2");
        lb1.Items.Add("Item 3");
        lb1.SelectedIndex = 0;

        var lb2 = new ListBox { Width = 18, Height = 5, ShowSelection = false };
        lb2.Items.Add("Only Focused");
        lb2.Items.Add("Item 2");
        lb2.Items.Add("Item 3");
        lb2.SelectedIndex = 0;

        var lb1Output = new TextBlock { Text = "lb1 selected: Always Visible" };
        var lb2Output = new TextBlock { Text = "lb2 selected: Only Focused" };

        lb1.SelectionChanged += (s, e) =>
        {
            lb1Output.Text = lb1.SelectedIndex >= 0
                ? $"lb1 selected: {lb1.Items[lb1.SelectedIndex]}"
                : "lb1 selected: None";
        };
        lb2.SelectionChanged += (s, e) =>
        {
            lb2Output.Text = lb2.SelectedIndex >= 0
                ? $"lb2 selected: {lb2.Items[lb2.SelectedIndex]}"
                : "lb2 selected: None";
        };

        var lb1Stack = new StackPanel { Orientation = Orientation.Vertical };
        lb1Stack.AddChild(new TextBlock { Text = "ShowSelection = true" });
        lb1Stack.AddChild(lb1);

        var lb2Stack = new StackPanel { Orientation = Orientation.Vertical };
        lb2Stack.AddChild(new TextBlock { Text = "ShowSelection = false" });
        lb2Stack.AddChild(lb2);

        var selectionRow = new StackPanel { Orientation = Orientation.Horizontal };
        selectionRow.AddChild(lb1Stack);
        selectionRow.AddChild(new TextBlock { Text = "  " });
        selectionRow.AddChild(lb2Stack);

        var selectionPanel = new StackPanel { Orientation = Orientation.Vertical };
        selectionPanel.AddChild(selectionRow);
        selectionPanel.AddChild(new TextBlock { Text = " " });
        selectionPanel.AddChild(lb1Output);
        selectionPanel.AddChild(lb2Output);

        AddScenario("Selection Modes", selectionPanel);
    }
}
