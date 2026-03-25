using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ValidatorPanelTests
{
    [Fact]
    public void StackPanel_HierarchicalCompositionValidation()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        var topBorder = new Border { BoxStyle = BoxStyle.Single, Width = 10, Height = 3, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };
        var bottomBorder = new Border { BoxStyle = BoxStyle.Double, Width = 10, Height = 3, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };

        panel.Children.Add(topBorder);
        panel.Children.Add(bottomBorder);

        panel.Measure(new Size(20, 10));
        panel.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        panel.Render(buffer, 0, 0);

        // Top Border (Single)
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Top-Left
        Assert.Equal('\u2514', buffer.GetPixel(0, 2).Character); // Bottom-Left

        // Bottom Border (Double) - starts at Y=3
        Assert.Equal('\u2554', buffer.GetPixel(0, 3).Character); // Top-Left
        Assert.Equal('\u255A', buffer.GetPixel(0, 5).Character); // Bottom-Left
    }

    [Fact]
    public void DockPanel_CoordinatePreciseCharacterAssertion()
    {
        var panel = new DockPanel { LastChildFill = true };

        var leftBorder = new Border { BoxStyle = BoxStyle.Heavy, Width = 5, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };
        DockPanel.SetDock(leftBorder, Dock.Left);

        var rightBorder = new Border { BoxStyle = BoxStyle.Single, Width = 5, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };
        DockPanel.SetDock(rightBorder, Dock.Right);

        var fillBorder = new Border { BoxStyle = BoxStyle.Double, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };

        panel.Children.Add(leftBorder);
        panel.Children.Add(rightBorder);
        panel.Children.Add(fillBorder);

        panel.Measure(new Size(20, 10));
        panel.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        panel.Render(buffer, 0, 0);

        // Left Border (Heavy)
        Assert.Equal('\u250F', buffer.GetPixel(0, 0).Character); // Heavy Top-Left
        Assert.Equal('\u251B', buffer.GetPixel(4, 9).Character); // Heavy Bottom-Right

        // Right Border (Single)
        Assert.Equal('\u250C', buffer.GetPixel(15, 0).Character); // Single Top-Left
        Assert.Equal('\u2518', buffer.GetPixel(19, 9).Character); // Single Bottom-Right

        // Fill Border (Double)
        Assert.Equal('\u2554', buffer.GetPixel(5, 0).Character); // Double Top-Left
        Assert.Equal('\u255D', buffer.GetPixel(14, 9).Character); // Double Bottom-Right
    }

    [Fact]
    public void Panel_DynamicStateMutation()
    {
        var panel = new DockPanel { LastChildFill = true };
        var topBorder = new Border { BoxStyle = BoxStyle.Heavy, Height = 2, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };
        DockPanel.SetDock(topBorder, Dock.Top);
        var fillBorder = new Border { BoxStyle = BoxStyle.Double, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false };

        panel.Children.Add(topBorder);
        panel.Children.Add(fillBorder);

        // 20x10 State
        panel.Measure(new Size(20, 10));
        panel.Arrange(new Rect(0, 0, 20, 10));
        var buffer1 = new VirtualBuffer(20, 10);
        panel.Render(buffer1, 0, 0);

        Assert.Equal('\u250F', buffer1.GetPixel(0, 0).Character); // Heavy Top-Left at 0,0
        Assert.Equal('\u2554', buffer1.GetPixel(0, 2).Character); // Double Top-Left at 0,2
        Assert.Equal('\u255D', buffer1.GetPixel(19, 9).Character); // Double Bottom-Right at 19,9

        // 30x15 State (Resize)
        panel.Measure(new Size(30, 15));
        panel.Arrange(new Rect(0, 0, 30, 15));
        var buffer2 = new VirtualBuffer(30, 15);
        panel.Render(buffer2, 0, 0);

        Assert.Equal('\u250F', buffer2.GetPixel(0, 0).Character); // Heavy Top-Left at 0,0
        Assert.Equal('\u2554', buffer2.GetPixel(0, 2).Character); // Double Top-Left at 0,2
        Assert.Equal('\u255D', buffer2.GetPixel(29, 14).Character); // Double Bottom-Right at 29,14
    }

    [Fact]
    public void Panel_BoundaryAndEdgeVerification_ExtremeConstraints()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new Border { BoxStyle = BoxStyle.Single, Width = 5, Height = 5, HorizontalScrollBarVisibility = false, VerticalScrollBarVisibility = false });
        panel.Children.Add(new TextBlock { Text = "Test" });

        // 0x0 size
        panel.Measure(new Size(0, 0));
        panel.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(10, 10);

        // Assert it doesn't throw out of bounds or other exceptions
        var exception = Record.Exception(() => panel.Render(buffer0, 0, 0));
        Assert.Null(exception);
    }
}
