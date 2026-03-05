using System;
using Xunit;
using Tedd.TUI;
using System.Collections.Generic;

namespace Tedd.TUI.Tests;

public class ItemsPresenterTests
{
    private class TestItemsControl : ItemsControl
    {
        public TestItemsControl()
        {
            Template = new ControlTemplate((c) => new ItemsPresenter());
        }

        public UIElement GetTemplateRoot() => TemplateRoot;
    }

    [Theory]
    [InlineData(0, 0, 100, 100)]
    [InlineData(10, 20, 50, 50)]
    [InlineData(-10, -10, 0, 0)]
    public void ItemsPresenter_ApplyTemplate_PopulatesPanel(int x, int y, int width, int height)
    {
        var control = new TestItemsControl();
        control.ItemsSource = new List<string> { "Item 1", "Item 2" };

        // Force template application
        control.ApplyTemplate();

        control.Measure(new Size(width, height));
        control.Arrange(new Rect(x, y, width, height));

        var presenter = (ItemsPresenter)control.GetTemplateRoot();
        Assert.NotNull(presenter);

        Assert.Equal(1, presenter.VisualChildrenCount);

        var panel = presenter.GetVisualChild(0) as Panel;
        Assert.NotNull(panel);

        // Assert items are generated
        Assert.Equal(2, panel.Children.Count);

        var buffer = new VirtualBuffer(Math.Max(1, width), Math.Max(1, height));
        presenter.Render(buffer, 0, 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ItemsPresenter_GetVisualChild_ThrowsOutOfRange(int index)
    {
        var presenter = new ItemsPresenter();
        Assert.Throws<ArgumentOutOfRangeException>(() => presenter.GetVisualChild(index));
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(0, 0)]
    public void ItemsPresenter_NoPanel_Measure_Arrange(int width, int height)
    {
        var presenter = new ItemsPresenter();
        presenter.Measure(new Size(width, height));
        presenter.Arrange(new Rect(0, 0, width, height));
        var buffer = new VirtualBuffer(Math.Max(1, width), Math.Max(1, height));
        presenter.Render(buffer, 0, 0);
        Assert.Equal(0, presenter.VisualChildrenCount);
    }

    [Fact]
    public void ItemsPresenter_PopulatePanel_EarlyExitIfPanelNull()
    {
        var presenter = new ItemsPresenter();
        var control = new TestItemsControl();
        // Since panel is null, this should return immediately without throwing
        presenter.PopulatePanel(control);
    }

    [Fact]
    public void ItemsPresenter_PopulatePanel_ItemIsOwnContainer()
    {
        var control = new TestItemsControl();
        control.ApplyTemplate();

        var presenter = (ItemsPresenter)control.GetTemplateRoot();

        // Add a UIElement directly, which should be its own container
        var textBlock = new TextBlock { Text = "Test UI Element" };
        control.ItemsSource = new List<object> { textBlock };
        presenter.PopulatePanel(control);

        // Populate Panel should process the UIElement as its own container
        var panel = presenter.GetVisualChild(0) as Panel;
        Assert.NotNull(panel);
        Assert.Single(panel.Children);
        Assert.Same(textBlock, panel.Children[0]);
    }
}
