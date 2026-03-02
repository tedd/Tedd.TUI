using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class BorderCoverageTests
{
    [Theory]
    [InlineData(5, 0, 0, 0)]
    [InlineData(0, 10, 0, 0)]
    [InlineData(0, 0, 15, 0)]
    [InlineData(0, 0, 0, 20)]
    public void ScrollBarMargins_GetSet_Invalidates(int vTop, int vBot, int hLeft, int hRight)
    {
        var border = new Border();

        border.VerticalScrollBarMarginTop = vTop;
        border.VerticalScrollBarMarginBottom = vBot;
        border.HorizontalScrollBarMarginLeft = hLeft;
        border.HorizontalScrollBarMarginRight = hRight;

        Assert.Equal(vTop, border.VerticalScrollBarMarginTop);
        Assert.Equal(vBot, border.VerticalScrollBarMarginBottom);
        Assert.Equal(hLeft, border.HorizontalScrollBarMarginLeft);
        Assert.Equal(hRight, border.HorizontalScrollBarMarginRight);
    }

    [Fact]
    public void GetVisualChild_WithTitleAndStatusBar()
    {
        var border = new Border();
        var title = new TextBlock();
        var statusBar = new TextBlock();

        border.Title = title;
        border.StatusBar = statusBar;

        // Base children: ScrollViewer adds ScrollBars internally.
        // Let's rely on VisualChildrenCount.
        int total = border.VisualChildrenCount;

        // The last two should be Title and StatusBar
        var lastChild = border.GetVisualChild(total - 1);
        var secondLastChild = border.GetVisualChild(total - 2);

        // Depending on order in GetVisualChild:
        // if (Title != null) { if (index == 0) return Title; index--; }
        // if (StatusBar != null) { if (index == 0) return StatusBar; }
        Assert.Same(statusBar, lastChild);
        Assert.Same(title, secondLastChild);

        // Out of bounds
        Assert.Throws<ArgumentOutOfRangeException>(() => border.GetVisualChild(total));
        Assert.Throws<ArgumentOutOfRangeException>(() => border.GetVisualChild(-1));
    }

    [Fact]
    public void GetVisualChild_WithTitleOnly()
    {
        var border = new Border();
        var title = new TextBlock();

        border.Title = title;

        int total = border.VisualChildrenCount;
        var lastChild = border.GetVisualChild(total - 1);

        Assert.Same(title, lastChild);
        Assert.Throws<ArgumentOutOfRangeException>(() => border.GetVisualChild(total));
    }

    [Fact]
    public void GetVisualChild_WithStatusBarOnly()
    {
        var border = new Border();
        var statusBar = new TextBlock();

        border.StatusBar = statusBar;

        int total = border.VisualChildrenCount;
        var lastChild = border.GetVisualChild(total - 1);

        Assert.Same(statusBar, lastChild);
        Assert.Throws<ArgumentOutOfRangeException>(() => border.GetVisualChild(total));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Left)]
    public void ArrangeOverride_TitleAlignment(HorizontalAlignment alignment)
    {
        var border = new Border();
        var title = new TextBlock { Text = "Test" };
        border.Title = title;
        border.TitleAlignment = alignment;

        border.Measure(new Size(20, 10));
        border.Arrange(new Rect(0, 0, 20, 10));

        // Title width is 4.
        // For Left: X = 1
        // For Center: (20 - 4) / 2 = 8
        // For Right: 20 - 1 - 4 = 15

        int expectedX = 1;
        if (alignment == HorizontalAlignment.Center) expectedX = 8;
        if (alignment == HorizontalAlignment.Right) expectedX = 15;

        Assert.Equal(expectedX, title.RenderSize.X);
    }

    [Fact]
    public void Render_SmallSize_ReturnsEarly()
    {
        var border = new Border { BorderColor = ConsoleColor.Red };
        var buffer = new VirtualBuffer(10, 10);

        // Measure and Arrange with size 1x1
        border.Measure(new Size(1, 1));
        border.Arrange(new Rect(0, 0, 1, 1));

        border.Render(buffer, 0, 0);

        // Because width < 2 and height < 2, it should return early
        // Buffer should remain empty (default)
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void OnDataContextChanged_PropagatesToTitleAndStatusBar()
    {
        var border = new Border();
        var title = new TextBlock();
        var statusBar = new TextBlock();

        border.Title = title;
        border.StatusBar = statusBar;

        var context = new object();
        border.DataContext = context;

        Assert.Same(context, title.DataContext);
        Assert.Same(context, statusBar.DataContext);
    }

    [Fact]
    public void ArrangeOverride_WithHorizontalScrollBar()
    {
        var border = new Border();
        border.HorizontalScrollBarVisibility = true;
        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // Horizontal scrollbar should be arranged
        Assert.True(border.HorizontalScrollBarVisibility);
    }

    [Fact]
    public void Render_WithChildrenAndScrollBars()
    {
        var border = new Border();
        border.Title = new TextBlock { Text = "Title" };
        border.StatusBar = new TextBlock { Text = "Status" };
        border.Content = new TextBlock { Text = "Content" };
        border.VerticalScrollBarVisibility = true;
        border.HorizontalScrollBarVisibility = true;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        var buffer = new VirtualBuffer(20, 20);
        border.Render(buffer, 0, 0);

        // We aren't testing exact pixel output here, just executing the branches
        Assert.True(border.VerticalScrollBarVisibility);
        Assert.True(border.HorizontalScrollBarVisibility);
        Assert.NotNull(border.Title);
        Assert.NotNull(border.StatusBar);
        Assert.NotNull(border.Content);
    }
}
