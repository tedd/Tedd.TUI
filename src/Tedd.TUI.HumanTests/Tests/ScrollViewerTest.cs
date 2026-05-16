using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class ScrollViewerTest : TestPage
{
    public override string Name => "ScrollViewer";
    public override string Description => "Standard ScrollViewer Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var sv = new ScrollViewer { Width = 40, Height = 10, VerticalScrollBarVisibility = ScrollBarVisibility.Visible, HorizontalScrollBarVisibility = ScrollBarVisibility.Visible };

        var content = new StackPanel { Orientation = Orientation.Vertical };
        for (int i = 0; i < 30; i++)
        {
            content.AddChild(new TextBlock { Text = $"Line {i} - This is a very long line to test horizontal scrolling if needed." });
        }

        sv.Content = content;

        funcPanel.AddChild(sv);

        AddScenario("Functionality", funcPanel);

        // 2. Nested Scroll (Not recommended but possible)
        // Usually ScrollViewer inside ScrollViewer is confusing, but valid to test clipping.
        var outerSv = new ScrollViewer { Width = 30, Height = 15, VerticalScrollBarVisibility = ScrollBarVisibility.Visible };
        var innerSv = new ScrollViewer { Width = 20, Height = 8, VerticalScrollBarVisibility = ScrollBarVisibility.Visible };

        var innerContent = new StackPanel { Orientation = Orientation.Vertical };
        for (int i = 0; i < 20; i++) innerContent.AddChild(new TextBlock { Text = $"Inner {i}" });
        innerSv.Content = innerContent;

        var outerContent = new StackPanel { Orientation = Orientation.Vertical };
        outerContent.AddChild(new TextBlock { Text = "Above Inner" });
        outerContent.AddChild(innerSv);
        outerContent.AddChild(new TextBlock { Text = "Below Inner" });
        for (int i = 0; i < 10; i++) outerContent.AddChild(new TextBlock { Text = $"Outer {i}" });

        outerSv.Content = outerContent;

        AddScenario("Nested Scroll", outerSv);
    }
}
