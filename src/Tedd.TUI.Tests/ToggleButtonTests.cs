using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class ToggleButtonTests
{
    [Fact]
    public void Click_NestedToggleButtons_TogglesOnlyTargetButton()
    {
        var first = new ToggleButton { Content = "First" };
        var second = new ToggleButton { Content = "Second" };
        var firstClicks = 0;
        var secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        var toggles = new StackPanel();
        toggles.AddChild(first);
        toggles.AddChild(new TextBlock { Text = " spacer " });
        toggles.AddChild(second);
        var surface = new Border { Content = toggles };
        var host = new ControlTestHost(surface, 24, 7);

        host.Click(first, 1, 0);

        Assert.True(first.IsChecked);
        Assert.False(second.IsChecked);
        Assert.Equal(1, firstClicks);
        Assert.Equal(0, secondClicks);

        host.Click(second, 1, 0);

        Assert.True(first.IsChecked);
        Assert.True(second.IsChecked);
        Assert.Equal(1, firstClicks);
        Assert.Equal(1, secondClicks);
    }
}
