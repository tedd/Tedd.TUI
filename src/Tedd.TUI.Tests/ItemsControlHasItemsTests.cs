using System;
using System.Collections.ObjectModel;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class ItemsControlHasItemsTests
{
    [Fact]
    public void HasItems_UpdatesWhenItemsChange()
    {
        // ItemsControl is abstract, use a concrete derived class like ListBox or create a dummy one
        var control = new ListBox();
        Assert.False(control.HasItems);

        control.Items.Add("Test 1");
        Assert.True(control.HasItems);

        control.Items.Remove("Test 1");
        Assert.False(control.HasItems);
    }

    [Fact]
    public void HasItems_UpdatesWhenItemsSourceChanges()
    {
        var control = new ListBox();
        Assert.False(control.HasItems);

        var source = new ObservableCollection<string> { "Test 1" };
        control.ItemsSource = source;
        Assert.True(control.HasItems);

        source.Add("Test 2");
        Assert.True(control.HasItems);

        source.Clear();
        Assert.False(control.HasItems);

        control.ItemsSource = null;
        Assert.False(control.HasItems);
    }
}
