using System;
using Xunit;
using Tedd.TUI;
using System.Collections.Generic;
using System.Linq;

namespace Tedd.TUI.Tests;

public class ListBoxTests
{
    [Fact]
    public void SelectionChange_ShouldInvalidate()
    {
        var window = new TuiWindow();
        var listBox = new ListBox();
        window.Content = listBox;
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        bool invalidated = false;
        window.VisualChanged += (s, e) => invalidated = true;

        // Action
        listBox.SelectedIndex = 1;

        // Assert
        Assert.True(invalidated, "Changing SelectedIndex should trigger Invalidate via VisualChanged on Window");
    }

    [Fact]
    public void KeyDown_Arrow_ShouldInvalidate()
    {
        var window = new TuiWindow();
        var listBox = new ListBox();
        window.Content = listBox;
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.SelectedIndex = 0;

        bool invalidated = false;
        window.VisualChanged += (s, e) => invalidated = true;

        // Action
        listBox.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.DownArrow });

        // Assert
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(invalidated, "Arrow key selection change should trigger Invalidate via VisualChanged on Window");
    }

    [Fact]
    public void Render_UsesItemTemplate_WhenSet()
    {
        var listBox = new ListBox();
        listBox.Width = 10;
        listBox.Height = 2;

        var template = new DataTemplate(() =>
        {
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            return tb;
        });
        listBox.ItemTemplate = template;

        listBox.Items.Add(new { Name = "Alice" });
        listBox.Items.Add(new { Name = "Bob" });

        listBox.Measure(new Size(10, 2));
        listBox.Arrange(new Rect(0, 0, 10, 2));

        var buffer = new VirtualBuffer(10, 2);
        listBox.Render(buffer, 0, 0);

        // The template renders a TextBlock bound to "Name" – verify the bound text appears in the buffer
        var row0 = string.Concat(Enumerable.Range(0, 5).Select(dx => buffer.GetPixel(dx, 0).Character));
        var row1 = string.Concat(Enumerable.Range(0, 3).Select(dx => buffer.GetPixel(dx, 1).Character));

        Assert.Equal("Alice", row0);
        Assert.Equal("Bob", row1);
    }

    [Fact]
    public void Render_FallsBackToGetItemText_WhenNoTemplate()
    {
        var listBox = new ListBox();
        listBox.Width = 10;
        listBox.Height = 2;
        listBox.Items.Add("Hello");
        listBox.Items.Add("World");

        listBox.Measure(new Size(10, 2));
        listBox.Arrange(new Rect(0, 0, 10, 2));

        var buffer = new VirtualBuffer(10, 2);
        listBox.Render(buffer, 0, 0);

        var row0 = string.Concat(Enumerable.Range(0, 5).Select(dx => buffer.GetPixel(dx, 0).Character));
        var row1 = string.Concat(Enumerable.Range(0, 5).Select(dx => buffer.GetPixel(dx, 1).Character));

        Assert.Equal("Hello", row0);
        Assert.Equal("World", row1);
    }

    [Fact]
    public void ItemIsSelected_ViaIsSelectedProperty_UpdatesListBoxSelectedIndex()
    {
        var listBox = new ListBox();
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.Items.Add("Item 3");

        // ItemsPanelRoot is populated after items are added (via OnItemsCollectionChanged → PopulatePanel)
        Assert.NotNull(listBox.ItemsPanelRoot);
        var container1 = listBox.ItemsPanelRoot!.Children[1] as ListBoxItem;
        Assert.NotNull(container1);

        // Act: set IsSelected = true on the second container; this raises SelectedEvent which bubbles to ListBox
        container1!.IsSelected = true;

        // Assert: SelectedIndex should be updated to match the container's position
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void ItemSelectedEvent_RaisedOnContainer_UpdatesListBoxSelectedIndex()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");

        Assert.NotNull(listBox.ItemsPanelRoot);
        var container2 = listBox.ItemsPanelRoot!.Children[2] as ListBoxItem;
        Assert.NotNull(container2);

        // Act: raise SelectedEvent directly on the third container
        container2!.RaiseEvent(new RoutedEventArgs(ListBoxItem.SelectedEvent, container2));

        // Assert: SelectedIndex should be updated to 2
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_Change_Updates_ContainersIsSelected()
    {
        var listBox = new ListBox();
        listBox.Items.Add("Alpha");
        listBox.Items.Add("Beta");
        listBox.Items.Add("Gamma");

        Assert.NotNull(listBox.ItemsPanelRoot);

        // Act: select the second item
        listBox.SelectedIndex = 1;

        var container0 = listBox.ItemsPanelRoot!.Children[0] as ListBoxItem;
        var container1 = listBox.ItemsPanelRoot!.Children[1] as ListBoxItem;
        var container2 = listBox.ItemsPanelRoot!.Children[2] as ListBoxItem;

        Assert.NotNull(container0);
        Assert.NotNull(container1);
        Assert.NotNull(container2);

        Assert.False(container0!.IsSelected);
        Assert.True(container1!.IsSelected);
        Assert.False(container2!.IsSelected);
    }

    [Fact]
    public void SelectedIndex_Change_Deselects_PreviousContainer()
    {
        var listBox = new ListBox();
        listBox.Items.Add("X");
        listBox.Items.Add("Y");
        listBox.Items.Add("Z");

        Assert.NotNull(listBox.ItemsPanelRoot);

        // Select the first item
        listBox.SelectedIndex = 0;
        var container0 = listBox.ItemsPanelRoot!.Children[0] as ListBoxItem;
        var container1 = listBox.ItemsPanelRoot!.Children[1] as ListBoxItem;
        Assert.NotNull(container0);
        Assert.NotNull(container1);
        Assert.True(container0!.IsSelected);
        Assert.False(container1!.IsSelected);

        // Act: change selection to the second item
        listBox.SelectedIndex = 1;

        // Previous container should be deselected, new one selected
        Assert.False(container0.IsSelected);
        Assert.True(container1.IsSelected);
    }

    [Fact]
    public void SelectedIndex_MinusOne_Deselects_AllContainers()
    {
        var listBox = new ListBox();
        listBox.Items.Add("One");
        listBox.Items.Add("Two");

        Assert.NotNull(listBox.ItemsPanelRoot);

        listBox.SelectedIndex = 0;
        var container0 = listBox.ItemsPanelRoot!.Children[0] as ListBoxItem;
        var container1 = listBox.ItemsPanelRoot!.Children[1] as ListBoxItem;
        Assert.NotNull(container0);
        Assert.NotNull(container1);
        Assert.True(container0!.IsSelected);

        // Act: clear selection
        listBox.SelectedIndex = -1;

        Assert.False(container0.IsSelected);
        Assert.False(container1!.IsSelected);
    }
}
