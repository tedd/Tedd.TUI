using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class ExpanderTests
{
    [Fact]
    public void Expander_DefaultState_IsCollapsed()
    {
        var expander = new Expander();
        Assert.False(expander.IsExpanded);
    }

    [Fact]
    public void Expander_Measure_Collapsed_OnlyMeasuresHeader()
    {
        var expander = new Expander
        {
            Header = "Options",
            Content = new TextBlock { Text = "Line 1\nLine 2\nLine 3" }
        };

        expander.Measure(new Size(80, 24));

        Assert.Equal(3, expander.DesiredSize.Height);
    }

    [Fact]
    public void Expander_Measure_Expanded_MeasuresHeaderAndContent()
    {
        var expander = new Expander
        {
            Header = "Options",
            Content = new TextBlock { Text = "Line 1\nLine 2\nLine 3" },
            IsExpanded = true
        };

        expander.Measure(new Size(80, 24));

        Assert.True(expander.DesiredSize.Height > 3, $"Expected height > 3, but was {expander.DesiredSize.Height}");
    }

    [Fact]
    public void Expander_Toggle_By_Keyboard_Enter()
    {
        var expander = new Expander();
        Assert.False(expander.IsExpanded);

        expander.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });

        Assert.True(expander.IsExpanded);

        expander.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });

        Assert.False(expander.IsExpanded);
    }

    [Fact]
    public void Expander_Toggle_By_Keyboard_Space()
    {
        var expander = new Expander();
        Assert.False(expander.IsExpanded);

        expander.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Spacebar });

        Assert.True(expander.IsExpanded);
    }

    [Fact]
    public void MouseClick_NestedExpanders_TogglesOnlyClickedHeader()
    {
        var first = new Expander
        {
            Header = "First",
            Content = new TextBlock { Text = "First content" }
        };
        var second = new Expander
        {
            Header = "Second",
            Content = new TextBlock { Text = "Second content" }
        };
        var panel = new StackPanel();
        panel.AddChild(new TextBlock { Text = "Expand sections" });
        panel.AddChild(first);
        panel.AddChild(new TextBlock { Text = "between" });
        panel.AddChild(second);
        panel.AddChild(new TextBlock { Text = "footer" });
        var host = new ControlTestHost(new Border { Child = panel }, 24, 16);

        host.Click(first, 2, 0);
        Assert.True(first.IsExpanded);
        Assert.False(second.IsExpanded);

        host.Click(second, 2, 0);
        Assert.True(first.IsExpanded);
        Assert.True(second.IsExpanded);

        host.Click(first, 2, 0);
        Assert.False(first.IsExpanded);
        Assert.True(second.IsExpanded);
    }

    [Fact]
    public void Expander_Mouse_Click_On_Content_Does_Not_Toggle()
    {
        var expander = new Expander
        {
            Header = "Options",
            Content = new TextBlock { Text = "Content" },
            IsExpanded = true
        };

        expander.Measure(new Size(80, 24));
        expander.Arrange(new Rect(0, 0, expander.DesiredSize.Width, expander.DesiredSize.Height));

        Assert.True(expander.IsExpanded);

        expander.OnMouseDown(new MouseEventArgs { Y = 4 });

        Assert.True(expander.IsExpanded);
    }

    [Fact]
    public void Expander_Events_Fired_On_Toggle()
    {
        var expander = new Expander();
        bool expandedFired = false;
        bool collapsedFired = false;

        expander.Expanded += (s, e) => expandedFired = true;
        expander.Collapsed += (s, e) => collapsedFired = true;

        expander.IsExpanded = true;
        Assert.True(expandedFired);
        Assert.False(collapsedFired);

        expandedFired = false;
        expander.IsExpanded = false;
        Assert.True(collapsedFired);
        Assert.False(expandedFired);
    }
}
