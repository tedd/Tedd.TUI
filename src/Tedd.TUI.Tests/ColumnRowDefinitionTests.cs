using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ColumnRowDefinitionTests
{
    [Fact]
    public void RowDefinition_Properties_Defaults()
    {
        var row = new RowDefinition();
        Assert.Equal(GridUnitType.Star, row.Height.GridUnitType);
        Assert.Equal(1.0, row.Height.Value);
        Assert.Equal(0, row.MinHeight);
        Assert.Equal(int.MaxValue, row.MaxHeight);
    }

    [Theory]
    [InlineData(10, 100)]
    [InlineData(0, int.MaxValue)]
    [InlineData(50, 50)]
    public void RowDefinition_Properties_Setters(int min, int max)
    {
        var row = new RowDefinition();
        row.MinHeight = min;
        row.MaxHeight = max;

        Assert.Equal(min, row.MinHeight);
        Assert.Equal(max, row.MaxHeight);
    }

    [Fact]
    public void ColumnDefinition_Properties_Defaults()
    {
        var col = new ColumnDefinition();
        Assert.Equal(GridUnitType.Star, col.Width.GridUnitType);
        Assert.Equal(1.0, col.Width.Value);
        Assert.Equal(0, col.MinWidth);
        Assert.Equal(int.MaxValue, col.MaxWidth);
    }

    [Theory]
    [InlineData(5, 500)]
    [InlineData(0, 100)]
    public void ColumnDefinition_Properties_Setters(int min, int max)
    {
        var col = new ColumnDefinition();
        col.MinWidth = min;
        col.MaxWidth = max;

        Assert.Equal(min, col.MinWidth);
        Assert.Equal(max, col.MaxWidth);
    }
}
