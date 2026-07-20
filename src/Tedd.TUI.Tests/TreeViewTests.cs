using System.Collections.Generic;
using Xunit;
using Tedd.TUI.Tests.TestInfrastructure;

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
    public void TreeViewItem_InheritsFromHeaderedItemsControl()
    {
        var item = new TreeViewItem();
        Assert.IsAssignableFrom<HeaderedItemsControl>(item);
    }

    [Fact]
    public void CanAddItemsManually()
    {
        var tree = new TreeView();
        var item1 = new TreeViewItem { Header = "Root" };
        var item2 = new TreeViewItem { Header = "Child" };
        item1.Items.Add(item2);
        tree.Items.Add(item1);

        Assert.Single(tree.Items);
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

    [Fact]
    public void InitialSelectionIsCoordinated()
    {
        var tree = new TreeView();
        var root = new TreeViewItem { Header = "Root" };
        var child1 = new TreeViewItem { Header = "Child1", IsSelected = true };
        var child2 = new TreeViewItem { Header = "Child2" };

        root.Items.Add(child1);
        root.Items.Add(child2);
        tree.Items.Add(root);

        // Verify initial selection coordinates correctly
        Assert.Equal(child1, tree.SelectedItem);
        Assert.True(child1.IsSelected);
        Assert.False(child2.IsSelected);

        // Selecting child2 should clear child1's selection
        tree.SelectedItem = child2;
        Assert.Equal(child2, tree.SelectedItem);
        Assert.True(child2.IsSelected);
        Assert.False(child1.IsSelected);
    }

    [Fact]
    public void ItemUnselectionClearsSelectedItem()
    {
        var tree = new TreeView();
        var item1 = new TreeViewItem { Header = "Root" };
        tree.Items.Add(item1);

        item1.IsSelected = true;
        Assert.Equal(item1, tree.SelectedItem);

        item1.IsSelected = false;
        Assert.Null(tree.SelectedItem);
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
        var rootItem = (TreeViewItem)tree.Items[0];
        Assert.Equal("Root", rootItem.Header);

        Assert.Single(rootItem.Items);
        var childItem = (TreeViewItem)rootItem.Items[0];
        Assert.Equal("Child", childItem.Header);

        // Check data context of items
        Assert.Equal(data[0], rootItem.DataContext);
        Assert.Equal(data[0].Children[0], childItem.DataContext);
    }

    [Fact]
    public void MouseClick_NestedTreeView_SelectsAndTogglesOnlyVisibleScrolledItem()
    {
        var rootA = new TreeViewItem { Header = "Root A", IsExpanded = true };
        var childA1 = new TreeViewItem { Header = "Child A1" };
        var childA2 = new TreeViewItem { Header = "Child A2" };
        rootA.Items.Add(childA1);
        rootA.Items.Add(childA2);
        var rootB = new TreeViewItem { Header = "Root B", IsExpanded = true };
        var childB1 = new TreeViewItem { Header = "Child B1" };
        var childB2 = new TreeViewItem { Header = "Child B2" };
        rootB.Items.Add(childB1);
        rootB.Items.Add(childB2);
        var tree = new TreeView { Width = 16, Height = 4 };
        tree.Items.Add(rootA);
        tree.Items.Add(rootB);

        var panel = new StackPanel();
        panel.AddChild(new TextBlock { Text = "tree" });
        panel.AddChild(tree);
        panel.AddChild(new TextBlock { Text = "status surface" });
        var host = new ControlTestHost(new Border { Child = panel, Padding = new Thickness(0) }, 20, 8);

        var childClick = host.Click(childA1, 7, 0);

        Assert.True(childClick.Down.Handled);
        Assert.True(tree.IsFocused);
        Assert.Same(childA1, tree.SelectedItem);
        Assert.True(rootA.IsExpanded);
        Assert.True(rootB.IsExpanded);

        var scrollViewer = (ScrollViewer)tree.GetVisualChild(0);
        scrollViewer.ScrollToVerticalOffset(2);
        var rootClick = host.Click(rootB, 6, 0);

        Assert.True(rootClick.Down.Handled);
        Assert.Same(rootB, tree.SelectedItem);
        Assert.True(rootA.IsExpanded);
        Assert.True(rootB.IsExpanded);

        host.Click(rootB, 1, 0);

        Assert.Same(rootB, tree.SelectedItem);
        Assert.True(rootA.IsExpanded);
        Assert.False(rootB.IsExpanded);
        Assert.False(childA1.IsSelected);
    }
}
