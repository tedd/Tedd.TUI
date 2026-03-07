using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ToggleButtonTests
{
    // Helper to simulate a user toggle via Space key (same as ButtonTests pattern)
    private static void SimulateToggle(ToggleButton tb)
    {
        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Spacebar });
        tb.OnKeyUp(new KeyEventArgs { Key = ConsoleKey.Spacebar });
    }

    [Fact]
    public void IsChecked_DefaultValue_IsFalse()
    {
        var tb = new ToggleButton();
        Assert.Equal(false, tb.IsChecked);
    }

    [Fact]
    public void IsThreeState_DefaultValue_IsFalse()
    {
        var tb = new ToggleButton();
        Assert.False(tb.IsThreeState);
    }

    // false -> true -> null -> false cycle when IsThreeState = true
    [Fact]
    public void ThreeState_ToggleCycle_FalseTrueNullFalse()
    {
        var tb = new ToggleButton { IsThreeState = true, IsChecked = false };

        SimulateToggle(tb);
        Assert.Equal(true, tb.IsChecked);   // false -> true

        SimulateToggle(tb);
        Assert.Null(tb.IsChecked);          // true -> null

        SimulateToggle(tb);
        Assert.Equal(false, tb.IsChecked);  // null -> false
    }

    // With IsThreeState = false, true should go to false (not null)
    [Fact]
    public void NonThreeState_ToggleCycleSkipsNull_TrueFalse()
    {
        var tb = new ToggleButton { IsThreeState = false, IsChecked = false };

        SimulateToggle(tb);
        Assert.Equal(true, tb.IsChecked);   // false -> true

        SimulateToggle(tb);
        Assert.Equal(false, tb.IsChecked);  // true -> false (not null)

        // Verify that another cycle still doesn't hit null
        SimulateToggle(tb);
        Assert.Equal(true, tb.IsChecked);
        SimulateToggle(tb);
        Assert.Equal(false, tb.IsChecked);
    }

    // IsThreeState = false: setting IsChecked = null directly still fires Indeterminate
    // but toggling via user interaction never reaches null
    [Fact]
    public void NonThreeState_UserToggle_NeverProducesNull()
    {
        var tb = new ToggleButton { IsThreeState = false };

        for (int i = 0; i < 10; i++)
        {
            SimulateToggle(tb);
            Assert.NotNull(tb.IsChecked);
        }
    }

    [Fact]
    public void Indeterminate_RaisedWhenIsCheckedSetToNull()
    {
        var tb = new ToggleButton();
        int indeterminateCount = 0;
        tb.Indeterminate += (s, e) => indeterminateCount++;

        tb.IsChecked = null;
        Assert.Equal(1, indeterminateCount);
    }

    [Fact]
    public void Indeterminate_RaisedWhenTogglingToNullState()
    {
        var tb = new ToggleButton { IsThreeState = true, IsChecked = true };
        int indeterminateCount = 0;
        tb.Indeterminate += (s, e) => indeterminateCount++;

        SimulateToggle(tb); // true -> null
        Assert.Equal(1, indeterminateCount);
    }

    [Fact]
    public void Checked_RaisedWhenIsCheckedSetToTrue()
    {
        var tb = new ToggleButton { IsChecked = false };
        int checkedCount = 0;
        tb.Checked += (s, e) => checkedCount++;

        tb.IsChecked = true;
        Assert.Equal(1, checkedCount);
    }

    [Fact]
    public void Unchecked_RaisedWhenIsCheckedSetToFalse()
    {
        var tb = new ToggleButton { IsChecked = true };
        int uncheckedCount = 0;
        tb.Unchecked += (s, e) => uncheckedCount++;

        tb.IsChecked = false;
        Assert.Equal(1, uncheckedCount);
    }

    [Fact]
    public void ThreeState_AllEventsRaisedInFullCycle()
    {
        var tb = new ToggleButton { IsThreeState = true, IsChecked = false };
        int checkedCount = 0;
        int uncheckedCount = 0;
        int indeterminateCount = 0;

        tb.Checked += (s, e) => checkedCount++;
        tb.Unchecked += (s, e) => uncheckedCount++;
        tb.Indeterminate += (s, e) => indeterminateCount++;

        SimulateToggle(tb); // false -> true  => Checked
        Assert.Equal(1, checkedCount);
        Assert.Equal(0, uncheckedCount);
        Assert.Equal(0, indeterminateCount);

        SimulateToggle(tb); // true -> null   => Indeterminate
        Assert.Equal(1, checkedCount);
        Assert.Equal(0, uncheckedCount);
        Assert.Equal(1, indeterminateCount);

        SimulateToggle(tb); // null -> false  => Unchecked
        Assert.Equal(1, checkedCount);
        Assert.Equal(1, uncheckedCount);
        Assert.Equal(1, indeterminateCount);
    }

    [Fact]
    public void Indeterminate_EventBubblesUpLogicalTree()
    {
        var panel = new StackPanel();
        var tb = new ToggleButton { IsThreeState = true, IsChecked = true };
        panel.AddChild(tb);

        bool panelIndeterminateRaised = false;
        panel.AddHandler(ToggleButton.IndeterminateEvent, new RoutedEventHandler((s, e) => panelIndeterminateRaised = true));

        SimulateToggle(tb); // true -> null => Indeterminate bubbles
        Assert.True(panelIndeterminateRaised);
    }

    [Fact]
    public void Checked_EventBubblesUpLogicalTree()
    {
        var panel = new StackPanel();
        var tb = new ToggleButton { IsChecked = false };
        panel.AddChild(tb);

        bool panelCheckedRaised = false;
        panel.AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler((s, e) => panelCheckedRaised = true));

        tb.IsChecked = true;
        Assert.True(panelCheckedRaised);
    }
}
