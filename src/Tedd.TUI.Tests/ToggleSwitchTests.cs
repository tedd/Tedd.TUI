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

        host.MouseDown(2, 0);
        host.MouseUp(2, 0);

        // After first click
        Assert.Equal(true, ts.IsChecked);
        Assert.True(ts.IsFocused);
        VirtualBufferAssertions.EqualText("[──●] On  Sound", host.Render());

        host.MouseDown(2, 0);
        host.MouseUp(2, 0);

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

        host.MouseDown(1, 0);
        host.MouseUp(1, 0);

        Assert.Equal(1, checkedCount);
        Assert.Equal(0, uncheckedCount);
        Assert.Equal(1, clickCount);

        host.MouseDown(1, 0);
        host.MouseUp(1, 0);

        Assert.Equal(1, checkedCount);
        Assert.Equal(1, uncheckedCount);
        Assert.Equal(2, clickCount);
    }

    [Fact]
    public void Disabled_IgnoresMouseAndKeyboard()
    {
        var ts = new ToggleSwitch { IsEnabled = false };
        var host = new ControlTestHost(ts, 9, 1);

        host.MouseDown(1, 0);
        host.MouseUp(1, 0);

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
}
