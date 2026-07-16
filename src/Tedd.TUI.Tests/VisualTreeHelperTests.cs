using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class VisualTreeHelperTests
{
    [Fact]
    public void GetChildrenCount_ReturnsCorrectCount()
    {
        var grid = new Grid();
        var tb1 = new TextBlock();
        var tb2 = new TextBlock();
        grid.Children.Add(tb1);
        grid.Children.Add(tb2);

        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(grid));
        Assert.Equal(0, VisualTreeHelper.GetChildrenCount(tb1));
    }

    [Fact]
    public void GetChildrenCount_NullReference_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VisualTreeHelper.GetChildrenCount(null!));
    }

    [Fact]
    public void GetChildrenCount_NonUIElement_ReturnsZero()
    {
        var dp = new MockDependencyObject();
        Assert.Equal(0, VisualTreeHelper.GetChildrenCount(dp));
    }

    [Fact]
    public void GetChild_ReturnsCorrectChild()
    {
        var grid = new Grid();
        var tb1 = new TextBlock();
        var tb2 = new TextBlock();
        grid.Children.Add(tb1);
        grid.Children.Add(tb2);

        var child1 = VisualTreeHelper.GetChild(grid, 0);
        var child2 = VisualTreeHelper.GetChild(grid, 1);

        Assert.Same(tb1, child1);
        Assert.Same(tb2, child2);
    }

    [Fact]
    public void GetChild_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var tb = new TextBlock();
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualTreeHelper.GetChild(tb, 0));

        var grid = new Grid();
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualTreeHelper.GetChild(grid, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualTreeHelper.GetChild(grid, -1));
    }

    [Fact]
    public void GetChild_NullReference_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VisualTreeHelper.GetChild(null!, 0));
    }

    [Fact]
    public void GetChild_NonUIElement_ThrowsArgumentOutOfRangeException()
    {
        var dp = new MockDependencyObject();
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualTreeHelper.GetChild(dp, 0));
    }

    [Fact]
    public void GetParent_ReturnsCorrectParent()
    {
        var grid = new Grid();
        var tb = new TextBlock();
        grid.Children.Add(tb);

        var parent = VisualTreeHelper.GetParent(tb);
        Assert.Same(grid, parent);
    }

    [Fact]
    public void GetParent_NoParent_ReturnsNull()
    {
        var tb = new TextBlock();
        Assert.Null(VisualTreeHelper.GetParent(tb));
    }

    [Fact]
    public void GetParent_NullReference_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VisualTreeHelper.GetParent(null!));
    }

    [Fact]
    public void GetParent_NonUIElement_ReturnsNull()
    {
        var dp = new MockDependencyObject();
        Assert.Null(VisualTreeHelper.GetParent(dp));
    }

    private class MockDependencyObject : DependencyObject
    {
    }
}
