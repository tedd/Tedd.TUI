using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

/// <summary>
/// The selection state machine shared by every list-like control, exercised through
/// ListBox (the simplest concrete Selector).
/// </summary>
public class SelectorMultiSelectionTests
{
    private static ListBox MakeList(SelectionMode mode = SelectionMode.Extended, int count = 6)
    {
        var list = new ListBox { SelectionMode = mode };
        for (int i = 0; i < count; i++) list.Items.Add($"Item{i}");
        return list;
    }

    [Fact]
    public void DefaultMode_IsSingle_AndSelectedItemsMirrorsSelectedItem()
    {
        var list = MakeList(SelectionMode.Single);

        Assert.Equal(SelectionMode.Single, list.SelectionMode);
        Assert.Empty(list.SelectedItems);

        list.SelectedIndex = 2;

        Assert.Equal(new[] { 2 }, list.SelectedIndices);
        Assert.Equal(new object?[] { "Item2" }, list.SelectedItems.Cast<object?>());
    }

    [Fact]
    public void SelectSingle_ReplacesAnyExistingSelection()
    {
        var list = MakeList();

        list.SelectSingle(1);
        list.ToggleSelection(3);
        Assert.Equal(new[] { 1, 3 }, list.SelectedIndices);

        list.SelectSingle(4);

        Assert.Equal(new[] { 4 }, list.SelectedIndices);
        Assert.Equal(4, list.SelectedIndex);
        Assert.Equal("Item4", list.SelectedItem);
    }

    [Fact]
    public void ToggleSelection_AddsThenRemoves_WithoutDisturbingTheRest()
    {
        var list = MakeList();

        list.SelectSingle(0);
        list.ToggleSelection(2);
        list.ToggleSelection(4);
        Assert.Equal(new[] { 0, 2, 4 }, list.SelectedIndices);

        list.ToggleSelection(2);

        Assert.Equal(new[] { 0, 4 }, list.SelectedIndices);
    }

    [Fact]
    public void ToggleSelection_RemovingThePrimary_MovesItToTheFirstRemaining()
    {
        var list = MakeList();

        list.SelectSingle(1);
        list.ToggleSelection(3);
        Assert.Equal(3, list.SelectedIndex);

        list.ToggleSelection(3);

        Assert.Equal(new[] { 1 }, list.SelectedIndices);
        Assert.Equal(1, list.SelectedIndex);
    }

    [Fact]
    public void ToggleSelection_ClearingTheLastItem_LeavesNoSelection()
    {
        var list = MakeList();

        list.SelectSingle(2);
        list.ToggleSelection(2);

        Assert.Empty(list.SelectedIndices);
        Assert.Equal(-1, list.SelectedIndex);
        Assert.Null(list.SelectedItem);
    }

    [Fact]
    public void ExtendSelectionTo_SelectsTheInclusiveRangeFromTheAnchor()
    {
        var list = MakeList();

        list.SelectSingle(1);
        list.ExtendSelectionTo(4);

        Assert.Equal(new[] { 1, 2, 3, 4 }, list.SelectedIndices);
        Assert.Equal(4, list.SelectedIndex);
    }

    [Fact]
    public void ExtendSelectionTo_WorksBackwards()
    {
        var list = MakeList();

        list.SelectSingle(4);
        list.ExtendSelectionTo(1);

        Assert.Equal(new[] { 1, 2, 3, 4 }, list.SelectedIndices);
        Assert.Equal(1, list.SelectedIndex);
    }

    [Fact]
    public void ExtendSelectionTo_KeepsTheAnchorSoTheRangeCanBeResized()
    {
        var list = MakeList();

        list.SelectSingle(2);
        list.ExtendSelectionTo(5);
        list.ExtendSelectionTo(3);

        // Re-extending re-picks from the anchor rather than walking the range along.
        Assert.Equal(new[] { 2, 3 }, list.SelectedIndices);
    }

    [Fact]
    public void ExtendSelectionTo_WithUnion_AddsTheRangeToTheExistingSelection()
    {
        var list = MakeList();

        list.SelectSingle(0);
        list.ToggleSelection(3);
        list.ExtendSelectionTo(5, union: true);

        Assert.Equal(new[] { 0, 3, 4, 5 }, list.SelectedIndices);
    }

