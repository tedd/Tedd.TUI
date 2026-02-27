using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class GridLengthTests
{
    [Fact]
    public void Auto_Property_ReturnsCorrectStruct()
    {
        var gl = GridLength.Auto;
        Assert.Equal(GridUnitType.Auto, gl.GridUnitType);
        Assert.Equal(1.0, gl.Value);
    }

    [Fact]
    public void Star_Property_ReturnsCorrectStruct()
    {
        var gl = GridLength.Star;
        Assert.Equal(GridUnitType.Star, gl.GridUnitType);
        Assert.Equal(1.0, gl.Value);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(0)]
    [InlineData(-10)] // Struct allows negative values, logic might clamp later
    public void Pixel_Method_ReturnsCorrectStruct(int pixels)
    {
        var gl = GridLength.Pixel(pixels);
        Assert.Equal(GridUnitType.Pixel, gl.GridUnitType);
        Assert.Equal((double)pixels, gl.Value);
    }

    [Theory]
    [InlineData(2.5, GridUnitType.Star)]
    [InlineData(100.0, GridUnitType.Pixel)]
    [InlineData(1.0, GridUnitType.Auto)]
    public void Constructor_SetsValuesCorrectly(double value, GridUnitType type)
    {
        var gl = new GridLength(value, type);
        Assert.Equal(type, gl.GridUnitType);
        Assert.Equal(value, gl.Value);
    }
}
