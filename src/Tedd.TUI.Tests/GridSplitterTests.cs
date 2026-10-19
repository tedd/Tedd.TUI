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

        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows };
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

    [Fact]
    public void GridSplitter_FractionalDrags_AccumulateIntoWholeCells()
    {
        var window = new TuiWindow();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter();
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;
        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        int initialLeftWidth = grid.ColumnDefinitions[0].ActualWidth;

        // Sub-cell deltas (pixel hosts) must not be truncated away: 0.4 + 0.4 < 1 cell
        // does nothing yet, the third 0.4 crosses the cell boundary and moves 1 column.
        splitter.RaiseEvent(new DragDeltaEventArgs(0.4, 0.0, Thumb.DragDeltaEvent, splitter));
        splitter.RaiseEvent(new DragDeltaEventArgs(0.4, 0.0, Thumb.DragDeltaEvent, splitter));
        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(initialLeftWidth, grid.ColumnDefinitions[0].ActualWidth);

        splitter.RaiseEvent(new DragDeltaEventArgs(0.4, 0.0, Thumb.DragDeltaEvent, splitter));
        Assert.Equal(initialLeftWidth + 1, grid.ColumnDefinitions[0].Width.Value);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Center, GridResizeDirection.Rows)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Stretch, GridResizeDirection.Columns)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Center, GridResizeDirection.Columns)]
    public void GridSplitter_AutoDirection_ByAlignment(HorizontalAlignment hAlign, VerticalAlignment vAlign, GridResizeDirection expectedDirection)
    {
        var window = new TuiWindow();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Auto,
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign
        };

        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        // Measure layout - the measured size will reflect the direction chosen
        window.Measure(new Size(80, 24));

        if (expectedDirection == GridResizeDirection.Columns)
        {
            Assert.Equal(1, splitter.DesiredSize.Width); // Thickness 1 horizontally
        }
        else
        {
            Assert.Equal(1, splitter.DesiredSize.Height); // Thickness 1 vertically
        }
    }

    [Theory]
    [InlineData(true, false, GridResizeDirection.Rows)]
    [InlineData(false, true, GridResizeDirection.Columns)]
    public void GridSplitter_AutoDirection_ByGridLengthAuto(bool rowAuto, bool colAuto, GridResizeDirection expectedDirection)
    {
        var window = new TuiWindow();
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });
        grid.RowDefinitions.Add(new RowDefinition { Height = rowAuto ? new GridLength(1, GridUnitType.Auto) : new GridLength(10, GridUnitType.Pixel) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = colAuto ? new GridLength(1, GridUnitType.Auto) : new GridLength(10, GridUnitType.Pixel) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });

        var splitter = new GridSplitter
        {
            ResizeDirection = GridResizeDirection.Auto,
            HorizontalAlignment = HorizontalAlignment.Center, // Explicitly prevent alignment overriding logic
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetRow(splitter, 1);
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        window.Measure(new Size(80, 24));

        if (expectedDirection == GridResizeDirection.Columns)
        {
            Assert.Equal(1, splitter.DesiredSize.Width);
        }
        else
        {
            Assert.Equal(1, splitter.DesiredSize.Height);
        }
    }

    [Fact]
    public void GridSplitter_Drag_RespectsMinMaxWidthConstraints()
    {
        var window = new TuiWindow();
        var grid = new Grid();

        // Setup columns with Min/Max constraints
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel), MinWidth = 5, MaxWidth = 15 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel), MinWidth = 5, MaxWidth = 15 });

        var splitter = new GridSplitter();
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        window.Content = grid;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Act 1: Drag left far past bounds.
        // Max negative change allowed: left can shrink down to MinWidth (10->5 = -5)
        // or right can grow to MaxWidth (10->15 = -5). So max drag left is -5.
        splitter.RaiseEvent(new DragDeltaEventArgs(-20.0, 0.0, Thumb.DragDeltaEvent, splitter));

        Assert.Equal(5, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(15, grid.ColumnDefinitions[2].Width.Value);

        // Reset accumulation (we're hacking a new drag essentially to test other bound)
        splitter.RaiseEvent(new DragStartedEventArgs(0, 0, Thumb.DragStartedEvent, splitter));

        // Act 2: Drag right far past bounds
        // Left is now 5, can grow to 15 (change +10)
        // Right is now 15, can shrink to 5 (change +10)
        // Let's drag +20
        splitter.RaiseEvent(new DragDeltaEventArgs(20.0, 0.0, Thumb.DragDeltaEvent, splitter));

        Assert.Equal(15, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(5, grid.ColumnDefinitions[2].Width.Value);
    }

    [Fact]
    public void GridSplitter_TemplateRoot_OverridesMeasureAndRender()
    {
        var splitter = new GridSplitter();
        var textBlock = new TextBlock { Text = "SPLIT" };
        var template = new ControlTemplate(_ => textBlock);
        splitter.Template = template;

        splitter.Measure(new Size(100, 100));

        // The splitter should assume the desired size of the textblock template.
        Assert.Equal(5, splitter.DesiredSize.Width); // "SPLIT".Length
        Assert.Equal(1, splitter.DesiredSize.Height);

        splitter.Arrange(new Rect(0, 0, 5, 1));

        var buffer = new VirtualBuffer(10, 5);
        splitter.Render(buffer, 0, 0);

        Assert.Equal('S', buffer.GetPixel(0, 0).Character);
        Assert.Equal('P', buffer.GetPixel(1, 0).Character);
    }
}
