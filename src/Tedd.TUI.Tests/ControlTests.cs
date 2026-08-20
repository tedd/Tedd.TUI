using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ControlTests
{
    private class TestControl : Control
    {
        public TestControl()
        {
            Template = new ControlTemplate(parent =>
            {
                var border = new Border();
                border.TemplatedParent = parent;
                return border;
            });
        }
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var control = new TestControl();

        control.BorderBrush = ConsoleColor.Red;
        Assert.Equal(ConsoleColor.Red, control.BorderBrush);

        var thickness = new Thickness(1, 2, 3, 4);
        control.BorderThickness = thickness;
        Assert.Equal(thickness, control.BorderThickness);
    }

    [Fact]
    public void Padding_AffectsMeasureAndArrange()
    {
        var control = new TestControl();
        control.Padding = new Thickness(2, 1, 2, 1);

        // Measure with available size
        control.Measure(new Size(100, 100));

        // Border gets measured. Border default min size might be 2x2.
        // Actually Border DesiredSize depends on BoxStyle etc.
        // Let's just check the arrange logic directly.

        control.Arrange(new Rect(0, 0, 10, 10));
        var templateRoot = control.GetVisualChild(0) as UIElement;

        // Arrange inner size: 10-4=6, 10-2=8
        // Positioned at padding left/top: (2, 1)
        Assert.Equal(new Rect(2, 1, 6, 8), templateRoot.RenderSize);
    }

    [Fact]
    public void ApplyTemplate_WithNull_ClearsTemplateRoot()
    {
        var control = new TestControl();
        control.ApplyTemplate();
        Assert.Equal(1, control.VisualChildrenCount);

        control.Template = null;
        Assert.Equal(0, control.VisualChildrenCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => control.GetVisualChild(0));
    }
}

public class ContentControlTests
{
    private class TestContentControl : ContentControl
    {
    }

    [Fact]
    public void ContentTemplate_SetAndGet()
    {
        var cc = new TestContentControl();
        var template = new DataTemplate(() => new TextBlock { Text = "Test" });
        cc.ContentTemplate = template;

        Assert.Equal(template, cc.ContentTemplate);
    }

    [Fact]
    public void HasContent_IsInitiallyFalse()
    {
        var control = new TestContentControl();
        Assert.False(control.HasContent);
    }

    [Fact]
    public void SettingContent_UpdatesHasContent()
    {
        var control = new TestContentControl();
        control.Content = "Test";

        Assert.Equal("Test", control.Content);
        Assert.True(control.HasContent);
    }

    [Fact]
    public void ClearingContent_UpdatesHasContent()
    {
        var control = new TestContentControl();
        control.Content = "Test";
        Assert.True(control.HasContent);

        control.Content = null;
        Assert.False(control.HasContent);
    }
}

public class ControlTemplateTests
{
    [Fact]
    public void ControlTemplate_LoadContent_RequiresControlType()
    {
        var template = new ControlTemplate(parent => new Border());

        // Valid case
        var control = new Button();
        var element = template.LoadContent(control);
        Assert.IsType<Border>(element);

        // Invalid case
        var nonControl = new Border(); // Border is UIElement, not Control
        Assert.Throws<ArgumentException>(() => template.LoadContent(nonControl));
    }

    [Fact]
    public void DataTemplate_LoadContent()
    {
        var template = new DataTemplate(() => new TextBlock { Text = "Data" });

        var element = template.LoadContent(new Button()); // Parent doesn't matter for DataTemplate
        var tb = Assert.IsType<TextBlock>(element);
        Assert.Equal("Data", tb.Text);
    }
}
