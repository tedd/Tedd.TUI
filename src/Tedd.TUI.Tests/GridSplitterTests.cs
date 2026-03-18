using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class GridSplitterTests
{
    [Fact]
    public void GridSplitter_ResizesColumns_Pixel_Pixel()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(1) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });

        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);

        var window = new TuiWindow();
        window.Content = grid;
        grid.Measure(new Size(21, 10));
        grid.Arrange(new Rect(0, 0, 21, 10));

        splitter.RaiseEvent(new DragDeltaEventArgs(2, 0, Thumb.DragDeltaEvent, splitter));

        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(12, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[2].Width.GridUnitType);
        Assert.Equal(8, grid.ColumnDefinitions[2].Width.Value);
    }

    [Fact]
    public void GridSplitter_ResizesRows_Star_Star()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Pixel(1) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows };
        Grid.SetRow(splitter, 1);
        grid.Children.Add(splitter);

        var window = new TuiWindow();
        window.Content = grid;
        // Total height 21: row0=10, row1=1, row2=10
        grid.Measure(new Size(10, 21));
        grid.Arrange(new Rect(0, 0, 10, 21));

        splitter.RaiseEvent(new DragDeltaEventArgs(0, 5, Thumb.DragDeltaEvent, splitter));

        Assert.Equal(GridUnitType.Star, grid.RowDefinitions[0].Height.GridUnitType);
        Assert.Equal(GridUnitType.Star, grid.RowDefinitions[2].Height.GridUnitType);

        // Stars start at 1 and 1. Ratio becomes 15 / 20 = 0.75 for top, 0.25 for bottom.
        // Wait, they start at 1.0 each. Sum = 2.0.
        // 10 + 5 = 15. 10 - 5 = 5.
        // Ratio = 15/20 = 0.75. Sum * Ratio = 1.5.
        Assert.Equal(1.5, grid.RowDefinitions[0].Height.Value);
        Assert.Equal(0.5, grid.RowDefinitions[2].Height.Value);
    }

    [Fact]
    public void GridSplitter_ResizesColumns_Pixel_Star()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(1) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);

        var window = new TuiWindow();
        window.Content = grid;
        grid.Measure(new Size(21, 10));
        grid.Arrange(new Rect(0, 0, 21, 10));

        splitter.RaiseEvent(new DragDeltaEventArgs(2, 0, Thumb.DragDeltaEvent, splitter));

        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(12, grid.ColumnDefinitions[0].Width.Value);

        Assert.Equal(GridUnitType.Star, grid.ColumnDefinitions[2].Width.GridUnitType);
        Assert.Equal(1.0, grid.ColumnDefinitions[2].Width.Value);
    }

    [Fact]
    public void GridSplitter_ResizesRows_Star_Pixel()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Pixel(1) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Pixel(10) });

        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows };
        Grid.SetRow(splitter, 1);
        grid.Children.Add(splitter);

        var window = new TuiWindow();
        window.Content = grid;
        grid.Measure(new Size(10, 21));
        grid.Arrange(new Rect(0, 0, 10, 21));

        splitter.RaiseEvent(new DragDeltaEventArgs(0, -2, Thumb.DragDeltaEvent, splitter));

        Assert.Equal(GridUnitType.Star, grid.RowDefinitions[0].Height.GridUnitType);
        Assert.Equal(1.0, grid.RowDefinitions[0].Height.Value);

        Assert.Equal(GridUnitType.Pixel, grid.RowDefinitions[2].Height.GridUnitType);
        Assert.Equal(12, grid.RowDefinitions[2].Height.Value); // 10 - (-2) = 12
    }

    [Fact]
    public void GridSplitter_AutoDirection_DeterminedByRenderSize()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(1) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        // Auto uses Width <= Height logic. So if it's 1 wide and 10 tall, it should resize columns.
        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Auto };
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);

        var window = new TuiWindow();
        window.Content = grid;
        grid.Measure(new Size(21, 10));
        grid.Arrange(new Rect(0, 0, 21, 10));

        // Verify rendersize
        Assert.Equal(1, splitter.RenderSize.Width);
        Assert.Equal(10, splitter.RenderSize.Height);

        // This should trigger column resize
        splitter.RaiseEvent(new DragDeltaEventArgs(2, 0, Thumb.DragDeltaEvent, splitter));

        // It should have resized the columns
        Assert.Equal(1.2, grid.ColumnDefinitions[0].Width.Value, 3);
        Assert.Equal(0.8, grid.ColumnDefinitions[2].Width.Value, 3);
    }
}
