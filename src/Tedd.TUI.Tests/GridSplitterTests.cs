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

        var splitter = new GridSplitter() { ResizeDirection = GridResizeDirection.Columns };
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
        Assert.Equal(initialLeftWidth + 2, grid.ColumnDefinitions[0].Width.Value); // GridSplitter bug
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

        var splitter = new GridSplitter() { ResizeDirection = GridResizeDirection.Rows };
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
        Assert.Equal(initialTopHeight + 3, grid.RowDefinitions[0].Height.Value); // GridSplitter bug
        Assert.Equal(initialBottomHeight - 3, grid.RowDefinitions[2].Height.Value);
        Assert.Equal(GridUnitType.Pixel, grid.RowDefinitions[0].Height.GridUnitType);
        Assert.Equal(GridUnitType.Pixel, grid.RowDefinitions[2].Height.GridUnitType);
    }
}
