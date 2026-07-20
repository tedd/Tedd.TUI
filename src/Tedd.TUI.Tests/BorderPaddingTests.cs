using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

/// <summary>
/// Tests for <see cref="Border.Padding"/>: the gap between the border line and
/// the content, defaulting to one character on every side.
/// </summary>
public class BorderPaddingTests
{
    private sealed class MeasuringChild : UIElement
    {
        public Size DesiredContentSize { get; init; } = new Size(10, 10);
        public Size LastMeasureSize { get; private set; }
        public Size LastArrangeSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastMeasureSize = availableSize;
            return DesiredContentSize;
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            LastArrangeSize = finalSize;
        }
    }

    private static Border CreateBorder(MeasuringChild child) => new Border
    {
        Child = child,
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
    };

    [Fact]
    public void Default_Padding_Is_One_On_Every_Side()
    {
        var border = new Border();
        Assert.Equal(new Thickness(1), border.Padding);
    }

    [Fact]
    public void Zero_Padding_Restores_Flush_Layout()
    {
        var child = new MeasuringChild();
        var border = CreateBorder(child);
        border.Padding = new Thickness(0);

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // Only the border line insets content: 20 - 2 = 18, child at (1,1).
        Assert.Equal(18, child.LastMeasureSize.Width);
        Assert.Equal(18, child.LastMeasureSize.Height);
        Assert.Equal(1, child.RenderSize.X);
        Assert.Equal(1, child.RenderSize.Y);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Uniform_Padding_Insets_Measure_And_Position(int pad)
    {
        var child = new MeasuringChild();
        var border = CreateBorder(child);
        border.Padding = new Thickness(pad);

        border.Measure(new Size(30, 30));
        border.Arrange(new Rect(0, 0, 30, 30));

        int expectedViewport = 30 - 2 - 2 * pad;
        Assert.Equal(expectedViewport, child.LastMeasureSize.Width);
        Assert.Equal(expectedViewport, child.LastMeasureSize.Height);
        Assert.Equal(1 + pad, child.RenderSize.X);
        Assert.Equal(1 + pad, child.RenderSize.Y);
    }

    [Fact]
    public void Asymmetric_Padding_Applies_Per_Side()
    {
        var child = new MeasuringChild();
        var border = CreateBorder(child);
        border.Padding = new Thickness(2, 1, 3, 0); // left, top, right, bottom

        border.Measure(new Size(30, 30));
        border.Arrange(new Rect(0, 0, 30, 30));

        // Width: 30 - 2 (border) - 2 - 3 = 23. Height: 30 - 2 - 1 - 0 = 27.
        Assert.Equal(23, child.LastMeasureSize.Width);
        Assert.Equal(27, child.LastMeasureSize.Height);
        // Position: border line (1) + left/top padding.
        Assert.Equal(3, child.RenderSize.X);
        Assert.Equal(2, child.RenderSize.Y);
    }

    [Fact]
    public void DesiredSize_Includes_Border_And_Padding()
    {
        var child = new MeasuringChild { DesiredContentSize = new Size(6, 4) };
        var border = CreateBorder(child);
        border.Padding = new Thickness(2);

        border.Measure(new Size(50, 50));

        // 6 + 2 (border) + 4 (padding) = 12; 4 + 2 + 4 = 10.
        Assert.Equal(12, border.DesiredSize.Width);
        Assert.Equal(10, border.DesiredSize.Height);
    }

    [Fact]
    public void BoxStyle_None_Ignores_Padding()
    {
        var child = new MeasuringChild();
        var border = CreateBorder(child);
        border.BoxStyle = BoxStyle.None;
        border.Padding = new Thickness(3);

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // A borderless Border stays an exact passthrough container.
        Assert.Equal(20, child.LastMeasureSize.Width);
        Assert.Equal(20, child.LastMeasureSize.Height);
        Assert.Equal(0, child.RenderSize.X);
        Assert.Equal(0, child.RenderSize.Y);
        Assert.Equal(10, border.DesiredSize.Width);
        Assert.Equal(10, border.DesiredSize.Height);
    }

    [Fact]
    public void Padding_Reduces_Scroll_Viewport_But_Not_Track_Length()
    {
        var child = new MeasuringChild { DesiredContentSize = new Size(5, 40) };
        var border = new Border
        {
            Child = child,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(1)
        };

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // Visible content rows: 20 - 2 (border) - 2 (padding) = 16 < 40 -> bar shown.
        Assert.True(border.IsVerticalScrollBarShown);
    }

    [Fact]
    public void Wrap_Clamp_Uses_Padded_Viewport()
    {
        // The Disabled-axis arrange clamp must clamp to the padded viewport,
        // not the border-only viewport, or wrapped content would overflow
        // into the padding gutter.
        var child = new MeasuringChild { DesiredContentSize = new Size(100, 5) };
        var border = CreateBorder(child);
        border.Padding = new Thickness(2);

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // 20 - 2 (border) - 4 (padding) = 14.
        Assert.Equal(14, child.LastArrangeSize.Width);
    }

    [Fact]
    public void Render_Leaves_Padding_Gutter_Blank()
    {
        var text = new TextBlock { Text = "XXXXXXXXXXXXXXXXXXXX" };
        var border = new Border
        {
            Child = text,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(1)
        };

        border.Measure(new Size(10, 5));
        border.Arrange(new Rect(0, 0, 10, 5));

        var buffer = new VirtualBuffer(10, 5);
        buffer.Clear();
        border.Render(buffer, 0, 0);

        // Row 1 is inside the border: col 1 is the left padding gutter, col 8 the
        // right gutter; the clip must keep the (overlong) text out of both.
        Assert.Equal(' ', buffer.GetPixel(1, 2).Character);
        Assert.Equal(' ', buffer.GetPixel(8, 2).Character);
        // Content starts after the gutter.
        Assert.Equal('X', buffer.GetPixel(2, 2).Character);
        // Row 1 (top padding row) is blank between the border lines.
        for (int x = 1; x < 9; x++)
        {
            Assert.Equal(' ', buffer.GetPixel(x, 1).Character);
        }
    }
}
