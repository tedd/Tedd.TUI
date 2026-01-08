using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class DataContextInheritanceTests
{
    public class ViewModel
    {
        public string Text { get; set; } = "Inherited";
    }

    [Fact]
    public void TestStackPanelInheritance()
    {
        var stack = new StackPanel();
        var tb = new TextBlock();
        stack.AddChild(tb);
        
        var vm = new ViewModel();
        stack.DataContext = vm;
        
        Assert.Same(vm, tb.DataContext);
        
        tb.SetBinding(TextBlock.TextProperty, new Binding("Text"));
        Assert.Equal("Inherited", tb.Text);
    }

    [Fact]
    public void TestBorderInheritance()
    {
        var border = new Border();
        var tb = new TextBlock();
        border.Child = tb;
        
        var vm = new ViewModel();
        border.DataContext = vm;
        
        Assert.Same(vm, tb.DataContext);
    }
}
