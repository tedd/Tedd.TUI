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

        // Condition met, but LocalValue takes precedence over TriggerValue
        control.IsHovered = true;
        Assert.Equal(100, control.TestValue);

        // Condition no longer met, remains at local value
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
        // Trigger is active but LocalValue takes precedence over TriggerValue
        Assert.Equal(42, control.TestValue);

        // User explicitly overrides local value while trigger is active
        control.TestValue = 999;

        // Since LocalValue has higher precedence, it takes effect immediately
        Assert.Equal(999, control.TestValue);

        control.IsHovered = false;

        // Value should remain 999, as the LocalValue is untouched
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
    public void Trigger_RemainsActiveAfterUnwatchedPropertyChange()
    {
        // Verify that changing a property not referenced in any trigger condition
        // does not deactivate an active trigger or revert its setter values.
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

        // Change BorderBrush — not watched by any trigger, should be a no-op for trigger state
        control.BorderBrush = ConsoleColor.Cyan;

        // Trigger should still be active; TestValue must remain at 42
        Assert.Equal(42, control.TestValue);

        control.IsHovered = false;
        Assert.Equal(0, control.TestValue);
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
}
