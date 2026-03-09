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

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register("IsFocused", typeof(bool), typeof(TestControl), false);

        public bool IsFocused
        {
            get => (bool)GetValue(IsFocusedProperty);
            set => SetValue(IsFocusedProperty, value);
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
    public void Trigger_OverlappingTriggers_LaterDeclaredWins_WhenFirstActivatedFirst()
    {
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        // Trigger A declared first (lower precedence) sets TestValue=1 when IsHovered=true
        var triggerA = new Trigger { Property = TestControl.IsHoveredProperty, Value = true };
        triggerA.Setters.Add(new Setter(TestControl.TestValueProperty, 1));

        // Trigger B declared last (higher precedence) sets TestValue=2 when IsFocused=true
        var triggerB = new Trigger { Property = TestControl.IsFocusedProperty, Value = true };
        triggerB.Setters.Add(new Setter(TestControl.TestValueProperty, 2));

        template.Triggers.Add(triggerA);
        template.Triggers.Add(triggerB);
        control.Template = template;

        Assert.Equal(0, control.TestValue);

        // Activate A only
        control.IsHovered = true;
        Assert.Equal(1, control.TestValue);

        // Activate B while A is active — B is declared last so its value wins
        control.IsFocused = true;
        Assert.Equal(2, control.TestValue);

        // Deactivate A; B remains active — value must not revert to stale A value
        control.IsHovered = false;
        Assert.Equal(2, control.TestValue);

        // Deactivate B; original value restored
        control.IsFocused = false;
        Assert.Equal(0, control.TestValue);
    }

    [Fact]
    public void Trigger_OverlappingTriggers_LaterDeclaredWins_WhenSecondActivatedFirst()
    {
        var control = new TestControl();
        var template = new ControlTemplate((c) => new Border());

        // Trigger A declared first (lower precedence) sets TestValue=1 when IsHovered=true
        var triggerA = new Trigger { Property = TestControl.IsHoveredProperty, Value = true };
        triggerA.Setters.Add(new Setter(TestControl.TestValueProperty, 1));

        // Trigger B declared last (higher precedence) sets TestValue=2 when IsFocused=true
        var triggerB = new Trigger { Property = TestControl.IsFocusedProperty, Value = true };
        triggerB.Setters.Add(new Setter(TestControl.TestValueProperty, 2));

        template.Triggers.Add(triggerA);
        template.Triggers.Add(triggerB);
        control.Template = template;

        Assert.Equal(0, control.TestValue);

        // Activate B first
        control.IsFocused = true;
        Assert.Equal(2, control.TestValue);

        // Activate A while B is active — B is still declared last, so value must remain 2
        control.IsHovered = true;
        Assert.Equal(2, control.TestValue);

        // Deactivate B; A is still active — value must reflect A's value
        control.IsFocused = false;
        Assert.Equal(1, control.TestValue);

        // Deactivate A; original value restored
        control.IsHovered = false;
        Assert.Equal(0, control.TestValue);
    }
}
