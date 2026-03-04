using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class TabControlTest : TestPage
{
    public override string Name => "TabControl";
    public override string Description => "Standard TabControl Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var tabs = new TabControl { Width = 40, Height = 10 };
        tabs.Items.Add(new TabItem { Header = "Tab 1", Content = new TextBlock { Text = "Content 1" } });
        tabs.Items.Add(new TabItem { Header = "Tab 2", Content = new Button { Content = "Click Me in Tab 2" } });

        funcPanel.AddChild(tabs);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(new TextBlock { Text = "Press Left/Right to switch tabs." });

        AddScenario("Functionality", funcPanel);

        // 2. Nested Tabs
        var outerTabs = new TabControl { Width = 50, Height = 15 };

        var innerTabs1 = new TabControl { Width = 40, Height = 10 };
        innerTabs1.Items.Add(new TabItem { Header = "Inner 1", Content = new TextBlock { Text = "Deep 1" } });
        innerTabs1.Items.Add(new TabItem { Header = "Inner 2", Content = new TextBlock { Text = "Deep 2" } });

        outerTabs.Items.Add(new TabItem { Header = "Outer 1", Content = innerTabs1 });
        outerTabs.Items.Add(new TabItem { Header = "Outer 2", Content = new TextBlock { Text = "Just Content" } });

        AddScenario("Nested Tabs", outerTabs);

        // 3. Many Tabs
        var manyTabs = new TabControl { Width = 60, Height = 10 };
        for (int i = 1; i <= 10; i++)
        {
            manyTabs.Items.Add(new TabItem { Header = $"Tab {i}", Content = new TextBlock { Text = $"Content {i}" } });
        }

        AddScenario("Many Tabs", manyTabs);
    }
}
