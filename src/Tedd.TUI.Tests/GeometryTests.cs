using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class GeometryTests
{
    [Fact]
    public void Point_Constructor_SetsProperties()
    {
        var p = new Point(10, 20);
        Assert.Equal(10, p.X);
        Assert.Equal(20, p.Y);
    }

    [Fact]
    public void Point_Properties_CanBeSet()
    {
        var p = new Point();
        p.X = 5;
        p.Y = 15;
        Assert.Equal(5, p.X);
        Assert.Equal(15, p.Y);
    }

    [Fact]
    public void Size_Constructor_SetsProperties()
    {
        var s = new Size(100, 200);
        Assert.Equal(100, s.Width);
        Assert.Equal(200, s.Height);
    }

    [Fact]
    public void Size_Properties_CanBeSet()
    {
        var s = new Size();
        s.Width = 50;
        s.Height = 150;
        Assert.Equal(50, s.Width);
        Assert.Equal(150, s.Height);
    }

    [Fact]
    public void Rect_Constructor1_SetsProperties()
    {
        var r = new Rect(1, 2, 3, 4);
        Assert.Equal(1, r.X);
        Assert.Equal(2, r.Y);
        Assert.Equal(3, r.Width);
        Assert.Equal(4, r.Height);
    }

    [Fact]
    public void Rect_Constructor2_SetsProperties()
    {
        var p = new Point(5, 6);
        var s = new Size(7, 8);
        var r = new Rect(p, s);
        Assert.Equal(5, r.X);
        Assert.Equal(6, r.Y);
        Assert.Equal(7, r.Width);
        Assert.Equal(8, r.Height);
    }

    [Fact]
    public void Rect_Properties_CanBeSet()
    {
        var r = new Rect();
        r.X = 10;
        r.Y = 20;
        r.Width = 30;
        r.Height = 40;
        Assert.Equal(10, r.X);
        Assert.Equal(20, r.Y);
        Assert.Equal(30, r.Width);
        Assert.Equal(40, r.Height);
    }
}
