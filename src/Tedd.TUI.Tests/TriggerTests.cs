using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TriggerTests
{
    private class TestControl : Control
    {
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register("IsHovered", typeof(bool), typeof(TestControl), false);

        public bool IsHovered
        {
            get => (bool)GetValue(IsHoveredProperty);
            set => SetValue(IsHoveredProperty, value);
        }

        public static readonly DependencyProperty TestValueProperty =
            DependencyProperty.Register("TestValue", typeof(int), typeof(TestControl), 0);

        public int TestValue
        {
            get => (int)GetValue(TestValueProperty);
            set => SetValue(TestValueProperty, value);
        }
    }

    [Fact]
    public void Trigger_AppliesSetter_WhenConditionMet()
    {
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        var trigger = new Trigger
        {
            Property = TestControl.IsHoveredProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(TestControl.TestValueProperty, 42));

        template.Triggers.Add(trigger);
        control.Template = template;

        Assert.Equal(0, control.TestValue);

        control.IsHovered = true;

        Assert.Equal(42, control.TestValue);
    }

    [Fact]
    public void Trigger_RevertsSetter_WhenConditionNoLongerMet()
    {
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        var trigger = new Trigger
        {
            Property = TestControl.IsHoveredProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(TestControl.TestValueProperty, 42));

        template.Triggers.Add(trigger);
        control.Template = template;

        // Condition met
        control.IsHovered = true;
        Assert.Equal(42, control.TestValue);

        // Condition no longer met
        control.IsHovered = false;
        Assert.Equal(0, control.TestValue);
    }

    [Fact]
    public void Trigger_RevertsToLocalValue_WhenConditionNoLongerMet()
    {
        var control = new TestControl();
        control.TestValue = 100; // Local value

        var template = new ControlTemplate((c) => new Border());

        var trigger = new Trigger
        {
            Property = TestControl.IsHoveredProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(TestControl.TestValueProperty, 42));

        template.Triggers.Add(trigger);
        control.Template = template;

        Assert.Equal(100, control.TestValue);

        // Condition met
        control.IsHovered = true;
        Assert.Equal(42, control.TestValue);

        // Condition no longer met, should revert to local value
        control.IsHovered = false;
        Assert.Equal(100, control.TestValue);
    }

    [Fact]
    public void Trigger_RespectsManualLocalOverride_WhileActive()
    {
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        var trigger = new Trigger
        {
            Property = TestControl.IsHoveredProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(TestControl.TestValueProperty, 42));

        template.Triggers.Add(trigger);
        control.Template = template;

        control.IsHovered = true;
        Assert.Equal(42, control.TestValue);

        // User explicitly overrides local value while trigger is active
        control.TestValue = 999;

        control.IsHovered = false;

        // Value should remain 999, not revert to 0
        Assert.Equal(999, control.TestValue);
    }

    [Fact]
    public void Trigger_ResolvesTargetName()
    {
        var control = new TestControl();
        var border = new Control { Name = "MyBorder" };
        var template = new ControlTemplate((c) => border);

        var trigger = new Trigger
        {
            Property = TestControl.IsHoveredProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(Control.BorderBrushProperty, ConsoleColor.Red) { TargetName = "MyBorder" });

        template.Triggers.Add(trigger);
        control.Template = template;

        // Apply template loads the border
        Assert.Equal(ConsoleColor.Gray, border.BorderBrush);

        control.IsHovered = true;
        Assert.Equal(ConsoleColor.Red, border.BorderBrush);

        control.IsHovered = false;
        Assert.Equal(ConsoleColor.Gray, border.BorderBrush);
    }

    [Fact]
    public void Trigger_RevertsValues_WhenTemplateSwapped()
    {
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        var trigger = new Trigger
        {
            Property = TestControl.IsHoveredProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(TestControl.TestValueProperty, 42));

        template.Triggers.Add(trigger);
        control.Template = template;

        control.IsHovered = true;
        Assert.Equal(42, control.TestValue);

        // Swap template to null or empty
        control.Template = null!;
        Assert.Equal(0, control.TestValue); // state leak prevented
    }

    [Fact]
    public void MultipleTriggers_SameProperty_LastActiveWins_ThenReverts()
    {
        // Two triggers watch the same condition and target the same property.
        // The last trigger in collection order wins when both are active.
        // When both deactivate, the property reverts to its original value.
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        var triggerA = new Trigger { Property = TestControl.IsHoveredProperty, Value = true };
        triggerA.Setters.Add(new Setter(TestControl.TestValueProperty, 10));

        var triggerB = new Trigger { Property = TestControl.IsHoveredProperty, Value = true };
        triggerB.Setters.Add(new Setter(TestControl.TestValueProperty, 99));

        template.Triggers.Add(triggerA);
        template.Triggers.Add(triggerB);
        control.Template = template;

        // Both active: last trigger (B) wins
        control.IsHovered = true;
        Assert.Equal(99, control.TestValue);

        // Both deactivate: property reverts to original default
        control.IsHovered = false;
        Assert.Equal(0, control.TestValue);
    }

    [Fact]
    public void MultipleTriggers_DifferentConditions_SameProperty_ReAssertsWhenCompetingTriggerDeactivates()
    {
        // Trigger A is active the whole time.
        // Trigger B activates and wins (last in list), then deactivates.
        // After B deactivates, A must re-assert its value instead of leaving the
        // property stuck at B's now-inactive value.
        var control = new TestControlWithTwoFlags();
        var template = new ControlTemplate((c) => new Border());

        var triggerA = new Trigger { Property = TestControlWithTwoFlags.Flag1Property, Value = true };
        triggerA.Setters.Add(new Setter(TestControl.TestValueProperty, 10));

        var triggerB = new Trigger { Property = TestControlWithTwoFlags.Flag2Property, Value = true };
        triggerB.Setters.Add(new Setter(TestControl.TestValueProperty, 99));

        template.Triggers.Add(triggerA);
        template.Triggers.Add(triggerB);
        control.Template = template;

        // Activate A → TestValue = 10
        control.Flag1 = true;
        Assert.Equal(10, control.TestValue);

        // Activate B → TestValue = 99 (B is last in collection, wins)
        control.Flag2 = true;
        Assert.Equal(99, control.TestValue);

        // Deactivate B → A is still active and must re-assert its value
        control.Flag2 = false;
        Assert.Equal(10, control.TestValue);

        // Deactivate A → property reverts to original default (0)
        control.Flag1 = false;
        Assert.Equal(0, control.TestValue);
    }

    private class TestControlWithTwoFlags : TestControl
    {
        public static readonly DependencyProperty Flag1Property =
            DependencyProperty.Register("Flag1", typeof(bool), typeof(TestControlWithTwoFlags), false);

        public bool Flag1
        {
            get => (bool)GetValue(Flag1Property);
            set => SetValue(Flag1Property, value);
        }

        public static readonly DependencyProperty Flag2Property =
            DependencyProperty.Register("Flag2", typeof(bool), typeof(TestControlWithTwoFlags), false);

        public bool Flag2
        {
            get => (bool)GetValue(Flag2Property);
            set => SetValue(Flag2Property, value);
        }
    }
}
