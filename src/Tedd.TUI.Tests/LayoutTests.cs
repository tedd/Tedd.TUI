using System;
using System.Reflection;
using Xunit;

namespace Tedd.TUI.Tests;

public class LayoutTests
{
    private class TestElement : UIElement
    {
        public Size AvailableSizePassed { get; private set; }
        public Size DesiredSizeToReturn { get; set; } = new Size(10, 10);
        public Rect FinalRectPassed { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            AvailableSizePassed = availableSize;
            return DesiredSizeToReturn;
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            FinalRectPassed = new Rect(0, 0, finalSize.Width, finalSize.Height);
        }
    }

    private class TestControl : Control
    {
        public TestElement Child { get; }

        public TestControl()
        {
            Child = new TestElement();
            Template = new ControlTemplate(c => Child);
            ApplyTemplate();
        }
    }

    [Fact]
    public void Margin_ModifiesMeasureAndArrange()
    {
        var element = new TestElement();
        element.Margin = new Thickness(1, 2, 3, 4);
        element.DesiredSizeToReturn = new Size(10, 10);

        // Measure Pass
        Size available = new Size(100, 100);
        element.Measure(available);

        // Available size passed to MeasureOverride should be reduced by margins
        Assert.Equal(96, element.AvailableSizePassed.Width);  // 100 - (1 + 3)
        Assert.Equal(94, element.AvailableSizePassed.Height); // 100 - (2 + 4)

        // Desired size should include margins
        Assert.Equal(14, element.DesiredSize.Width);  // 10 + (1 + 3)
        Assert.Equal(16, element.DesiredSize.Height); // 10 + (2 + 4)

        // Arrange Pass
        Rect finalRect = new Rect(10, 10, 50, 50);
        element.Arrange(finalRect);

        // RenderSize position should be offset by top/left margins
        Assert.Equal(11, element.RenderSize.X); // 10 + 1 (Left)
        Assert.Equal(12, element.RenderSize.Y); // 10 + 2 (Top)

        // Final rect passed to ArrangeOverride should be reduced by margins
        // (Wait, ArrangeOverride receives Size, RenderSize receives Rect)
        Assert.Equal(46, element.RenderSize.Width);  // 50 - 4 (Left+Right)
        Assert.Equal(44, element.RenderSize.Height); // 50 - 6 (Top+Bottom)
    }

    [Fact]
    public void Control_Padding_ModifiesMeasureAndArrangeOfTemplateRoot()
    {
        var control = new TestControl();
        control.Padding = new Thickness(2, 3, 4, 5);
        control.Child.DesiredSizeToReturn = new Size(20, 20);

        // Measure Pass
        Size available = new Size(100, 100);
        control.Measure(available);

        // Control passes reduced available size to TemplateRoot
        Assert.Equal(94, control.Child.AvailableSizePassed.Width);  // 100 - (2 + 4)
        Assert.Equal(92, control.Child.AvailableSizePassed.Height); // 100 - (3 + 5)

        // Control's desired size includes the child's desired size + padding
        Assert.Equal(26, control.DesiredSize.Width);  // 20 + 6
        Assert.Equal(28, control.DesiredSize.Height); // 20 + 8

        // Arrange Pass
        Rect finalRect = new Rect(0, 0, 50, 50);
        control.Arrange(finalRect);

        // The TemplateRoot (Child) should be offset by the padding
        Assert.Equal(2, control.Child.RenderSize.X);
        Assert.Equal(3, control.Child.RenderSize.Y);

        // The TemplateRoot should have its size reduced by padding
        Assert.Equal(44, control.Child.RenderSize.Width);  // 50 - 6
        Assert.Equal(42, control.Child.RenderSize.Height); // 50 - 8
    }
}