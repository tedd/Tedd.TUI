using Xunit;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Tests;

public class DockPanelTests
{
    [Fact]
    public void TestDockPanel_LeftRightFill()
    {
        var dockPanel = new DockPanel { Width = 100, Height = 20 };

        var left = new TextBlock { Width = 20, Text = "Left" };
        DockPanel.SetDock(left, Dock.Left);

        var right = new TextBlock { Width = 20, Text = "Right" };
        DockPanel.SetDock(right, Dock.Right);

        var fill = new TextBlock { Text = "Fill" }; // Last child fills by default

        dockPanel.AddChild(left);
        dockPanel.AddChild(right);
        dockPanel.AddChild(fill);

        dockPanel.Measure(new Size(100, 20));
        dockPanel.Arrange(new Rect(0, 0, 100, 20));

        // Left should be at 0,0 with width 20
        Assert.Equal(0, left.RenderSize.X);
        Assert.Equal(0, left.RenderSize.Y);
        Assert.Equal(20, left.RenderSize.Width);
        Assert.Equal(20, left.RenderSize.Height); // Stretches vertically by default

        // Right should be at 80,0 with width 20
        Assert.Equal(80, right.RenderSize.X);
        Assert.Equal(0, right.RenderSize.Y);
        Assert.Equal(20, right.RenderSize.Width);
        Assert.Equal(20, right.RenderSize.Height);

        // Fill should take remaining space: 100 - 20 - 20 = 60
        Assert.Equal(20, fill.RenderSize.X);
        Assert.Equal(0, fill.RenderSize.Y);
        Assert.Equal(60, fill.RenderSize.Width);
        Assert.Equal(20, fill.RenderSize.Height);
    }

    [Fact]
    public void TestDockPanel_TopBottom()
    {
        var dockPanel = new DockPanel { Width = 50, Height = 50 };

        var top = new TextBlock { Height = 10, Text = "Top" };
        DockPanel.SetDock(top, Dock.Top);

        var bottom = new TextBlock { Height = 10, Text = "Bottom" };
        DockPanel.SetDock(bottom, Dock.Bottom);

        dockPanel.AddChild(top);
        dockPanel.AddChild(bottom);

        // Default LastChildFill is true, so the last child (bottom) will fill the remaining space!
        // Remaining space after top (10) is 40. So bottom will be 40 high, not 10.
        // To test strict docking without fill, we need another child or disable LastChildFill.

        // Let's test with LastChildFill = false to verify docking logic specifically.
        dockPanel.LastChildFill = false;

        dockPanel.Measure(new Size(50, 50));
        dockPanel.Arrange(new Rect(0, 0, 50, 50));

        // Top
        Assert.Equal(0, top.RenderSize.X);
        Assert.Equal(0, top.RenderSize.Y);
        Assert.Equal(50, top.RenderSize.Width); // Stretches horizontally
        Assert.Equal(10, top.RenderSize.Height);

        // Bottom
        Assert.Equal(0, bottom.RenderSize.X);
        Assert.Equal(40, bottom.RenderSize.Y); // 50 - 10
        Assert.Equal(50, bottom.RenderSize.Width);
        Assert.Equal(10, bottom.RenderSize.Height);
    }

    [Fact]
    public void TestDockPanel_LastChildFill_False()
    {
        var dockPanel = new DockPanel { Width = 100, Height = 20, LastChildFill = false };

        var left = new TextBlock { Width = 20, Text = "Left" };
        DockPanel.SetDock(left, Dock.Left);

        var center = new TextBlock { Width = 20, Text = "Center" };
        // No dock set, defaults to Left? No, DockProperty default is Left.

        dockPanel.AddChild(left);
        dockPanel.AddChild(center);

        dockPanel.Measure(new Size(100, 20));
        dockPanel.Arrange(new Rect(0, 0, 100, 20));

        // Left
        Assert.Equal(0, left.RenderSize.X);
        Assert.Equal(20, left.RenderSize.Width);

        // Center (Dock.Left default)
        Assert.Equal(20, center.RenderSize.X);
        Assert.Equal(20, center.RenderSize.Width); // Not stretched to fill
    }

    [Fact]
    public void TestDockPanel_ComplexMix()
    {
        // Layout: Top, Left, Right, Bottom, Fill
        var dockPanel = new DockPanel { Width = 100, Height = 100 };

        var top = new TextBlock { Height = 10 };
        DockPanel.SetDock(top, Dock.Top);

        var left = new TextBlock { Width = 10 };
        DockPanel.SetDock(left, Dock.Left);

        var right = new TextBlock { Width = 10 };
        DockPanel.SetDock(right, Dock.Right);

        var bottom = new TextBlock { Height = 10 };
        DockPanel.SetDock(bottom, Dock.Bottom);

        var center = new TextBlock();

        dockPanel.AddChild(top);
        dockPanel.AddChild(left);
        dockPanel.AddChild(right);
        dockPanel.AddChild(bottom);
        dockPanel.AddChild(center);

        dockPanel.Measure(new Size(100, 100));
        dockPanel.Arrange(new Rect(0, 0, 100, 100));

        // Top: 0,0 100x10
        Assert.Equal(new Rect(0, 0, 100, 10), top.RenderSize);

        // Remaining: 0,10 100x90

        // Left: 0,10 10x90
        Assert.Equal(new Rect(0, 10, 10, 90), left.RenderSize);

        // Remaining: 10,10 90x90

        // Right: 90,10 10x90 (TotalW 100 - Right 10 = 90)
        Assert.Equal(new Rect(90, 10, 10, 90), right.RenderSize);

        // Remaining: 10,10 80x90

        // Bottom: 10,90 80x10 (TotalH 100 - Bottom 10 = 90)
        // Note: Bottom takes from remaining height.
        // Remaining rect was x=10, y=10, w=80, h=90.
        // Bottom takes 10 height from bottom of that rect.
        // y = 10 + 90 - 10 = 90.
        Assert.Equal(new Rect(10, 90, 80, 10), bottom.RenderSize);

        // Remaining: 10,10 80x80

        // Center: 10,10 80x80
        Assert.Equal(new Rect(10, 10, 80, 80), center.RenderSize);
    }
}
