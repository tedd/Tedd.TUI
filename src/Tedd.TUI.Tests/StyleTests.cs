using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class StyleTests
{
    private class TestControl : Control
    {
    }

    [Fact]
    public void Style_Setter_AppliesValue()
    {
        var control = new TestControl();
        var style = new Style();
        style.Setters.Add(new Setter(UIElement.ForegroundProperty, TuiColor.Red));

        control.Style = style;

        Assert.Equal(TuiColor.Red, control.Foreground);
    }

    [Fact]
    public void LocalValue_Overrides_StyleSetter()
    {
        var control = new TestControl();
        control.Foreground = TuiColor.Blue; // Local value

        var style = new Style();
        style.Setters.Add(new Setter(UIElement.ForegroundProperty, TuiColor.Red));

        control.Style = style;

        // Local value should take precedence
        Assert.Equal(TuiColor.Blue, control.Foreground);
    }

    [Fact]
    public void ClearingLocalValue_Restores_StyleSetter()
    {
        var control = new TestControl();
        control.Foreground = TuiColor.Blue; // Local value

        var style = new Style();
        style.Setters.Add(new Setter(UIElement.ForegroundProperty, TuiColor.Red));

        control.Style = style;

        // Local value should take precedence
        Assert.Equal(TuiColor.Blue, control.Foreground);

        // Clear local value
        control.ClearValue(UIElement.ForegroundProperty);

        // Style value should take over
        Assert.Equal(TuiColor.Red, control.Foreground);
    }

    [Fact]
    public void Style_Trigger_Overrides_StyleSetter()
    {
        var control = new TestControl();

        var style = new Style();
        style.Setters.Add(new Setter(UIElement.ForegroundProperty, TuiColor.Red));

        var trigger = new Trigger
        {
            Property = UIElement.IsFocusedProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(UIElement.ForegroundProperty, TuiColor.Yellow));
        style.Triggers.Add(trigger);

        control.Style = style;

        // Initial state, no trigger
        Assert.Equal(TuiColor.Red, control.Foreground);

        // Activate trigger
        control.IsFocused = true;

        // Trigger should take precedence over style setter
        Assert.Equal(TuiColor.Yellow, control.Foreground);

        // Deactivate trigger
        control.IsFocused = false;

        // Should restore to style setter
        Assert.Equal(TuiColor.Red, control.Foreground);
    }

    [Fact]
    public void LocalValue_Overrides_StyleTrigger()
    {
        var control = new TestControl();

        var style = new Style();
        var trigger = new Trigger
        {
            Property = UIElement.IsFocusedProperty,
            Value = true
        };
        trigger.Setters.Add(new Setter(UIElement.ForegroundProperty, TuiColor.Yellow));
        style.Triggers.Add(trigger);

        control.Style = style;

        // Activate trigger
        control.IsFocused = true;

        Assert.Equal(TuiColor.Yellow, control.Foreground);

        // Set local value while trigger is active
        control.Foreground = TuiColor.Blue;

        // Local value overrides trigger
        Assert.Equal(TuiColor.Blue, control.Foreground);

        // Deactivate trigger
        control.IsFocused = false;

        // Local value persists
        Assert.Equal(TuiColor.Blue, control.Foreground);
    }
}
