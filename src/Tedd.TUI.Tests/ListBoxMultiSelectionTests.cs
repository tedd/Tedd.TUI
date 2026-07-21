using System;
using System.Linq;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

/// <summary>
/// The mouse and keyboard gestures a ListBox exposes for multi-selection, driven through
/// the real input pipeline.
/// </summary>
public class ListBoxMultiSelectionTests
{
    private static (ControlTestHost Host, ListBox List) MakeHost(
        SelectionMode mode = SelectionMode.Extended, int count = 6)
    {
        var list = new ListBox { Width = 8, Height = count, SelectionMode = mode };
        for (int i = 0; i < count; i++) list.Items.Add($"Item{i}");
        var host = new ControlTestHost(list, 10, count + 2);
        return (host, list);
    }

    [Fact]
    public void Click_SelectsOneRow()
    {
        var (host, list) = MakeHost();

        host.Click(list, 1, 2);

        Assert.Equal(new[] { 2 }, list.SelectedIndices);
        Assert.Equal("Item2", list.SelectedItem);
    }

    [Fact]
    public void ShiftClick_SelectsTheRangeFromTheLastClickedRow()
    {
        var (host, list) = MakeHost();

        host.Click(list, 1, 1);
        host.Click(list, 1, 4, ConsoleModifiers.Shift);

        Assert.Equal(new[] { 1, 2, 3, 4 }, list.SelectedIndices);
        Assert.Equal(
            new object?[] { "Item1", "Item2", "Item3", "Item4" },
            list.SelectedItems.Cast<object?>());
    }

    [Fact]
    public void ControlClick_AddsAndRemovesIndividualRows()
    {
        var (host, list) = MakeHost();

        host.Click(list, 1, 0);
        host.Click(list, 1, 3, ConsoleModifiers.Control);
        Assert.Equal(new[] { 0, 3 }, list.SelectedIndices);

        host.Click(list, 1, 3, ConsoleModifiers.Control);
        Assert.Equal(new[] { 0 }, list.SelectedIndices);
    }

    [Fact]
    public void PlainClickAfterMultiSelect_CollapsesBackToOneRow()
    {
        var (host, list) = MakeHost();

        host.Click(list, 1, 1);
        host.Click(list, 1, 4, ConsoleModifiers.Shift);
        host.Click(list, 1, 2);

        Assert.Equal(new[] { 2 }, list.SelectedIndices);
    }

    [Fact]
    public void ModifiedClicks_AreIgnoredInSingleMode()
    {
        var (host, list) = MakeHost(SelectionMode.Single);

        host.Click(list, 1, 1);
        host.Click(list, 1, 4, ConsoleModifiers.Shift);

        Assert.Equal(new[] { 4 }, list.SelectedIndices);
    }

    [Fact]
    public void ShiftArrow_ExtendsTheSelection()
    {
        var (host, list) = MakeHost();
        list.Focus();

        host.Click(list, 1, 1);
        host.KeyDown(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Shift);
        host.KeyDown(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Shift);

        Assert.Equal(new[] { 1, 2, 3 }, list.SelectedIndices);
        Assert.Equal(3, list.SelectedIndex);
    }

    [Fact]
    public void ShiftArrow_ShrinksTheRangeWhenReversing()
    {
        var (host, list) = MakeHost();
        list.Focus();

        host.Click(list, 1, 1);
        host.KeyDown(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Shift);
        host.KeyDown(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Shift);
        host.KeyDown(ConsoleKey.UpArrow, modifiers: ConsoleModifiers.Shift);

        Assert.Equal(new[] { 1, 2 }, list.SelectedIndices);
    }

    [Fact]
    public void UnmodifiedArrow_MovesAndReplacesTheSelection()
    {
        var (host, list) = MakeHost();
        list.Focus();

        host.Click(list, 1, 1);
        host.KeyDown(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Shift);
        host.KeyDown(ConsoleKey.DownArrow);

        Assert.Equal(new[] { 3 }, list.SelectedIndices);
    }

    [Fact]
    public void ControlA_SelectsEverything()
    {
        var (host, list) = MakeHost();
        list.Focus();

        host.KeyDown(ConsoleKey.A, modifiers: ConsoleModifiers.Control);

        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, list.SelectedIndices);
    }

    [Fact]
    public void ControlA_DoesNothingInSingleMode()
    {
        var (host, list) = MakeHost(SelectionMode.Single);
        list.Focus();

        host.Click(list, 1, 2);
        host.KeyDown(ConsoleKey.A, modifiers: ConsoleModifiers.Control);

        Assert.Equal(new[] { 2 }, list.SelectedIndices);
    }

    [Fact]
    public void Space_TogglesTheCurrentRowInMultiSelectModes()
    {
        var (host, list) = MakeHost();
        list.Focus();

        host.Click(list, 1, 2);
        host.KeyDown(ConsoleKey.Spacebar);
        Assert.Empty(list.SelectedIndices);

        host.KeyDown(ConsoleKey.Spacebar);
        Assert.Equal(new[] { 2 }, list.SelectedIndices);
    }

    [Fact]
    public void Space_StillRaisesSelectionChangedInSingleMode()
    {
        var (host, list) = MakeHost(SelectionMode.Single);
        list.Focus();
        host.Click(list, 1, 2);

        int raised = 0;
        list.SelectionChanged += (s, e) => raised++;
        host.KeyDown(ConsoleKey.Spacebar);

        Assert.Equal(1, raised);
        Assert.Equal(new[] { 2 }, list.SelectedIndices);
    }

    [Fact]
    public void EverySelectedRowIsHighlighted()
    {
        var (host, list) = MakeHost();
        list.Focus();

        host.Click(list, 1, 1);
        host.Click(list, 1, 3, ConsoleModifiers.Shift);

        var buffer = host.Render();
        var selectedBackground = list.FocusedSelectionBackground;

        for (int row = 0; row < 6; row++)
        {
            bool highlighted = buffer.GetPixel(0, row).Background == selectedBackground;
            Assert.Equal(row is >= 1 and <= 3, highlighted);
        }
    }
}
