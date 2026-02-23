using System.Collections.Generic;
using Xunit;

namespace Tedd.TUI.Tests;

public class TreeViewTests
{
    [Fact]
    public void CanCreateTreeView()
    {
        var tree = new TreeView();
        Assert.NotNull(tree.Items);
    }

    [Fact]
    public void CanAddItemsManually()
    {
        var tree = new TreeView();
        var item1 = new TreeViewItem { Header = "Root" };
        var item2 = new TreeViewItem { Header = "Child" };
        item1.Items.Add(item2);
        tree.Items.Add(item1);

        Assert.Equal(1, tree.Items.Count);
        Assert.Single(item1.Items);
        Assert.Equal("Root", item1.Header);
        Assert.Equal("Child", item2.Header);
    }

    [Fact]
    public void DataContextPropagatesToChildren()
    {
        var tree = new TreeView();
        var item1 = new TreeViewItem { Header = "Root" };
        var item2 = new TreeViewItem { Header = "Child" };
        item1.Items.Add(item2);
        tree.Items.Add(item1);

        var context = new object();
        tree.DataContext = context;

        // Logical propagation relies on ParentItem linkage
        Assert.Equal(context, item1.DataContext);

        // Ensure child gets context via inheritance
        Assert.Equal(context, item2.DataContext);
    }

    [Fact]
    public void SelectionUpdates()
    {
        var tree = new TreeView();
        var item1 = new TreeViewItem { Header = "Root" };
        tree.Items.Add(item1);

        tree.SelectedItem = item1;
        Assert.True(item1.IsSelected);

        tree.SelectedItem = null;
        Assert.False(item1.IsSelected);
    }

    class Node
    {
        public string Name { get; set; }
        public List<Node> Children { get; set; }
    }

    [Fact]
    public void ItemsSourceGeneratesItems()
    {
        var tree = new TreeView();
        var data = new List<Node>
        {
            new Node
            {
                Name = "Root",
                Children = new List<Node>
                {
                    new Node { Name = "Child" }
                }
            }
        };

        tree.DisplayMemberPath = "Name";
        tree.ChildItemsPath = "Children";
        tree.ItemsSource = data;

        Assert.Single(tree.Items);
        var rootItem = tree.Items[0];
        Assert.Equal("Root", rootItem.Header);

        Assert.Single(rootItem.Items);
        var childItem = rootItem.Items[0];
        Assert.Equal("Child", childItem.Header);

        // Check data context of items
        Assert.Equal(data[0], rootItem.DataContext);
        Assert.Equal(data[0].Children[0], childItem.DataContext);
    }
}
