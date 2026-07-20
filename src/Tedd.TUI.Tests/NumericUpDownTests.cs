using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class NumericUpDownTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var nud = new NumericUpDown();
        Assert.Equal(0, nud.Value);
        Assert.Equal(0, nud.Minimum);
        Assert.Equal(100, nud.Maximum);
        Assert.Equal(1, nud.Increment);
        Assert.True(nud.Focusable);
    }

    [Fact]
    public void Measure_SizesValueFieldFromRange()
    {
        var nud = new NumericUpDown();
        nud.Measure(new Size(100, 100));
        // [-] + space + "100" field + space + [+]
        Assert.Equal(11, nud.DesiredSize.Width);
        Assert.Equal(1, nud.DesiredSize.Height);

        var wide = new NumericUpDown { Minimum = -500, Maximum = 5000 };
        wide.Measure(new Size(100, 100));
        // field width 4 ("5000" / "-500")
        Assert.Equal(12, wide.DesiredSize.Width);
    }

    [Fact]
    public void Render_ValueRightAlignedBetweenButtons()
    {
        var nud = new NumericUpDown { Value = 42 };
        var host = new ControlTestHost(nud, 11, 1);

        VirtualBufferAssertions.EqualText("[-]  42 [+]", host.Render());
    }

    [Fact]
    public void MouseClick_MinusAndPlusButtons_ChangeValue()
    {
        var nud = new NumericUpDown { Value = 5 };
        var host = new ControlTestHost(nud, 11, 1);

        // Before
        Assert.Equal(5, nud.Value);
        VirtualBufferAssertions.EqualText("[-]   5 [+]", host.Render());

        var (down, _) = host.Click(nud, 1, 0); // [-]

        Assert.True(down.Handled);
        Assert.True(nud.IsFocused);
        Assert.Equal(4, nud.Value);
        VirtualBufferAssertions.EqualText("[-]   4 [+]", host.Render());

        host.Click(nud, 9, 0); // [+]
        host.Click(nud, 9, 0);

        Assert.Equal(6, nud.Value);
        VirtualBufferAssertions.EqualText("[-]   6 [+]", host.Render());
    }

    [Fact]
    public void MouseClick_ValueArea_FocusesWithoutChangingValue()
    {
        var nud = new NumericUpDown { Value = 5 };
        var host = new ControlTestHost(nud, 11, 1);

        host.Click(nud, 5, 0);

        Assert.True(nud.IsFocused);
        Assert.Equal(5, nud.Value);
    }

    [Fact]
    public void KeyUpDown_ChangesValue()
    {
        var nud = new NumericUpDown { Value = 10 };
        var host = new ControlTestHost(nud, 11, 1);
        nud.Focus();

        // Before
        Assert.Equal(10, nud.Value);

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(11, nud.Value);

        host.PressKey(ConsoleKey.DownArrow);
        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(9, nud.Value);
    }

    [Fact]
    public void PlusMinusKeys_ChangeValue()
    {
        var nud = new NumericUpDown { Value = 10 };
        var host = new ControlTestHost(nud, 11, 1);
        nud.Focus();

        host.KeyDown(ConsoleKey.OemPlus, '+');
        Assert.Equal(11, nud.Value);

        host.KeyDown(ConsoleKey.OemMinus, '-');
        Assert.Equal(10, nud.Value);
    }

    [Fact]
    public void Increment_AppliedPerSpin()
    {
        var nud = new NumericUpDown { Value = 10, Increment = 5 };
        var host = new ControlTestHost(nud, 11, 1);
        nud.Focus();

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(15, nud.Value);

        host.Click(nud, 1, 0); // [-]
        Assert.Equal(10, nud.Value);
    }

    [Fact]
    public void Value_ClampsToMinimumAndMaximum()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 3, Value = 3 };
        var host = new ControlTestHost(nud, 9, 1);
        nud.Focus();

        host.PressKey(ConsoleKey.UpArrow);
        Assert.Equal(3, nud.Value);

        nud.Value = 0;
        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal(0, nud.Value);

        nud.Value = 99;
        Assert.Equal(3, nud.Value);
    }

    [Fact]
    public void MinimumMaximumChange_ReclampsValue()
    {
        var nud = new NumericUpDown { Maximum = 100, Value = 50 };

        nud.Maximum = 30;
        Assert.Equal(30, nud.Value);

        nud.Value = 5;
        nud.Minimum = 10;
        Assert.Equal(10, nud.Value);
    }

    [Fact]
    public void ValueChanged_RaisedOncePerChange_NotWhenClampedNoOp()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 2, Value = 1 };
        var host = new ControlTestHost(nud, 9, 1);
        nud.Focus();
        int changes = 0;
        nud.ValueChanged += (_, _) => changes++;

        host.PressKey(ConsoleKey.UpArrow); // 1 -> 2
        Assert.Equal(1, changes);

        host.PressKey(ConsoleKey.UpArrow); // clamped at 2, no change
        Assert.Equal(1, changes);

        host.Click(nud, 1, 0); // [-]: 2 -> 1
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Disabled_IgnoresMouseAndKeyboard()
    {
        var nud = new NumericUpDown { Value = 5, IsEnabled = false };
        var host = new ControlTestHost(nud, 11, 1);

        host.Click(nud, 1, 0);
        Assert.Equal(5, nud.Value);
        Assert.False(nud.IsFocused);

        nud.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow });
        Assert.Equal(5, nud.Value);
    }

    [Fact]
    public void Click_NestedNumericUpDowns_ChangesOnlyTargetValue()
    {
        var first = new NumericUpDown { Value = 5 };
        var second = new NumericUpDown { Value = 10 };
        var controls = new StackPanel { Orientation = Orientation.Horizontal };
        controls.AddChild(first);
        controls.AddChild(new TextBlock { Text = "  " });
        controls.AddChild(second);
        var surface = new Border { Content = controls, Padding = new Thickness(0) };
        var host = new ControlTestHost(surface, 26, 3);

        host.Click(first, 1, 0);
        Assert.Equal(4, first.Value);
        Assert.Equal(10, second.Value);
        Assert.True(first.IsFocused);
        Assert.False(second.IsFocused);

        host.Click(second, 9, 0);
        Assert.Equal(4, first.Value);
        Assert.Equal(11, second.Value);
        Assert.False(first.IsFocused);
        Assert.True(second.IsFocused);
    }

}
