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
        Assert.Equal(System.ConsoleColor.Red, tb.Foreground);
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
