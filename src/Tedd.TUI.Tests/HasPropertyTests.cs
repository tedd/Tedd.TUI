using System;
using System.Collections.ObjectModel;
using Xunit;
using Tedd.TUI.Controls;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class HasPropertyTests
{
    [Fact]
    public void ContentControl_HasContent_UpdatesCorrectly()
    {
        var control = new ContentControl();
        Assert.False(control.HasContent);

        control.Content = "Test";
        Assert.True(control.HasContent);

        control.Content = null;
        Assert.False(control.HasContent);
    }

    [Fact]
    public void HeaderedContentControl_HasHeader_UpdatesCorrectly()
    {
        var control = new HeaderedContentControl();
        Assert.False(control.HasHeader);

        control.Header = "Test Header";
        Assert.True(control.HasHeader);

        control.Header = null;
        Assert.False(control.HasHeader);
    }

    [Fact]
    public void ItemsControl_HasItems_UpdatesCorrectly()
    {
        var control = new ItemsControlMock();
        Assert.False(control.HasItems);

        control.Items.Add("Item 1");
        Assert.True(control.HasItems);

        control.Items.Add("Item 2");
        Assert.True(control.HasItems);

        control.Items.Remove("Item 1");
        Assert.True(control.HasItems);

        control.Items.Clear();
        Assert.False(control.HasItems);
    }

    [Fact]
    public void ItemsControl_HasItems_UpdatesCorrectlyWithItemsSource()
    {
        var control = new ItemsControlMock();
        var source = new ObservableCollection<string>();
        control.ItemsSource = source;
        Assert.False(control.HasItems);

        source.Add("Item 1");
        Assert.True(control.HasItems);

        source.Remove("Item 1");
        Assert.False(control.HasItems);

        control.ItemsSource = null;
        Assert.False(control.HasItems);
    }

    private class ItemsControlMock : ItemsControl
    {
        // Concrete class for testing abstract ItemsControl
    }
}
