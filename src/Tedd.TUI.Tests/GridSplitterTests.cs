using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class GridSplitterTests
{
    [Fact]
    public void GridSplitter_HorizontalDrag_AdjustsColumnWidths()
    {
        // Arrange
        var window = new TuiWindow();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Splitter column
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter();
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        // Measure and arrange so sizes are set
        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        int initialLeftWidth = grid.ColumnDefinitions[0].ActualWidth;
        int initialRightWidth = grid.ColumnDefinitions[2].ActualWidth;

        // Act
        // Simulate dragging
        var dragArgs = new DragDeltaEventArgs(2.0, 0.0, Thumb.DragDeltaEvent, splitter);
        splitter.RaiseEvent(dragArgs);

        // Assert
        Assert.Equal(initialLeftWidth + 2, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(initialRightWidth - 2, grid.ColumnDefinitions[2].Width.Value);
        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[2].Width.GridUnitType);
    }

    [Fact]
    public void GridSplitter_VerticalDrag_AdjustsRowHeights()
    {
        // Arrange
        var window = new TuiWindow();
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Splitter row
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter();
        Grid.SetRow(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        // Measure and arrange so sizes are set
        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        int initialTopHeight = grid.RowDefinitions[0].ActualHeight;
        int initialBottomHeight = grid.RowDefinitions[2].ActualHeight;

        // Act
        // Simulate dragging
        var dragArgs = new DragDeltaEventArgs(0.0, 3.0, Thumb.DragDeltaEvent, splitter);
        splitter.RaiseEvent(dragArgs);

        // Assert
        Assert.Equal(initialTopHeight + 3, grid.RowDefinitions[0].Height.Value);
        Assert.Equal(initialBottomHeight - 3, grid.RowDefinitions[2].Height.Value);
        Assert.Equal(GridUnitType.Pixel, grid.RowDefinitions[0].Height.GridUnitType);
        Assert.Equal(GridUnitType.Pixel, grid.RowDefinitions[2].Height.GridUnitType);
    }

    [Theory]
    [InlineData(GridResizeDirection.Columns, 5.0, 0.0, 15, 5)]   // Move right
    [InlineData(GridResizeDirection.Columns, -5.0, 0.0, 5, 15)]  // Move left
    [InlineData(GridResizeDirection.Columns, 15.0, 0.0, 20, 0)]  // Clamp right
    [InlineData(GridResizeDirection.Columns, -15.0, 0.0, 0, 20)] // Clamp left
    [InlineData(GridResizeDirection.Rows, 0.0, 5.0, 10, 10)]     // Wrong axis, shouldn't move columns
    [InlineData(GridResizeDirection.Auto, 5.0, 0.0, 15, 5)]      // Auto detecting columns
    public void GridSplitter_SetResizeDirection_AffectsBehavior(GridResizeDirection direction, double hChange, double vChange, int expectedLeftWidth, int expectedRightWidth)
    {
        // Arrange
        var window = new TuiWindow();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel), MinWidth = 0, MaxWidth = 20 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Splitter column
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel), MinWidth = 0, MaxWidth = 20 });

        var splitter = new GridSplitter();
        splitter.ResizeDirection = direction;
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        // Measure and arrange so sizes are set
        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Act
        var dragArgs = new DragDeltaEventArgs(hChange, vChange, Thumb.DragDeltaEvent, splitter);
        splitter.RaiseEvent(dragArgs);

        // Assert
        Assert.Equal(expectedLeftWidth, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(expectedRightWidth, grid.ColumnDefinitions[2].Width.Value);
    }

    [Fact]
    public void GridSplitter_AutoDirectionFallback_EmptyRows_AssumesColumns()
    {
        // Arrange
        var window = new TuiWindow();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter();
        splitter.ResizeDirection = GridResizeDirection.Auto;
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        var dragArgs = new DragDeltaEventArgs(2.0, 0.0, Thumb.DragDeltaEvent, splitter);
        splitter.RaiseEvent(dragArgs);

        Assert.Equal(12, grid.ColumnDefinitions[0].Width.Value);
    }

    [Fact]
    public void GridSplitter_AutoDirectionFallback_EmptyColumns_AssumesRows()
    {
        // Arrange
        var window = new TuiWindow();
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter();
        splitter.ResizeDirection = GridResizeDirection.Auto;
        Grid.SetRow(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        var dragArgs = new DragDeltaEventArgs(0.0, 3.0, Thumb.DragDeltaEvent, splitter);
        splitter.RaiseEvent(dragArgs);

        Assert.Equal(13, grid.RowDefinitions[0].Height.Value);
    }
}
