using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests.Geometry;

public class ThicknessTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-10)]
    public void UniformConstructor_SetsAllSides(int length)
    {
        // Act
        var thickness = new Thickness(length);

        // Assert
        Assert.Equal(length, thickness.Left);
        Assert.Equal(length, thickness.Top);
        Assert.Equal(length, thickness.Right);
        Assert.Equal(length, thickness.Bottom);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 2, 3, 4)]
    [InlineData(-1, -2, -3, -4)]
    public void FourParameterConstructor_SetsIndividualSides(int left, int top, int right, int bottom)
    {
        // Act
        var thickness = new Thickness(left, top, right, bottom);

        // Assert
        Assert.Equal(left, thickness.Left);
        Assert.Equal(top, thickness.Top);
        Assert.Equal(right, thickness.Right);
        Assert.Equal(bottom, thickness.Bottom);
    }

    [Theory]
    [InlineData(1, 2, 3, 4, 1, 2, 3, 4, true)]
    [InlineData(1, 2, 3, 4, 0, 2, 3, 4, false)]
    [InlineData(1, 2, 3, 4, 1, 0, 3, 4, false)]
    [InlineData(1, 2, 3, 4, 1, 2, 0, 4, false)]
    [InlineData(1, 2, 3, 4, 1, 2, 3, 0, false)]
    public void Equals_Thickness_ReturnsExpectedResult(int l1, int t1, int r1, int b1, int l2, int t2, int r2, int b2, bool expected)
    {
        // Arrange
        var tA = new Thickness(l1, t1, r1, b1);
        var tB = new Thickness(l2, t2, r2, b2);

        // Act
        bool result = tA.Equals(tB);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 2, 3, 4, 1, 2, 3, 4, true)]
    [InlineData(1, 2, 3, 4, 0, 2, 3, 4, false)]
    public void Equals_Object_ReturnsExpectedResult(int l1, int t1, int r1, int b1, int l2, int t2, int r2, int b2, bool expected)
    {
        // Arrange
        var tA = new Thickness(l1, t1, r1, b1);
        object tB = new Thickness(l2, t2, r2, b2);

        // Act
        bool result = tA.Equals(tB);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Equals_Object_DifferentType_ReturnsFalse()
    {
        // Arrange
        var tA = new Thickness(1, 2, 3, 4);
        object other = new object();

        // Act
        bool result = tA.Equals(other);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_Object_Null_ReturnsFalse()
    {
        // Arrange
        var tA = new Thickness(1, 2, 3, 4);
        object? other = null;

        // Act
        bool result = tA.Equals(other);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(1, 2, 3, 4, 1, 2, 3, 4, true)]
    [InlineData(1, 2, 3, 4, 0, 2, 3, 4, false)]
    public void OperatorEquality_ReturnsExpectedResult(int l1, int t1, int r1, int b1, int l2, int t2, int r2, int b2, bool expected)
    {
        // Arrange
        var tA = new Thickness(l1, t1, r1, b1);
        var tB = new Thickness(l2, t2, r2, b2);

        // Act
        bool result = tA == tB;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 2, 3, 4, 1, 2, 3, 4, false)]
    [InlineData(1, 2, 3, 4, 0, 2, 3, 4, true)]
    public void OperatorInequality_ReturnsExpectedResult(int l1, int t1, int r1, int b1, int l2, int t2, int r2, int b2, bool expected)
    {
        // Arrange
        var tA = new Thickness(l1, t1, r1, b1);
        var tB = new Thickness(l2, t2, r2, b2);

        // Act
        bool result = tA != tB;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 2, 3, 4)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(-1, -2, -3, -4)]
    public void GetHashCode_ReturnsConsistentValue(int left, int top, int right, int bottom)
    {
        // Arrange
        var tA = new Thickness(left, top, right, bottom);
        var tB = new Thickness(left, top, right, bottom);

        // Act
        int hashA = tA.GetHashCode();
        int hashB = tB.GetHashCode();

        // Assert
        Assert.Equal(hashA, hashB);
    }

    [Theory]
    [InlineData(10, 20, 30, 40)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(-5, -5, -5, -5)]
    public void Properties_SetAndGetCorrectly(int left, int top, int right, int bottom)
    {
        // Arrange
        var thickness = new Thickness();

        // Act
        thickness.Left = left;
        thickness.Top = top;
        thickness.Right = right;
        thickness.Bottom = bottom;

        // Assert
        Assert.Equal(left, thickness.Left);
        Assert.Equal(top, thickness.Top);
        Assert.Equal(right, thickness.Right);
        Assert.Equal(bottom, thickness.Bottom);
    }
}
