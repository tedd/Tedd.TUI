using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class PanelZIndexTests
{
    private class TestPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(100, 100);
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children)
            {
                child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }
        }
    }

    [Fact]
    public void Panel_GetVisualChild_ReturnsChildrenSortedByZIndex()
    {
        var panel = new TestPanel();

        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };
        var el3 = new TextBlock { Text = "C" };

        // Add in order A, B, C
        panel.AddChild(el1);
        panel.AddChild(el2);
        panel.AddChild(el3);

        Panel.SetZIndex(el1, 10);
        Panel.SetZIndex(el2, -5);
        Panel.SetZIndex(el3, 0);

        // Expected order: B (-5), C (0), A (10)
        Assert.Equal(3, panel.VisualChildrenCount);
        Assert.Same(el2, panel.GetVisualChild(0));
        Assert.Same(el3, panel.GetVisualChild(1));
        Same(el1, panel.GetVisualChild(2));
    }

    [Fact]
    public void Panel_GetVisualChild_MaintainsDeclarationOrderForSameZIndex()
    {
        var panel = new TestPanel();

        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };
        var el3 = new TextBlock { Text = "C" };

        // Default ZIndex is 0
        panel.AddChild(el1);
        panel.AddChild(el2);
        panel.AddChild(el3);

        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el2, panel.GetVisualChild(1));
        Same(el3, panel.GetVisualChild(2));
    }

    [Fact]
    public void Panel_GetVisualChild_UpdatesWhenZIndexChanges()
    {
        var panel = new TestPanel();
        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };

        panel.AddChild(el1);
        panel.AddChild(el2);

        // Initially A then B
        Assert.Same(el1, panel.GetVisualChild(0));

        // Change B to be behind A
        Panel.SetZIndex(el2, -1);

        // Now B then A
        Assert.Same(el2, panel.GetVisualChild(0));
        Same(el1, panel.GetVisualChild(1));
    }

    private void Same(object expected, object actual)
    {
        Assert.Same(expected, actual);
    }
}
