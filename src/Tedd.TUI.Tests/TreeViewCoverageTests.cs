using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TreeViewCoverageTests
{
    private class TestNode
    {
        public string Name { get; set; } = "";
        public List<TestNode> Children { get; set; } = new();
    }

    [Fact]
    public void TreeView_OnKeyDown_DownArrow_NavigatesToNextVisibleItem()
    {
        var tree = new TreeView();
        var root1 = new TreeViewItem { Header = "Root1" };
        var root2 = new TreeViewItem { Header = "Root2" };
        tree.Items.Add(root1);
        tree.Items.Add(root2);

        tree.Measure(new Size(100, 100)); // Force visual tree build

        tree.SelectedItem = root1;

        var args = new KeyEventArgs { Key = ConsoleKey.DownArrow };
        tree.OnKeyDown(args);

        Assert.True(args.Handled);
        Assert.Equal(root2, tree.SelectedItem);
    }

    [Fact]
    public void TreeView_OnKeyDown_UpArrow_NavigatesToPreviousVisibleItem()
    {
        var tree = new TreeView();
        var root1 = new TreeViewItem { Header = "Root1" };
        var root2 = new TreeViewItem { Header = "Root2" };
        tree.Items.Add(root1);
        tree.Items.Add(root2);

        tree.Measure(new Size(100, 100));

        tree.SelectedItem = root2;

        var args = new KeyEventArgs { Key = ConsoleKey.UpArrow };
        tree.OnKeyDown(args);

        Assert.True(args.Handled);
        Assert.Equal(root1, tree.SelectedItem);
    }

    [Fact]
    public void TreeView_OnKeyDown_RightArrow_ExpandsItemOrNavigatesToChild()
    {
        var tree = new TreeView();
        var root = new TreeViewItem { Header = "Root" };
        var child = new TreeViewItem { Header = "Child" };
        root.Items.Add(child);
        tree.Items.Add(root);

        tree.Measure(new Size(100, 100));
        tree.SelectedItem = root;

        // First press expands
        var args1 = new KeyEventArgs { Key = ConsoleKey.RightArrow };
        tree.OnKeyDown(args1);
        Assert.True(args1.Handled);
        Assert.True(root.IsExpanded);
        Assert.Equal(root, tree.SelectedItem);

        tree.Measure(new Size(100, 100)); // Rebuild visual tree after expand

        // Second press navigates to child
        var args2 = new KeyEventArgs { Key = ConsoleKey.RightArrow };
        tree.OnKeyDown(args2);
        Assert.True(args2.Handled);
        Assert.Equal(child, tree.SelectedItem);
    }

    [Fact]
    public void TreeView_OnKeyDown_LeftArrow_CollapsesItemOrNavigatesToParent()
    {
        var tree = new TreeView();
        var root = new TreeViewItem { Header = "Root", IsExpanded = true };
        var child = new TreeViewItem { Header = "Child" };
        root.Items.Add(child);
        tree.Items.Add(root);

        tree.Measure(new Size(100, 100));
        tree.SelectedItem = child;

        // First press on child navigates to parent
        var args1 = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        tree.OnKeyDown(args1);
        Assert.True(args1.Handled);
        Assert.Equal(root, tree.SelectedItem);
        Assert.True(root.IsExpanded);

        // Second press on expanded parent collapses it
        var args2 = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        tree.OnKeyDown(args2);
        Assert.True(args2.Handled);
        Assert.False(root.IsExpanded);
    }

    [Fact]
    public void TreeView_OnKeyDown_Enter_TogglesExpandedState()
    {
        var tree = new TreeView();
        var root = new TreeViewItem { Header = "Root" };
        root.Items.Add(new TreeViewItem { Header = "Child" });
        tree.Items.Add(root);

        tree.Measure(new Size(100, 100));
        tree.SelectedItem = root;

        Assert.False(root.IsExpanded);

        var args = new KeyEventArgs { Key = ConsoleKey.Enter };
        tree.OnKeyDown(args);

        Assert.True(args.Handled);
        Assert.True(root.IsExpanded);
    }

    [Fact]
    public void TreeView_OnKeyDown_NoSelection_SelectsFirstItem()
    {
        var tree = new TreeView();
        var root = new TreeViewItem { Header = "Root" };
        tree.Items.Add(root);

        tree.Measure(new Size(100, 100));

        var args = new KeyEventArgs { Key = ConsoleKey.DownArrow };
        tree.OnKeyDown(args);

        Assert.True(args.Handled);
        Assert.Equal(root, tree.SelectedItem);
    }

    [Fact]
    public void TreeView_EnsureVisible_ScrollsWhenOutOfViewport()
    {
        var tree = new TreeView();
        for (int i = 0; i < 20; i++)
        {
            tree.Items.Add(new TreeViewItem { Header = $"Item {i}" });
        }

        tree.Measure(new Size(20, 10)); // Viewport height is 10
        tree.Arrange(new Rect(0, 0, 20, 10));

        // Select item outside viewport (index 15)
        tree.SelectedItem = (TreeViewItem)tree.Items[15];

        var scrollViewer = (ScrollViewer)tree.GetVisualChild(0);
        Assert.True(scrollViewer.VerticalOffset > 0);
    }
    [Fact]
    public void TreeView_Caching_DisplayMemberPath_Invalidates()
    {
        var tree = new TreeView();
        var data = new List<TestNode> { new TestNode { Name = "Node1" } };

        // Ensure DisplayMemberPath is set BEFORE ItemsSource to generate correctly
        tree.DisplayMemberPath = "Name";
        tree.ItemsSource = data;

        Assert.Single(tree.Items);
        Assert.Equal("Node1", ((TreeViewItem)tree.Items[0]).Header);

        // Change data structure and path to test cache clearance
        var data2 = new List<object> { new { OtherName = "Node2" } };
        tree.DisplayMemberPath = "OtherName";
        tree.ItemsSource = data2;

        Assert.Single(tree.Items);
        Assert.Equal("Node2", ((TreeViewItem)tree.Items[0]).Header);
    }

    [Fact]
    public void TreeView_Caching_ChildItemsPath_Invalidates()
    {
        var tree = new TreeView();
        var data = new List<TestNode>
        {
            new TestNode
            {
                Name = "Node1",
                Children = new List<TestNode> { new TestNode { Name = "Child1" } }
            }
        };

        tree.ChildItemsPath = "Children";
        tree.ItemsSource = data;

        Assert.Single(tree.Items);
        var item1 = (TreeViewItem)tree.Items[0];
        Assert.Single(item1.Items);

        // Clear and change
        tree.ChildItemsPath = "";
        tree.ItemsSource = new List<TestNode> { new TestNode { Name = "Node2" } }; // Re-generate
        Assert.Single(tree.Items);
        Assert.Empty(((TreeViewItem)tree.Items[0]).Items);
    }

    [Fact]
    public void TreeView_OnItemsChanged_SubscribesAndUnsubscribes()
    {
        var tree = new TreeView();
        var item = new TreeViewItem { Header = "Root" };
        var child = new TreeViewItem { Header = "Child" };
        item.Items.Add(child);

        // Trigger SubscribeItem
        tree.Items.Add(item);
        tree.Measure(new Size(100, 100)); // Build visual tree initially

        // The item's parent becomes the StackPanel used internally by TreeView
        Assert.IsType<StackPanel>(item.Parent);

        var stackPanel = tree.GetVisualChild(0) as ScrollViewer;
        var panel = stackPanel?.Content as StackPanel;

        int initialChildren = panel?.Children.Count ?? 0;

        item.IsExpanded = true;

        // Because it's expanded, the child should now be added to the visual tree
        Assert.True(panel?.Children.Count > initialChildren);

        // Trigger UnsubscribeItem
        tree.Items.Remove(item);

        // State change after unsubscribe shouldn't impact visual tree rebuilding (since it's removed)
        int childrenAfterRemove = panel?.Children.Count ?? 0;
        item.IsExpanded = false;
        Assert.Equal(childrenAfterRemove, panel?.Children.Count ?? 0);
    }

    [Fact]
    public void TreeViewItem_ExpandedCollapsed_EventsTriggered()
    {
        var item = new TreeViewItem();
        bool expanded = false;
        bool collapsed = false;

        item.Expanded += (s, e) => expanded = true;
        item.Collapsed += (s, e) => collapsed = true;

        item.IsExpanded = true;
        Assert.True(expanded);

        item.IsExpanded = false;
        Assert.True(collapsed);
    }

    [Fact]
    public void TreeViewItem_SelectedUnselected_EventsTriggered()
    {
        var item = new TreeViewItem();
        bool selected = false;
        bool unselected = false;

        item.Selected += (s, e) => selected = true;
        item.Unselected += (s, e) => unselected = true;

        item.IsSelected = true;
        Assert.True(selected);

        item.IsSelected = false;
        Assert.True(unselected);
    }

    [Fact]
    public void TreeViewItem_CollectionChanged_UpdatesParentItem()
    {
        var parent = new TreeViewItem();
        var child = new TreeViewItem();

        parent.Items.Add(child);
        Assert.Equal(parent, child.ParentItem);

        parent.Items.Remove(child);
        Assert.Null(child.ParentItem);
    }

    [Fact]
    public void TreeViewItem_MeasureOverride_CalculatesWidth()
    {
        var item = new TreeViewItem { Header = "Test", Level = 2 };

        item.Measure(new Size(100, 100));

        // 2 (Level) * 2 + 4 (Indicator) + 4 (Length of "Test") = 12
        Assert.Equal(12, item.DesiredSize.Width);
        Assert.Equal(1, item.DesiredSize.Height);
    }

    [Theory]
    [InlineData(true, true, '-', ConsoleColor.Blue, ConsoleColor.White)]
    [InlineData(false, false, ' ', ConsoleColor.Black, ConsoleColor.Gray)]
    [InlineData(true, false, '+', ConsoleColor.Black, ConsoleColor.Gray)]
    public void TreeViewItem_Render_DrawsIndicatorAndHeader(bool hasItems, bool isExpanded, char expectedIndicatorChar, ConsoleColor expectedBg, ConsoleColor expectedFg)
    {
        var item = new TreeViewItem { Header = "Node", IsExpanded = isExpanded, IsSelected = (expectedBg == ConsoleColor.Blue) };
        if (hasItems)
        {
            item.Items.Add(new TreeViewItem());
        }

        var buffer = new VirtualBuffer(20, 1);
        item.Measure(new Size(20, 1));
        item.Arrange(new Rect(0, 0, 20, 1));

        item.Render(buffer, 0, 0);

        // Indicator check: x + 1 (middle char of the 3-char indicator space)
        var indicatorCell = buffer.GetPixel(1, 0);
        Assert.Equal(expectedIndicatorChar, indicatorCell.Character);

        // Background check (at header pos)
        var headerCell = buffer.GetPixel(4, 0); // "Node" starts at index 4 (0 padding + 4 indicator)
        Assert.Equal('N', headerCell.Character);
        Assert.Equal(expectedBg, headerCell.Background);
        Assert.Equal(expectedFg, headerCell.Foreground);
    }

    [Fact]
    public void TreeView_RebuildVisualTree_ClearsAndAddsVisibleItems()
    {
        var tree = new TreeView();
        var root1 = new TreeViewItem { Header = "Root1" };
        var child1 = new TreeViewItem { Header = "Child1" };
        var root2 = new TreeViewItem { Header = "Root2" };

        root1.Items.Add(child1);
        tree.Items.Add(root1);
        tree.Items.Add(root2);

        tree.Measure(new Size(100, 100));
        var stackPanel = tree.GetVisualChild(0) as ScrollViewer;
        var panel = stackPanel?.Content as StackPanel;

        // By default items are not expanded, so Child1 shouldn't be in the panel yet
        Assert.Equal(2, panel?.Children.Count);

        root1.IsExpanded = true;
        // The event handler calls RebuildVisualTree directly, no Measure needed
        Assert.Equal(3, panel?.Children.Count);
        Assert.Equal(1, child1.Level); // Verify AddVisibleItems sets Level
    }

    [Fact]
    public void TreeViewItem_InheritanceParent_UsesLogicalParent()
    {
        var parent = new TreeViewItem { DataContext = "ParentContext" };
        var child = new TreeViewItem();

        parent.Items.Add(child); // Sets child.ParentItem = parent

        // This relies on the InheritanceParent override propagating DataContext down logically.
        Assert.Equal("ParentContext", child.DataContext);
    }
}
