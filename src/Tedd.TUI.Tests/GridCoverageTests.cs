using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class GridCoverageTests
{
    [Theory]
    [InlineData(100, 25, 25, 25, 25)] // 1*, 1*, 1*, 1* -> Equal
    [InlineData(100, 10, 20, 30, 40)] // 1*, 2*, 3*, 4* -> Proportional
    public void Grid_StarSizing_Distribution(int totalWidth, int w1, int w2, int w3, int w4)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });

        // Normalize weights for test case simplicity (1,1,1,1) vs (1,2,3,4)
        // If test inputs are meant to match specific scenarios:
        // Case 1: 100 / 4 = 25 each (if weights were equal)
        // Case 2: 100 / 10 = 10 unit. 10, 20, 30, 40.

        // I'll adjust the setup based on inputs or just use standard logic.
        // Let's make setup dynamic or fixed?
        // Let's use specific setup per test or standard setup.

        // Re-setup to match expected logic.
        grid.ColumnDefinitions.Clear();
        // Determine weights from expectations?
        // No, let's fix the grid structure and pass expectations.

        // Grid: 1*, 1*, 1*, 1*
        if (w1 == w2 && w2 == w3)
        {
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        else
        {
             // Grid: 1*, 2*, 3*, 4*
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
             grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
        }

        grid.Measure(new Size(totalWidth, 100));
        grid.Arrange(new Rect(0, 0, totalWidth, 100));

        Assert.Equal(w1, grid.ColumnDefinitions[0].ActualWidth);
        Assert.Equal(w2, grid.ColumnDefinitions[1].ActualWidth);
        Assert.Equal(w3, grid.ColumnDefinitions[2].ActualWidth);
        Assert.Equal(w4, grid.ColumnDefinitions[3].ActualWidth);
    }

    [Fact]
    public void Grid_Implicit_Is_1x1_Star()
    {
        var grid = new Grid();
        // No definitions

        grid.Measure(new Size(50, 60));
        grid.Arrange(new Rect(0, 0, 50, 60));

        // We can't verify definitions directly as they are internal lists in Grid implementation?
        // Wait, Grid.RowDefinitions is public List.
        // But implicit rows are stored in private `_implicitRows`.
        // However, child arrangement tells us the story.

        var child = new TextBlock { Text = "Test" };
        grid.AddChild(child);

        grid.Measure(new Size(50, 60));
        grid.Arrange(new Rect(0, 0, 50, 60));

        // Child should span full size
        Assert.Equal(50, child.RenderSize.Width);
        Assert.Equal(60, child.RenderSize.Height);
    }

    [Fact]
    public void Grid_Spans_Exceeding_Definitions_Clamp_To_End()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(20) });

        var child = new TextBlock();
        grid.AddChild(child);
        Grid.SetColumn(child, 0);
        Grid.SetColumnSpan(child, 5); // Exceeds 2

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        // Should span Col 0 + Col 1 = 10 + 20 = 30.
        Assert.Equal(30, child.RenderSize.Width);
    }

    [Fact]
    public void Grid_Placement_Outside_Definitions_Clamps_To_Last()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(20) });

        var child = new TextBlock();
        grid.AddChild(child);
        Grid.SetColumn(child, 5); // Index 5, but max index is 1

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        // Should be in Col 1 (width 20).
        // X position should be Offset of Col 1 = 10.

        Assert.Equal(20, child.RenderSize.Width);
        Assert.Equal(10, child.RenderSize.X);
    }

    [Theory]
    [InlineData(10, 50, 100, 50)] // Min 50 wins over Star share (10)
    [InlineData(90, 0, 50, 50)]   // Max 50 wins over Star share (90)
    public void Grid_Star_MinMax_Constraints(int starShare, int min, int max, int expected)
    {
        var grid = new Grid();
        // Col 0: 9* (takes most space)
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(9, GridUnitType.Star) });
        // Col 1: 1* but with constraints
        var col1 = new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
            MinWidth = min,
            MaxWidth = max
        };
        grid.ColumnDefinitions.Add(col1);

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        // Calculation:
        // Total Star = 10*. Total Width = 100.
        // Unit = 10.
        // Col 0 nominal = 90.
        // Col 1 nominal = 10.

        // If starShare argument matches nominal, we check constraints.
        // Test case 1: Nominal 10. Min 50. Expected 50.
        // Test case 2: Nominal 90? Wait.

        // Let's simplify the test to just check Col 1.
        // Setup: 100 total width.
        // Col 0: 9*. Col 1: 1*.
        // Nominal Col 1 is 10.

        // For the second case (Max constraint):
        // We need Col 1 to want MORE than Max.
        // Swap weights?
        if (starShare > 50)
        {
             // Case 2 logic: We want Col 1 to be huge.
             // Col 0: 1*. Col 1: 9*.
             grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
             col1.Width = new GridLength(9, GridUnitType.Star);
             // Nominal Col 1 = 90. Max = 50. Expected = 50.
        }

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(expected, col1.ActualWidth);
        // Note: Does Grid re-distribute the stolen/yielded space?
        // Current implementation:
        // Measure calculates Star, then applies Min/Max.
        // It does NOT re-distribute the difference to other star columns in the same pass.
        // So Col 0 will remain at its nominal width.
        // Total width might not equal 100 if constraints are hit?
        // Let's check total width too?
        // Grid Measure returns Sum of ActualWidths.
        // If Col 1 expands to 50 (from 10), Total = 90 + 50 = 140?
        // Wait, Available is 100.
        // Does it respect available size?
        // Measure logic:
        // 1. Calc Star shares based on Available.
        // 2. Apply Min/Max.
        // 3. Sum total.

        // So yes, it can exceed available size if MinWidth forces it.
        // Or be less if MaxWidth constrains it.
    }

    [Fact]
    public void Grid_Nesting_Works()
    {
        var outer = new Grid();
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Pixel(50) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var inner = new Grid();
        inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Pixel(10) });
        inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        outer.AddChild(inner);
        Grid.SetColumn(inner, 1);

        outer.Measure(new Size(100, 100));
        outer.Arrange(new Rect(0, 0, 100, 100));

        // Outer Col 1 width = 50.
        // Inner receives 50x100.
        // Inner Row 0 = 10. Row 1 = 40.

        Assert.Equal(50, inner.RenderSize.Width);
        Assert.Equal(100, inner.RenderSize.Height); // Height stretches to fill cell by default?
        // UIElement VerticalAlignment defaults to Stretch. Grid Arrange stretches child.

        Assert.Equal(10, inner.RowDefinitions[0].ActualHeight);
        Assert.Equal(90, inner.RowDefinitions[1].ActualHeight);
    }
}
