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
    public void TestLoad_RadioButton_IsChecked_NullableBool()
    {
        string xaml = "<RadioButton Content='Male' GroupName='Gender' IsChecked='True' />";
        var element = XamlLoader.Load(xaml);

        var rb = Assert.IsType<RadioButton>(element);
        Assert.True(rb.IsChecked);
    }

    [Fact]
    public void TestLoad_DesignerStyleDocument_NamespacesAndDesignerAttributesIgnored()
    {
        // A file authored for a XAML editor: default xmlns, x:, d:, mc: namespaces,
        // mc:Ignorable and design-time size hints must all be tolerated.
        string xaml = @"
<TuiWindow xmlns='urn:tedd-tui'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
           xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
           mc:Ignorable='d' d:DesignWidth='80' d:DesignHeight='25'>
    <StackPanel Orientation='Vertical'>
        <TextBlock x:Name='Title' Text='Hello' />
        <Button Content='OK' />
    </StackPanel>
</TuiWindow>";

        var element = XamlLoader.Load(xaml);

        var window = Assert.IsType<TuiWindow>(element);
        var stack = Assert.IsType<StackPanel>(window.Content);
        var tb = Assert.IsType<TextBlock>(stack.Children[0]);
        Assert.Equal("Title", tb.Name);
        Assert.Equal("Hello", tb.Text);
    }

    [Fact]
    public void TestLoad_PrefixedElements_ResolveByLocalName()
    {
        string xaml = @"
<tui:StackPanel xmlns:tui='urn:tedd-tui' Orientation='Horizontal'>
    <tui:TextBlock Text='A' />
    <tui:Grid tui:Grid.Row='0'>
        <tui:TextBlock Text='B' tui:Grid.Row='0' />
    </tui:Grid>
</tui:StackPanel>";

        var element = XamlLoader.Load(xaml);

        var stack = Assert.IsType<StackPanel>(element);
        Assert.Equal(2, stack.Children.Count);
        Assert.Equal("A", ((TextBlock)stack.Children[0]).Text);
        Assert.IsType<Grid>(stack.Children[1]);
    }

    [Fact]
    public void TestLoad_XNameInjectsIntoControllerField()
    {
        string xaml = @"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock x:Name='StatusText' Text='Ready' />
</StackPanel>";

        var controller = new NameInjectionController();
        XamlLoader.Load(xaml, controller);

        Assert.NotNull(controller.StatusText);
        Assert.Equal("Ready", controller.StatusText!.Text);
    }

    private class NameInjectionController
    {
#pragma warning disable CS0649 // assigned via reflection by XamlLoader
        public TextBlock? StatusText;
#pragma warning restore CS0649
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
