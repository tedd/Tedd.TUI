using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class GridTests
{
    [Fact]
    public void Grid_Pixel_Sizing_IsCorrect()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(20) });

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Pixel(5) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Pixel(10) });

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(10, grid.ColumnDefinitions[0].ActualWidth);
        Assert.Equal(20, grid.ColumnDefinitions[1].ActualWidth);
        Assert.Equal(5, grid.RowDefinitions[0].ActualHeight);
        Assert.Equal(10, grid.RowDefinitions[1].ActualHeight);
    }

    [Fact]
    public void Grid_Star_Sizing_Distributes_Remaining_Space()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // 1*
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // 2*

        grid.Measure(new Size(30, 100));
        grid.Arrange(new Rect(0, 0, 30, 100));

        // 30 total. 1* + 2* = 3*.
        // Col 0 = 10, Col 1 = 20.
        Assert.Equal(10, grid.ColumnDefinitions[0].ActualWidth);
        Assert.Equal(20, grid.ColumnDefinitions[1].ActualWidth);
    }

    [Fact]
    public void Grid_Auto_Sizing_Respects_Content()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var text = new TextBlock { Text = "12345" }; // Width 5
        grid.AddChild(text);
        Grid.SetColumn(text, 0);

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(5, grid.ColumnDefinitions[0].ActualWidth);
        // Remaining 95 goes to star
        Assert.Equal(95, grid.ColumnDefinitions[1].ActualWidth);
    }

    [Fact]
    public void Grid_Implicit_Definitions_Default_To_Star()
    {
        var grid = new Grid();
        // No definitions -> behaves like 1x1 Star grid

        grid.Measure(new Size(50, 50));
        grid.Arrange(new Rect(0, 0, 50, 50));

        // We can't access internal implicit definitions easily, but we can verify child size
        var child = new TextBlock { Text = "Hello" };
        grid.AddChild(child);

        grid.Measure(new Size(50, 50));
        grid.Arrange(new Rect(0, 0, 50, 50));

        // Child should be arranged to fill the cell if alignment is stretch (default)
        // But TextBlock Measure returns desired size.
        // Let's check RenderSize of child.
        // TextBlock implementation:
        // Measure returns (Len, 1).
        // Arrange uses finalRect. TextBlock.Arrange doesn't change RenderSize logic from UIElement.
        // UIElement.Arrange sets RenderSize = finalRect.

        // So child.RenderSize should be 50,50 if stretch is default.
        // Wait, UIElement.HorizontalAlignment defaults to Stretch.

        Assert.Equal(50, child.RenderSize.Width);
        Assert.Equal(50, child.RenderSize.Height);
    }

    [Fact]
    public void Grid_Span_Placement()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });

        var child = new TextBlock { Text = "Span" };
        grid.AddChild(child);
        Grid.SetColumn(child, 0);
        Grid.SetColumnSpan(child, 2);

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(20, child.RenderSize.Width);
    }

    [Fact]
    public void Grid_DataContext_Propagates()
    {
        var grid = new Grid();
        var child = new TextBlock();
        grid.AddChild(child);

        object data = new object();
        grid.DataContext = data;

        // UIElement handles inheritance
        Assert.Equal(data, child.DataContext);
    }
}
