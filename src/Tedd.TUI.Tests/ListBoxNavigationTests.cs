using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

/// <summary>Home/End/PageUp/PageDown navigation, with and without Shift.</summary>
public class ListBoxNavigationTests
{
    // A viewport of 5 rows over 20 items: one page step is 4 rows (viewport minus overlap).
    private static (ControlTestHost Host, ListBox List) MakeHost(
        SelectionMode mode = SelectionMode.Extended)
    {
        var list = new ListBox { Width = 8, Height = 5, SelectionMode = mode };
        for (int i = 0; i < 20; i++) list.Items.Add($"Item{i}");
        var host = new ControlTestHost(list, 10, 5);
        list.Focus();
        return (host, list);
    }

    [Fact]
    public void Home_And_End_JumpToTheEnds()
    {
        var (host, list) = MakeHost();
        list.SelectSingle(7);

        host.KeyDown(ConsoleKey.Home);
        Assert.Equal(new[] { 0 }, list.SelectedIndices);

        host.KeyDown(ConsoleKey.End);
        Assert.Equal(new[] { 19 }, list.SelectedIndices);
    }

    [Fact]
    public void ShiftHome_And_ShiftEnd_ExtendToTheEnds()
    {
        var (host, list) = MakeHost();
        list.SelectSingle(3);

        host.KeyDown(ConsoleKey.Home, modifiers: ConsoleModifiers.Shift);
        Assert.Equal(new[] { 0, 1, 2, 3 }, list.SelectedIndices);

        host.KeyDown(ConsoleKey.End, modifiers: ConsoleModifiers.Shift);
        // The anchor is still row 3, so extending the other way re-picks from there.
        Assert.Equal(17, list.SelectedIndices.Count);
        Assert.Equal(3, list.SelectedIndices[0]);
        Assert.Equal(19, list.SelectedIndex);
    }

    [Fact]
    public void PageDown_And_PageUp_MoveByAViewport()
    {
        var (host, list) = MakeHost();
        list.SelectSingle(0);

        host.KeyDown(ConsoleKey.PageDown);
        Assert.Equal(new[] { 4 }, list.SelectedIndices);

        host.KeyDown(ConsoleKey.PageDown);
        Assert.Equal(new[] { 8 }, list.SelectedIndices);

        host.KeyDown(ConsoleKey.PageUp);
        Assert.Equal(new[] { 4 }, list.SelectedIndices);
    }

    [Fact]
    public void Paging_StopsAtTheEndsInsteadOfBeingIgnored()
    {
        var (host, list) = MakeHost();
        list.SelectSingle(18);

        host.KeyDown(ConsoleKey.PageDown);
        Assert.Equal(new[] { 19 }, list.SelectedIndices);

        list.SelectSingle(1);
        host.KeyDown(ConsoleKey.PageUp);
        Assert.Equal(new[] { 0 }, list.SelectedIndices);
    }

    [Fact]
    public void ShiftPageDown_ExtendsByAViewport()
    {
        var (host, list) = MakeHost();
        list.SelectSingle(2);

        host.KeyDown(ConsoleKey.PageDown, modifiers: ConsoleModifiers.Shift);

        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, list.SelectedIndices);
        Assert.Equal(6, list.SelectedIndex);
    }

    [Fact]
    public void Navigation_ScrollsTheTargetIntoView()
    {
        var (host, list) = MakeHost();
        list.SelectSingle(0);

        host.KeyDown(ConsoleKey.End);

        var buffer = host.Render();
        // The last row of the viewport shows the last item.
        var text = string.Concat(new[] { 0, 1, 2, 3, 4, 5, 6 }.Select(dx => buffer.GetPixel(dx, 4).Character));
        Assert.Equal("Item19 ", text);
    }

    [Fact]
    public void ShiftNavigation_IsPlainNavigationInSingleMode()
    {
        var (host, list) = MakeHost(SelectionMode.Single);
        list.SelectSingle(3);

        host.KeyDown(ConsoleKey.End, modifiers: ConsoleModifiers.Shift);

        Assert.Equal(new[] { 19 }, list.SelectedIndices);
    }
}
