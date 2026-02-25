using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xunit;

namespace Tedd.TUI.Tests;

public class SelectorTests
{
    private class TestSelector : Selector
    {
        // Concrete implementation for testing abstract Selector
    }

    [Fact]
    public void Default_Selection_Is_Empty()
    {
        var selector = new TestSelector();
        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void SelectedIndex_Updates_SelectedItem()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.Items.Add("B");

        selector.SelectedIndex = 1;

        Assert.Equal(1, selector.SelectedIndex);
        Assert.Equal("B", selector.SelectedItem);
    }

    [Fact]
    public void SelectedItem_Updates_SelectedIndex()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.Items.Add("B");

        selector.SelectedItem = "A";

        Assert.Equal(0, selector.SelectedIndex);
        Assert.Equal("A", selector.SelectedItem);
    }

    [Fact]
    public void Invalid_SelectedIndex_Is_Ignored_If_Already_Valid()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.SelectedIndex = 0;

        // Try setting invalid index
        selector.SelectedIndex = 5;

        Assert.Equal(0, selector.SelectedIndex);
        Assert.Equal("A", selector.SelectedItem);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(100)]
    public void Invalid_SelectedIndex_Is_Ignored(int index)
    {
        var selector = new TestSelector();
        selector.Items.Add("A");

        selector.SelectedIndex = index;

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void Setting_SelectedIndex_To_MinusOne_Clears_Selection()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.SelectedIndex = 0;

        selector.SelectedIndex = -1;

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void Setting_SelectedItem_To_Null_Clears_Selection()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.SelectedIndex = 0;

        selector.SelectedItem = null;

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void Setting_SelectedItem_To_Unknown_Item_Clears_Selection()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.SelectedIndex = 0;

        selector.SelectedItem = "B"; // Not in list

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void SelectionChanged_Event_Fires_On_Index_Change()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        bool fired = false;
        selector.SelectionChanged += (s, e) => fired = true;

        selector.SelectedIndex = 0;

        Assert.True(fired);
    }

    [Fact]
    public void SelectionChanged_Event_Fires_On_Item_Change()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        bool fired = false;
        selector.SelectionChanged += (s, e) => fired = true;

        selector.SelectedItem = "A";

        Assert.True(fired);
    }

    [Fact]
    public void SelectionChanged_Does_Not_Fire_If_No_Change()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.SelectedIndex = 0;

        bool fired = false;
        selector.SelectionChanged += (s, e) => fired = true;

        selector.SelectedIndex = 0; // Same value
        Assert.False(fired);

        selector.SelectedItem = "A"; // Same value
        Assert.False(fired);
    }

    [Fact]
    public void Removing_SelectedItem_Clears_Selection()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.Items.Add("B");
        selector.SelectedIndex = 0; // Select "A"

        bool fired = false;
        selector.SelectionChanged += (s, e) => fired = true;

        selector.Items.Remove("A");

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
        Assert.True(fired);
    }

    [Fact]
    public void Removing_NonSelected_Item_Updates_Index()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.Items.Add("B");
        selector.Items.Add("C");
        selector.SelectedItem = "B"; // Index 1

        bool fired = false; // Should NOT fire because SelectedItem is same
        selector.SelectionChanged += (s, e) => fired = true;

        selector.Items.Remove("A"); // B becomes index 0

        Assert.Equal(0, selector.SelectedIndex);
        Assert.Equal("B", selector.SelectedItem);

        // Wait, does Selector fire SelectionChanged if ONLY index changes but Item is same?
        // Selector.cs: OnSelectionChanged is called?
        // OnItemsCollectionChanged:
        // if (_selectedItem != null) { index = IndexOf(_selectedItem); ... if (index >= 0) _selectedIndex = index; ... }
        // It does NOT call SelectionChanged if item is found, only updates index.
        Assert.False(fired);
    }

    [Fact]
    public void Inserting_Before_Selection_Updates_Index()
    {
        var selector = new TestSelector();
        selector.Items.Add("B");
        selector.SelectedIndex = 0; // "B"

        selector.Items.Insert(0, "A"); // "B" becomes index 1

        Assert.Equal(1, selector.SelectedIndex);
        Assert.Equal("B", selector.SelectedItem);
    }

    [Fact]
    public void Clear_Items_Clears_Selection()
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.SelectedIndex = 0;

        bool fired = false;
        selector.SelectionChanged += (s, e) => fired = true;

        selector.Items.Clear();

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
        Assert.True(fired);
    }

    [Fact]
    public void Replacing_SelectedItem_Clears_Selection()
    {
        var selector = new TestSelector();
        var list = new ObservableCollection<string> { "A", "B" };
        selector.ItemsSource = list;
        selector.SelectedItem = "A";

        // Replace "A" with "C"
        list[0] = "C";

        Assert.Equal(-1, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void Replacing_Other_Item_Preserves_Selection()
    {
        var selector = new TestSelector();
        var list = new ObservableCollection<string> { "A", "B" };
        selector.ItemsSource = list;
        selector.SelectedItem = "B";

        // Replace "A" with "C"
        list[0] = "C";

        Assert.Equal(1, selector.SelectedIndex);
        Assert.Equal("B", selector.SelectedItem);
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(2, "C")]
    public void Parameterized_Selection_Works(int index, string expectedItem)
    {
        var selector = new TestSelector();
        selector.Items.Add("A");
        selector.Items.Add("B");
        selector.Items.Add("C");

        selector.SelectedIndex = index;

        Assert.Equal(expectedItem, selector.SelectedItem);
    }

    [Fact]
    public void Can_Select_Null_Item()
    {
        var selector = new TestSelector();
        selector.Items.Add(null);

        selector.SelectedIndex = 0;

        Assert.Equal(0, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
    }

    [Fact]
    public void Removing_Selected_Null_Item_Clears_Selection()
    {
        var selector = new TestSelector();
        selector.Items.Add(null);
        selector.SelectedIndex = 0;

        // At this point _selectedItem is null, _selectedIndex is 0.
        // We need to verify OnItemsCollectionChanged handles this via the else if (_selectedIndex >= 0) path.

        selector.Items.Clear(); // or Remove(null)

        Assert.Equal(-1, selector.SelectedIndex);
    }

    [Fact]
    public void Adding_Item_When_Null_Is_Selected_Resyncs()
    {
        var selector = new TestSelector();
        selector.Items.Add(null);
        selector.SelectedIndex = 0;

        // _selectedItem is null, _selectedIndex is 0.

        bool fired = false;
        selector.SelectionChanged += (s, e) => fired = true;

        selector.Items.Add("A");

        // Should hit else if (_selectedIndex >= 0) -> if (0 < 2) -> _selectedItem = Items[0] (still null)
        // SelectionChanged fires?
        // Logic says: SelectionChanged?.Invoke

        Assert.Equal(0, selector.SelectedIndex);
        Assert.Null(selector.SelectedItem);
        Assert.True(fired); // Verification that the block was entered
    }
}
