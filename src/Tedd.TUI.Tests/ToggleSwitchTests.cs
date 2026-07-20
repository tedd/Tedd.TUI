using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class ToggleSwitchTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var ts = new ToggleSwitch();
        Assert.Equal(false, ts.IsChecked);
        Assert.True(ts.Focusable);
        Assert.Equal("On", ts.OnContent);
        Assert.Equal("Off", ts.OffContent);
        Assert.Null(ts.Content);
    }

    [Fact]
    public void Measure_TrackAndStateLabel()
    {
        var ts = new ToggleSwitch();
        ts.Measure(new Size(100, 100));
        // "[●──] Off" -> 5 + 1 + 3
        Assert.Equal(9, ts.DesiredSize.Width);
        Assert.Equal(1, ts.DesiredSize.Height);
    }

    [Fact]
    public void Measure_IncludesContent()
    {
        var ts = new ToggleSwitch { Content = "Sound" };
        ts.Measure(new Size(100, 100));
        // "[●──] Off Sound" -> 5 + 1 + 3 + 1 + 5
        Assert.Equal(15, ts.DesiredSize.Width);
    }

    [Fact]
    public void Measure_NoStateLabels_TrackOnly()
    {
        var ts = new ToggleSwitch { OnContent = null, OffContent = null };
        ts.Measure(new Size(100, 100));
        Assert.Equal(5, ts.DesiredSize.Width);
    }

    [Fact]
    public void Render_Off_KnobLeft()
    {
        var ts = new ToggleSwitch();
        var host = new ControlTestHost(ts, 9, 1);

        VirtualBufferAssertions.EqualText("[●──] Off", host.Render());
    }

    [Fact]
    public void Render_On_KnobRight()
    {
        var ts = new ToggleSwitch { IsChecked = true };
        var host = new ControlTestHost(ts, 9, 1);

        VirtualBufferAssertions.EqualText("[──●] On ", host.Render());
    }

    [Fact]
    public void Render_Indeterminate_KnobCenter()
    {
        var ts = new ToggleSwitch { IsChecked = null };
        var host = new ControlTestHost(ts, 9, 1);

        VirtualBufferAssertions.EqualText("[─●─] Off", host.Render());
    }

    [Fact]
    public void Render_OnUsesOnKnobColor()
    {
        var ts = new ToggleSwitch { IsChecked = true };
        var host = new ControlTestHost(ts, 9, 1);

        var buffer = host.Render();
        Assert.Equal(ts.OnKnobColor, buffer.GetPixel(3, 0).Foreground);
        Assert.Equal(ts.TrackColor, buffer.GetPixel(1, 0).Foreground);
        Assert.Equal(ts.BracketColor, buffer.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void MouseClick_TogglesOnThenOff()
    {
        var ts = new ToggleSwitch { Content = "Sound" };
        var host = new ControlTestHost(ts, 15, 1);

        // Before
        Assert.Equal(false, ts.IsChecked);
        VirtualBufferAssertions.EqualText("[●──] Off Sound", host.Render());

        host.Click(ts, 2, 0);

        // After first click
        Assert.Equal(true, ts.IsChecked);
        Assert.True(ts.IsFocused);
        VirtualBufferAssertions.EqualText("[──●] On  Sound", host.Render());

        host.Click(ts, 2, 0);

        // After second click
        Assert.Equal(false, ts.IsChecked);
        VirtualBufferAssertions.EqualText("[●──] Off Sound", host.Render());
    }

    [Theory]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.Enter)]
    public void KeyPress_TogglesOnThenOff(ConsoleKey key)
    {
        var ts = new ToggleSwitch();
        var host = new ControlTestHost(ts, 9, 1);
        ts.Focus();

        // Before
        Assert.Equal(false, ts.IsChecked);
        VirtualBufferAssertions.EqualText("[●──] Off", host.Render());

        host.PressKey(key);

        // After first press
        Assert.Equal(true, ts.IsChecked);
        VirtualBufferAssertions.EqualText("[──●] On ", host.Render());

        host.PressKey(key);

        // After second press
        Assert.Equal(false, ts.IsChecked);
        VirtualBufferAssertions.EqualText("[●──] Off", host.Render());
    }

    [Fact]
    public void MouseClick_RaisesCheckedAndUncheckedEvents()
    {
        var ts = new ToggleSwitch();
        var host = new ControlTestHost(ts, 9, 1);
        int checkedCount = 0;
        int uncheckedCount = 0;
        int clickCount = 0;
        ts.Checked += (_, _) => checkedCount++;
        ts.Unchecked += (_, _) => uncheckedCount++;
        ts.Click += (_, _) => clickCount++;

        host.Click(ts, 1, 0);

        Assert.Equal(1, checkedCount);
        Assert.Equal(0, uncheckedCount);
        Assert.Equal(1, clickCount);

        host.Click(ts, 1, 0);

        Assert.Equal(1, checkedCount);
        Assert.Equal(1, uncheckedCount);
        Assert.Equal(2, clickCount);
    }

    [Fact]
    public void Disabled_IgnoresMouseAndKeyboard()
    {
        var ts = new ToggleSwitch { IsEnabled = false };
        var host = new ControlTestHost(ts, 9, 1);

        host.Click(ts, 1, 0);

        Assert.Equal(false, ts.IsChecked);
        Assert.False(ts.IsFocused);

        ts.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Spacebar });
        ts.OnKeyUp(new KeyEventArgs { Key = ConsoleKey.Spacebar });

        Assert.Equal(false, ts.IsChecked);
    }

    [Fact]
    public void CustomStateContent_RenderedPerState()
    {
        var ts = new ToggleSwitch { OnContent = "Yes", OffContent = "No" };
        var host = new ControlTestHost(ts, 9, 1);

        VirtualBufferAssertions.EqualText("[●──] No ", host.Render());

        ts.IsChecked = true;
        VirtualBufferAssertions.EqualText("[──●] Yes", host.Render());
    }

    [Fact]
    public void Click_NestedToggleSwitches_TogglesOnlyTargetSwitch()
    {
        var first = new ToggleSwitch();
        var second = new ToggleSwitch();
        var switches = new StackPanel { Orientation = Orientation.Horizontal };
        switches.AddChild(first);
        switches.AddChild(new TextBlock { Text = "  " });
        switches.AddChild(second);
        var surface = new Border { Content = switches, Padding = new Thickness(0) };
        var host = new ControlTestHost(surface, 22, 3);

        host.Click(first, 2, 0);
        Assert.True(first.IsChecked);
        Assert.False(second.IsChecked);
        Assert.True(first.IsFocused);
        Assert.False(second.IsFocused);

        host.Click(second, 2, 0);
        Assert.True(first.IsChecked);
        Assert.True(second.IsChecked);
        Assert.False(first.IsFocused);
        Assert.True(second.IsFocused);
    }

}