    [Fact]
    public void ToggleAndExtend_AreSingleSelectInSingleMode()
    {
        var list = MakeList(SelectionMode.Single);

        list.SelectSingle(1);
        list.ToggleSelection(3);
        Assert.Equal(new[] { 3 }, list.SelectedIndices);

        list.ExtendSelectionTo(5);
        Assert.Equal(new[] { 5 }, list.SelectedIndices);
    }

    [Theory]
    [InlineData(ConsoleModifiers.Control)]
    [InlineData(ConsoleModifiers.Alt)]
    public void Gesture_ToggleModifier_TogglesInExtendedMode(ConsoleModifiers toggleModifier)
    {
        var list = MakeList();

        list.ApplySelectionGesture(1, 0);
        list.ApplySelectionGesture(3, toggleModifier);

        Assert.Equal(new[] { 1, 3 }, list.SelectedIndices);
    }

    [Fact]
    public void Gesture_Shift_ExtendsInExtendedMode()
    {
        var list = MakeList();

        list.ApplySelectionGesture(1, 0);
        list.ApplySelectionGesture(3, ConsoleModifiers.Shift);

        Assert.Equal(new[] { 1, 2, 3 }, list.SelectedIndices);
    }

    [Fact]
    public void Gesture_PlainClick_ReplacesInExtendedMode()
    {
        var list = MakeList();

        list.ApplySelectionGesture(1, ConsoleModifiers.Control);
        list.ApplySelectionGesture(3, ConsoleModifiers.Control);
        list.ApplySelectionGesture(5, 0);

        Assert.Equal(new[] { 5 }, list.SelectedIndices);
    }

    [Fact]
    public void Gesture_PlainClick_TogglesInMultipleMode()
    {
        var list = MakeList(SelectionMode.Multiple);

        list.ApplySelectionGesture(1, 0);
        list.ApplySelectionGesture(3, 0);
        Assert.Equal(new[] { 1, 3 }, list.SelectedIndices);

        list.ApplySelectionGesture(1, 0);
        Assert.Equal(new[] { 3 }, list.SelectedIndices);
    }

    [Fact]
    public void Gesture_Shift_ExtendsAndKeepsExistingInMultipleMode()
    {
        var list = MakeList(SelectionMode.Multiple);

        list.ApplySelectionGesture(0, 0);
        list.ApplySelectionGesture(3, 0);
        list.ApplySelectionGesture(5, ConsoleModifiers.Shift);

        Assert.Equal(new[] { 0, 3, 4, 5 }, list.SelectedIndices);
    }

    [Fact]
    public void Gesture_IgnoresModifiersInSingleMode()
    {
        var list = MakeList(SelectionMode.Single);

        list.ApplySelectionGesture(1, 0);
        list.ApplySelectionGesture(3, ConsoleModifiers.Control);
        Assert.Equal(new[] { 3 }, list.SelectedIndices);

        list.ApplySelectionGesture(5, ConsoleModifiers.Shift);
        Assert.Equal(new[] { 5 }, list.SelectedIndices);
    }

    [Fact]
    public void SelectAll_SelectsEverything_AndThrowsInSingleMode()
    {
        var list = MakeList();
        list.SelectAll();
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, list.SelectedIndices);

        list.UnselectAll();
        Assert.Empty(list.SelectedIndices);
        Assert.Equal(-1, list.SelectedIndex);

