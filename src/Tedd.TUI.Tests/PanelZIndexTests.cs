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

    // --- Cache-invalidation tests: build cache first, then mutate Children ---

    [Fact]
    public void Panel_GetVisualChild_AfterAdd_ReflectsNewChild()
    {
        var panel = new TestPanel();
        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };

        panel.AddChild(el1);
        panel.AddChild(el2);
        Panel.SetZIndex(el1, 1);
        Panel.SetZIndex(el2, 2);

        // Build cache: order should be el1(1), el2(2)
        Assert.Equal(2, panel.VisualChildrenCount);
        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el2, panel.GetVisualChild(1));

        // Add a new child with ZIndex 0 (sorts first)
        var el3 = new TextBlock { Text = "C" };
        Panel.SetZIndex(el3, 0);
        panel.AddChild(el3);

        // Cache must be rebuilt: order el3(0), el1(1), el2(2)
        Assert.Equal(3, panel.VisualChildrenCount);
        Assert.Same(el3, panel.GetVisualChild(0));
        Assert.Same(el1, panel.GetVisualChild(1));
        Assert.Same(el2, panel.GetVisualChild(2));
    }

    [Fact]
    public void Panel_GetVisualChild_AfterInsert_ReflectsInsertedChild()
    {
        var panel = new TestPanel();
        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };

        panel.AddChild(el1);
        panel.AddChild(el2);
        Panel.SetZIndex(el1, 0);
        Panel.SetZIndex(el2, 2);

        // Build cache: order el1(0), el2(2)
        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el2, panel.GetVisualChild(1));

        // Insert a child with ZIndex 1 (between el1 and el2)
        var el3 = new TextBlock { Text = "C" };
        Panel.SetZIndex(el3, 1);
        panel.Children.Insert(1, el3);

        // Cache must be rebuilt: order el1(0), el3(1), el2(2)
        Assert.Equal(3, panel.VisualChildrenCount);
        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el3, panel.GetVisualChild(1));
        Assert.Same(el2, panel.GetVisualChild(2));
    }

    [Fact]
    public void Panel_GetVisualChild_AfterRemove_DoesNotReturnStaleEntry()
    {
        var panel = new TestPanel();
        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };
        var el3 = new TextBlock { Text = "C" };

        panel.AddChild(el1);
        panel.AddChild(el2);
        panel.AddChild(el3);
        Panel.SetZIndex(el1, 1);
        Panel.SetZIndex(el2, 2);
        Panel.SetZIndex(el3, 3);

        // Build cache: order el1(1), el2(2), el3(3)
        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el2, panel.GetVisualChild(1));
        Assert.Same(el3, panel.GetVisualChild(2));

        // Remove the middle child
        panel.Children.Remove(el2);

        // Cache must be rebuilt: order el1(1), el3(3)
        Assert.Equal(2, panel.VisualChildrenCount);
        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el3, panel.GetVisualChild(1));
    }

    [Fact]
    public void Panel_GetVisualChild_AfterSet_ReflectsReplacedChild()
    {
        var panel = new TestPanel();
        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };

        panel.AddChild(el1);
        panel.AddChild(el2);
        Panel.SetZIndex(el1, 0);
        Panel.SetZIndex(el2, 1);

        // Build cache: order el1(0), el2(1)
        Assert.Same(el1, panel.GetVisualChild(0));
        Assert.Same(el2, panel.GetVisualChild(1));

        // Replace el1 (index 0 in Children) with el3 that has a higher ZIndex
        var el3 = new TextBlock { Text = "C" };
        Panel.SetZIndex(el3, 5);
        panel.Children[0] = el3;

        // Cache must be rebuilt: order el2(1), el3(5)
        Assert.Equal(2, panel.VisualChildrenCount);
        Assert.Same(el2, panel.GetVisualChild(0));
        Assert.Same(el3, panel.GetVisualChild(1));
    }

    [Fact]
    public void Panel_GetVisualChild_AfterClear_ReturnsEmptyCollection()
    {
        var panel = new TestPanel();
        var el1 = new TextBlock { Text = "A" };
        var el2 = new TextBlock { Text = "B" };

        panel.AddChild(el1);
        panel.AddChild(el2);
        Panel.SetZIndex(el1, 0);
        Panel.SetZIndex(el2, 1);

        // Build cache
        Assert.Equal(2, panel.VisualChildrenCount);
        Assert.Same(el1, panel.GetVisualChild(0));

        // Clear all children
        panel.Children.Clear();

        // Cache must be rebuilt: no children
        Assert.Equal(0, panel.VisualChildrenCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => panel.GetVisualChild(0));
    }

    private void Same(object expected, object actual)
    {
        Assert.Same(expected, actual);
    }
}
