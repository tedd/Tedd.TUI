using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

/// <summary>
/// Tests for <see cref="Window.Padding"/> and <see cref="DialogBox.Padding"/>:
/// the gap between the frame and the content, defaulting to one character on
/// every side.
/// </summary>
public class WindowPaddingTests
{
    private sealed class MeasuringChild : UIElement
    {
        public Size DesiredContentSize { get; init; } = new Size(10, 4);
        public Size LastMeasureSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastMeasureSize = availableSize;
            return DesiredContentSize;
        }
    }

    [Fact]
    public void Window_Default_Padding_Is_One_On_Every_Side()
    {
        Assert.Equal(new Thickness(1), new Window().Padding);
        Assert.Equal(new Thickness(1), new DialogBox().Padding);
    }

    [Fact]
    public void Window_Zero_Padding_Restores_Flush_Layout()
    {
        var child = new MeasuringChild();
        var win = new Window { Width = 20, Height = 10, Content = child, Padding = new Thickness(0) };

        win.Measure(new Size(50, 50));
        win.Arrange(new Rect(0, 0, 20, 10));

        Assert.Equal(18, child.LastMeasureSize.Width);
        Assert.Equal(8, child.LastMeasureSize.Height);
        Assert.Equal(1, child.RenderSize.X);
        Assert.Equal(1, child.RenderSize.Y);
    }

    [Fact]
    public void Window_Custom_Padding_Insets_Measure_And_Position()
    {
        var child = new MeasuringChild();
        var win = new Window { Width = 30, Height = 20, Content = child, Padding = new Thickness(3, 2, 1, 0) };

        win.Measure(new Size(50, 50));
        win.Arrange(new Rect(0, 0, 30, 20));

        // Width: 30 - 2 (frame) - 3 - 1 = 24. Height: 20 - 2 - 2 - 0 = 16.
        Assert.Equal(24, child.LastMeasureSize.Width);
        Assert.Equal(16, child.LastMeasureSize.Height);
        Assert.Equal(4, child.RenderSize.X); // frame (1) + left padding (3)
        Assert.Equal(3, child.RenderSize.Y); // frame (1) + top padding (2)
    }

    [Fact]
    public void Window_AutoSize_Includes_Padding()
    {
        var child = new MeasuringChild { DesiredContentSize = new Size(10, 4) };
        var win = new Window { Content = child, Padding = new Thickness(2) };

        win.Measure(new Size(80, 80));

        // 10 + 2 (frame) + 4 (padding) = 16 wide; 4 + 2 + 4 = 10 tall.
        Assert.Equal(16, win.DesiredSize.Width);
        Assert.Equal(10, win.DesiredSize.Height);
    }

    [Fact]
    public void DialogBox_Padding_Insets_Content()
    {
        var child = new MeasuringChild();
        var dialog = new DialogBox { Width = 20, Height = 10, Content = child, Padding = new Thickness(2) };

        dialog.Measure(new Size(50, 50));
        dialog.Arrange(new Rect(0, 0, 20, 10));

        // 20 - 2 (frame) - 4 (padding) = 14; 10 - 2 - 4 = 4.
        Assert.Equal(14, child.LastMeasureSize.Width);
        Assert.Equal(4, child.LastMeasureSize.Height);
        Assert.Equal(3, child.RenderSize.X);
        Assert.Equal(3, child.RenderSize.Y);
    }

    [Fact]
    public void DialogBox_AutoSize_Includes_Padding()
    {
        var child = new MeasuringChild { DesiredContentSize = new Size(10, 4) };
        var dialog = new DialogBox { Content = child };

        dialog.Measure(new Size(80, 80));

        // Default padding 1: 10 + 2 + 2 = 14 wide; 4 + 2 + 2 = 8 tall.
        Assert.Equal(14, dialog.DesiredSize.Width);
        Assert.Equal(8, dialog.DesiredSize.Height);
    }
}
