using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class XamlLoaderTests
{
    [Fact]
    public void TestLoadSimple()
    {
        string xaml = "<TextBlock Text='Hello XAML' Foreground='Red' />";
        var element = XamlLoader.Load(xaml);

        Assert.IsType<TextBlock>(element);
        var tb = (TextBlock)element;
        Assert.Equal("Hello XAML", tb.Text);
        Assert.Equal(TuiColor.Red, tb.Foreground);
    }

    [Theory]
    [InlineData("#FF0000", 255, 0, 0, 255)]
    [InlineData("#00FF00FF", 0, 255, 0, 255)]
    [InlineData("#0000FF80", 0, 0, 255, 128)]
    [InlineData("#F00", 255, 0, 0, 255)]
    [InlineData("rgb(10,20,30)", 10, 20, 30, 255)]
    [InlineData("rgba(10,20,30,0.5)", 10, 20, 30, 128)]
    public void TestLoad_TuiColor_HexAndRgba(string text, int r, int g, int b, int a)
    {
        string xaml = $"<TextBlock Text='x' Foreground=\"{text}\" />";
        var element = XamlLoader.Load(xaml);
        var tb = (TextBlock)element;

        Assert.Equal((byte)r, tb.Foreground.R);
        Assert.Equal((byte)g, tb.Foreground.G);
        Assert.Equal((byte)b, tb.Foreground.B);
        Assert.Equal((byte)a, tb.Foreground.A);
    }

    [Fact]
    public void TestLoad_TuiColor_LegacyNameStillWorks()
    {
        string xaml = "<TextBlock Text='x' Foreground='Cyan' />";
        var element = XamlLoader.Load(xaml);
        var tb = (TextBlock)element;
        Assert.Equal(TuiColor.Cyan, tb.Foreground);
    }

    [Fact]
    public void TestLoadNested()
    {
        string xaml = @"
<StackPanel Orientation='Vertical'>
    <TextBlock Text='Top' />
    <TextBlock Text='Bottom' />
</StackPanel>";

        var element = XamlLoader.Load(xaml);
        Assert.IsType<StackPanel>(element);
        var stack = (StackPanel)element;
        Assert.Equal(2, stack.Children.Count);
        Assert.Equal("Top", ((TextBlock)stack.Children[0]).Text);
    }
}
