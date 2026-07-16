using System;
using System.Diagnostics;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class RadioButtonTests
{
    [Fact]
    public void TestRadioButtonNavigation()
    {
        var window = new TuiWindow();
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        window.Content = panel;

        var rb1 = new RadioButton { Content = "Option 1", GroupName = "Group1" };
        var rb2 = new RadioButton { Content = "Option 2", GroupName = "Group1" };
        var text = new TextBlock { Text = "Separator" };
        var rb3 = new RadioButton { Content = "Option 3", GroupName = "Group1" };
        var rbOther = new RadioButton { Content = "Other Group", GroupName = "Group2" };

        panel.AddChild(rb1);
        panel.AddChild(rb2);
        panel.AddChild(text); // Should be skipped
        panel.AddChild(rb3);
        panel.AddChild(rbOther); // Should be skipped (different group)

        // Simulate focusing and selecting the first radio button
        rb1.IsChecked = true;
        rb1.Focus();

        // Simulate Down arrow key press on rb1
        var keyEvent = new KeyEventArgs { Key = ConsoleKey.DownArrow, Modifiers = ConsoleModifiers.None };
        rb1.OnKeyDown(keyEvent);

        // Expect rb2 to be checked and focused
        Assert.True(rb2.IsChecked, "rb2 should be checked after Down arrow from rb1");
        Assert.False(rb1.IsChecked, "rb1 should be unchecked");
        Assert.True(rb2.IsFocused, "rb2 should be focused");

        // Simulate Down arrow key press on rb2 (skipping text block)
        keyEvent = new KeyEventArgs { Key = ConsoleKey.DownArrow, Modifiers = ConsoleModifiers.None };
        rb2.OnKeyDown(keyEvent);

        // Expect rb3 to be checked and focused
        Assert.True(rb3.IsChecked, "rb3 should be checked after Down arrow from rb2");
        Assert.False(rb2.IsChecked, "rb2 should be unchecked");

        // Simulate Down arrow key press on rb3 (wrapping around to rb1, skipping rbOther)
        keyEvent = new KeyEventArgs { Key = ConsoleKey.DownArrow, Modifiers = ConsoleModifiers.None };
        rb3.OnKeyDown(keyEvent);

        // Expect rb1 to be checked and focused (wrapping)
        Assert.True(rb1.IsChecked, "rb1 should be checked after Down arrow from rb3 (wrap)");
        Assert.False(rb3.IsChecked, "rb3 should be unchecked");

        // Simulate Up arrow key press on rb1 (wrapping backwards to rb3)
        keyEvent = new KeyEventArgs { Key = ConsoleKey.UpArrow, Modifiers = ConsoleModifiers.None };
        rb1.OnKeyDown(keyEvent);

        // Expect rb3 to be checked and focused (wrap backward)
        Assert.True(rb3.IsChecked, "rb3 should be checked after Up arrow from rb1 (wrap backward)");
        Assert.False(rb1.IsChecked, "rb1 should be unchecked");
    }

    [Fact]
    public void BenchmarkRadioButtonNavigation()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        int rbCount = 100;
        var rbs = new RadioButton[rbCount];
        for (int i = 0; i < rbCount; i++)
        {
            var rb = new RadioButton { Content = $"Option {i}", GroupName = "Group1" };
            panel.AddChild(rb);
            rbs[i] = rb;
        }

        // Start with the first one
        rbs[0].IsChecked = true;
        rbs[0].Focus();

        var keyEvent = new KeyEventArgs { Key = ConsoleKey.DownArrow, Modifiers = ConsoleModifiers.None };
        int iterations = 10000;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            // Simulate navigation on the currently focused/checked radio button
            // In a real scenario, the focused element handles the event.
            // We need to find which one is currently checked to simulate the event on it.
            // But checking every time is slow for the benchmark setup itself.
            // Instead, we know the index will increment by 1 each time (modulo rbCount).

            var current = rbs[i % rbCount];
            // Ensure current is actually checked/focused as the loop assumes sequential navigation
            // The logic in OnKeyDown will focus/check the next one.
            // Wait, OnKeyDown handles the event on 'this' (the current focused element).
            // So we need to call OnKeyDown on the currently active radio button.

            // To make this benchmark realistic but fast enough to set up:
            // The previous iteration's OnKeyDown call should have updated the state.
            // So we just need to track which one *should* have focus.

            // However, OnKeyDown calls NavigateToSibling which changes focus.
            // So we can just loop, but we need to know which instance to call OnKeyDown on.
            // We can track the current index.

            current.OnKeyDown(keyEvent);
        }
        sw.Stop();

        // This is just to ensure it runs and to print output if running in verbose mode.
        // Asserting a specific time is flaky, but we can log it.
        Console.WriteLine($"Benchmark completed in {sw.ElapsedMilliseconds}ms for {iterations} iterations with {rbCount} items.");
        Assert.True(true);
    }

    [Fact]
    public void IsChecked_Changes_RaisesRoutedEvents()
    {
        var rb = new RadioButton();
        bool checkedRaised = false;
        bool uncheckedRaised = false;

        rb.Checked += (s, e) => checkedRaised = true;
        rb.Unchecked += (s, e) => uncheckedRaised = true;

        rb.IsChecked = true;
        Assert.True(checkedRaised);
        Assert.False(uncheckedRaised);

        checkedRaised = false; // reset
        rb.IsChecked = false;
        Assert.True(uncheckedRaised);
        Assert.False(checkedRaised);
    }

    [Fact]
    public void CheckedEvent_BubblesUpLogicalTree()
    {
        var panel = new StackPanel();
        var rb = new RadioButton { GroupName = "G1" };
        panel.AddChild(rb);

        bool panelCheckedRaised = false;
        panel.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler((s, e) => panelCheckedRaised = true));

        rb.IsChecked = true;
        Assert.True(panelCheckedRaised);
    }

    [Fact]
    public void Group_Update_UnchecksAndRaisesEvent()
    {
        var panel = new StackPanel();
        var rb1 = new RadioButton { GroupName = "G1" };
        var rb2 = new RadioButton { GroupName = "G1" };
        panel.AddChild(rb1);
        panel.AddChild(rb2);

        rb1.IsChecked = true;

        bool rb1UncheckedRaised = false;
        bool rb2CheckedRaised = false;

        rb1.Unchecked += (s, e) => rb1UncheckedRaised = true;
        rb2.Checked += (s, e) => rb2CheckedRaised = true;

        // Checking rb2 should uncheck rb1 and raise events for both
        rb2.IsChecked = true;

        Assert.True(rb2CheckedRaised);
        Assert.True(rb1UncheckedRaised);
        Assert.False(rb1.IsChecked);
        Assert.True(rb2.IsChecked);
    }

    [Fact]
    public void Click_NestedRadioButtons_SelectsOnlyTargetInGroup()
    {
        var first = new RadioButton { Content = "First", GroupName = "Choice" };
        var second = new RadioButton { Content = "Second", GroupName = "Choice" };
        var firstClicks = 0;
        var secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        var choices = new StackPanel();
        choices.AddChild(first);
        choices.AddChild(new TextBlock { Text = " spacer " });
        choices.AddChild(second);
        var surface = new Border { Content = choices };
        var host = new ControlTestHost(surface, 24, 7);

        host.Click(first, 1, 0);

        Assert.True(first.IsChecked);
        Assert.False(second.IsChecked);
        Assert.Equal(1, firstClicks);
        Assert.Equal(0, secondClicks);

        host.Click(second, 1, 0);

        Assert.False(first.IsChecked);
        Assert.True(second.IsChecked);
        Assert.Equal(1, firstClicks);
        Assert.Equal(1, secondClicks);
    }
}