        var single = MakeList(SelectionMode.Single);
        Assert.Throws<NotSupportedException>(() => single.SelectAll());
    }

    [Fact]
    public void SettingSelectedIndex_ReplacesAMultiSelection()
    {
        var list = MakeList();

        list.SelectSingle(0);
        list.ExtendSelectionTo(3);
        Assert.Equal(4, list.SelectedIndices.Count);

        list.SelectedIndex = 5;

        Assert.Equal(new[] { 5 }, list.SelectedIndices);
        Assert.Equal(new object?[] { "Item5" }, list.SelectedItems.Cast<object?>());
    }

    [Fact]
    public void SettingSelectedItem_ReplacesAMultiSelection()
    {
        var list = MakeList();

        list.SelectAll();
        list.SelectedItem = "Item2";

        Assert.Equal(new[] { 2 }, list.SelectedIndices);
    }

    [Fact]
    public void SelectedItems_TracksTheSelectionInItemOrder()
    {
        var list = MakeList();

        list.SelectSingle(4);
        list.ToggleSelection(1);

        Assert.Equal(new object?[] { "Item1", "Item4" }, list.SelectedItems.Cast<object?>());
    }

    [Fact]
    public void SelectedItems_CanBeMutatedDirectlyToDriveTheSelection()
    {
        var list = MakeList();

        list.SelectedItems.Add("Item1");
        list.SelectedItems.Add("Item3");

        Assert.Equal(new[] { 1, 3 }, list.SelectedIndices);
        Assert.Equal(1, list.SelectedIndex);

        list.SelectedItems.Remove("Item1");
        Assert.Equal(new[] { 3 }, list.SelectedIndices);
    }

    [Fact]
    public void SelectedItems_CanBeBoundToAConsumerCollection()
    {
        var list = MakeList();
        var mine = new ObservableCollection<object?> { "Item2", "Item5" };

        list.SelectedItems = mine;

        Assert.Equal(new[] { 2, 5 }, list.SelectedIndices);

        // The control keeps writing through to the collection it was given.
        list.SelectSingle(0);
        Assert.Equal(new object?[] { "Item0" }, mine);
    }

    [Fact]
    public void SelectedItems_UnknownItemsAreIgnored()
    {
        var list = MakeList();

        list.SelectedItems.Add("not in the list");

        Assert.Empty(list.SelectedIndices);
        Assert.Equal(-1, list.SelectedIndex);
    }

    [Fact]
    public void SelectionChanged_ReportsTheAddedAndRemovedItems()
    {
        var list = MakeList();
        var events = new List<SelectionChangedEventArgs>();
        list.SelectionChanged += (s, e) => events.Add((SelectionChangedEventArgs)e);

        list.SelectSingle(1);
        list.ToggleSelection(2);
        list.SelectSingle(4);

        Assert.Equal(new object?[] { "Item1" }, events[0].AddedItems);
        Assert.Empty(events[0].RemovedItems);

        Assert.Equal(new object?[] { "Item2" }, events[1].AddedItems);
        Assert.Empty(events[1].RemovedItems);

        Assert.Equal(new object?[] { "Item4" }, events[2].AddedItems);
        Assert.Equal(new object?[] { "Item1", "Item2" }, events[2].RemovedItems);
    }

    [Fact]
    public void SelectionChanged_IsNotRaisedWhenNothingChanges()
    {
        var list = MakeList();
        list.SelectSingle(2);

        int count = 0;
        list.SelectionChanged += (s, e) => count++;
        list.SelectSingle(2);

        Assert.Equal(0, count);
    }

    [Fact]
    public void InsertingAnItem_ShiftsTheSelectionInsteadOfRepointingIt()
    {
        var list = MakeList();

        list.SelectSingle(2);
        list.ToggleSelection(4);

        list.Items.Insert(0, "New");

        Assert.Equal(new[] { 3, 5 }, list.SelectedIndices);
        Assert.Equal(new object?[] { "Item2", "Item4" }, list.SelectedItems.Cast<object?>());
    }

    [Fact]
    public void RemovingASelectedItem_DropsItFromTheSelection()
    {
        var list = MakeList();

        list.SelectSingle(1);
        list.ToggleSelection(3);

        list.Items.RemoveAt(1);

        Assert.Equal(new object?[] { "Item3" }, list.SelectedItems.Cast<object?>());
        Assert.Equal(new[] { 2 }, list.SelectedIndices);
    }

    [Fact]
    public void ClearingItems_ClearsTheSelection()
    {
        var list = MakeList();
        list.SelectAll();

        list.Items.Clear();

        Assert.Empty(list.SelectedIndices);
        Assert.Empty(list.SelectedItems);
        Assert.Equal(-1, list.SelectedIndex);
    }

    [Fact]
    public void EachInstanceGetsItsOwnSelectedItemsCollection()
    {
        var a = MakeList();
        var b = MakeList();

        a.SelectSingle(1);

        Assert.NotSame(a.SelectedItems, b.SelectedItems);
        Assert.Empty(b.SelectedItems);
    }
}
