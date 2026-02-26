using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class CollectionParentingTests
{
    private class ViewModel
    {
        public string Text { get; set; } = "Inherited";
    }

    [Fact]
    public void StackPanel_Children_Add_SetsParent()
    {
        var stack = new StackPanel();
        var tb = new TextBlock();

        stack.Children.Add(tb);

        Assert.Same(stack, tb.Parent);
    }

    [Fact]
    public void StackPanel_Children_Remove_ClearsParent()
    {
        var stack = new StackPanel();
        var tb = new TextBlock();
        stack.Children.Add(tb);

        stack.Children.Remove(tb);

        Assert.Null(tb.Parent);
    }

    [Fact]
    public void Grid_Children_Add_SetsParent()
    {
        var grid = new Grid();
        var tb = new TextBlock();

        grid.Children.Add(tb);

        Assert.Same(grid, tb.Parent);
    }

    [Fact]
    public void DataContext_ShouldPropagate_Through_ChildrenAdd()
    {
        var stack = new StackPanel();
        var tb = new TextBlock();
        stack.Children.Add(tb);

        var vm = new ViewModel();
        stack.DataContext = vm;

        Assert.Same(vm, tb.DataContext);
    }

    [Fact]
    public void DataContext_ShouldPropagate_WhenChildAddedAfterContextSet()
    {
        var stack = new StackPanel();
        var vm = new ViewModel();
        stack.DataContext = vm;

        var tb = new TextBlock();
        stack.Children.Add(tb);

        Assert.Same(vm, tb.DataContext);
    }
}
