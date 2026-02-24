using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Tedd.TUI.Tests;

public class ItemsControlTests
{
    private class TestItemsControl : ItemsControl
    {
    }

    [Fact]
    public void ItemsSource_Populates_Items()
    {
        var control = new TestItemsControl();
        var source = new[] { "A", "B", "C" };

        control.ItemsSource = source;

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("B", control.Items[1]);
        Assert.Equal("C", control.Items[2]);
    }

    [Fact]
    public void ItemsSource_CollectionChanged_Updates_Items()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string>();

        control.ItemsSource = source;
        Assert.Empty(control.Items);

        source.Add("A");
        Assert.Single(control.Items);
        Assert.Equal("A", control.Items[0]);

        source.Add("B");
        Assert.Equal(2, control.Items.Count);

        source.Remove("A");
        Assert.Single(control.Items);
        Assert.Equal("B", control.Items[0]);

        source.Clear();
        Assert.Empty(control.Items);
    }

    [Fact]
    public void ItemsSource_Insert_Updates_Correct_Index()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "C" };
        control.ItemsSource = source;

        source.Insert(1, "B");

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("B", control.Items[1]);
        Assert.Equal("C", control.Items[2]);
    }

    [Fact]
    public void ItemsSource_RemoveAt_Updates_Correct_Index()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "B", "C" };
        control.ItemsSource = source;

        source.RemoveAt(1);

        Assert.Equal(2, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
    }

    [Fact]
    public void ItemsSource_Replace_Updates_Correct_Index()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "B", "C" };
        control.ItemsSource = source;

        source[1] = "D";

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("D", control.Items[1]);
        Assert.Equal("C", control.Items[2]);
    }

    [Fact]
    public void ItemsSource_Move_Updates_Items()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "B", "C" };
        control.ItemsSource = source;

        source.Move(0, 2); // Move A to end

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("B", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
        Assert.Equal("A", control.Items[2]);
    }

    [Fact]
    public void Items_IsReadOnly_When_ItemsSource_Set()
    {
        var control = new TestItemsControl();
        control.ItemsSource = new[] { "A" };

        Assert.Throws<InvalidOperationException>(() => control.Items.Add("B"));
        Assert.Throws<InvalidOperationException>(() => control.Items.RemoveAt(0));
        Assert.Throws<InvalidOperationException>(() => control.Items.Clear());

        control.ItemsSource = null;
        control.Items.Add("B"); // Should work now
        Assert.Single(control.Items);
    }

    [Fact]
    public void GetItemText_Uses_DisplayMemberPath()
    {
        var control = new TestItemsControl();
        var item = new { Name = "TestItem", Value = 123 };
        control.DisplayMemberPath = "Name";

        var text = control.GetItemText(item);
        Assert.Equal("TestItem", text);

        control.DisplayMemberPath = "Value";
        text = control.GetItemText(item);
        Assert.Equal("123", text);

        control.DisplayMemberPath = "NonExistent";
        text = control.GetItemText(item);
        Assert.Equal(item.ToString(), text);
    }
}
