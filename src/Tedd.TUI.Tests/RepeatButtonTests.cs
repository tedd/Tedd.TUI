using System;
using System.Threading;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class RepeatButtonTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var rb = new RepeatButton();
        Assert.Equal(ClickMode.Press, rb.ClickMode);
        Assert.Equal(500, rb.Delay);
        Assert.Equal(100, rb.Interval);
        Assert.True(rb.Focusable);
    }

    [Fact]
    public void Render_LooksLikeButton()
    {
        var rb = new RepeatButton { Content = "+" };
        var host = new ControlTestHost(rb, 3, 3);

        VirtualBufferAssertions.EqualText("┌─┐\n│+│\n└─┘", host.Render());
    }

    [Fact]
    public void MouseDown_FiresClickImmediately()
    {
        var rb = new RepeatButton { Content = "+" };
        var host = new ControlTestHost(rb, 3, 3);
        int clicks = 0;
        rb.Click += (_, _) => clicks++;

        // Before
        Assert.Equal(0, clicks);
        Assert.False(rb.IsPressed);

        host.MouseDown(1, 1);

        // After: Press click mode fires on the down event
        Assert.Equal(1, clicks);
        Assert.True(rb.IsPressed);
        Assert.True(rb.IsFocused);

        host.MouseUp(1, 1);

        // Release does not fire an extra click in Press mode
        Assert.Equal(1, clicks);
        Assert.False(rb.IsPressed);
    }

    [Fact]
    public void RepeatTicks_WhilePressed_FireAdditionalClicks()
    {
        var rb = new RepeatButton { Content = "+" };
        var host = new ControlTestHost(rb, 3, 3);
        int clicks = 0;
        rb.Click += (_, _) => clicks++;

        host.MouseDown(1, 1);
        Assert.Equal(1, clicks);

        rb.OnRepeatTick();
        rb.OnRepeatTick();

        Assert.Equal(3, clicks);
    }

    [Fact]
    public void MouseUp_StopsRepeating()
    {
        var rb = new RepeatButton { Content = "+" };
        var host = new ControlTestHost(rb, 3, 3);
        int clicks = 0;
        rb.Click += (_, _) => clicks++;

        host.MouseDown(1, 1);
        rb.OnRepeatTick();
        Assert.Equal(2, clicks);

        host.MouseUp(1, 1);

        // A tick that races the release must not fire once the button is no longer pressed
        rb.OnRepeatTick();
        Assert.Equal(2, clicks);
    }

    [Theory]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.Enter)]
    public void KeyDown_FiresClickPerKeyDown(ConsoleKey key)
    {
        var rb = new RepeatButton { Content = "+" };
        var host = new ControlTestHost(rb, 3, 3);
        rb.Focus();
        int clicks = 0;
        rb.Click += (_, _) => clicks++;

        // Before
        Assert.Equal(0, clicks);

        host.KeyDown(key);
        Assert.Equal(1, clicks);

        // Terminal key auto-repeat re-sends KeyDown while the key is held
        host.KeyDown(key);
        host.KeyDown(key);
        Assert.Equal(3, clicks);

        host.KeyUp(key);
        Assert.Equal(3, clicks);
        Assert.False(rb.IsPressed);
    }

    [Fact]
    public void Timer_FiresRepeatedClicksWhileHeld()
    {
        var rb = new RepeatButton { Content = "+", Delay = 1, Interval = 1 };
        var host = new ControlTestHost(rb, 3, 3);
        int clicks = 0;
        rb.Click += (_, _) => Interlocked.Increment(ref clicks);

        host.MouseDown(1, 1);
        bool repeated = SpinWait.SpinUntil(() => Volatile.Read(ref clicks) >= 3, TimeSpan.FromSeconds(10));
        host.MouseUp(1, 1);

        Assert.True(repeated, $"Expected at least 3 clicks from the repeat timer, got {Volatile.Read(ref clicks)}");

        // After release no further clicks arrive
        int after = Volatile.Read(ref clicks);
        Thread.Sleep(50);
        Assert.Equal(after, Volatile.Read(ref clicks));
    }

    [Fact]
    public void Disabled_IgnoresMouseAndTicks()
    {
        var rb = new RepeatButton { Content = "+", IsEnabled = false };
        var host = new ControlTestHost(rb, 3, 3);
        int clicks = 0;
        rb.Click += (_, _) => clicks++;

        host.Click(rb, 1, 1);
        rb.OnRepeatTick();

        Assert.Equal(0, clicks);
        Assert.False(rb.IsPressed);
    }

    [Fact]
    public void Click_NestedRepeatButtons_FiresOnlyTargetButton()
    {
        var first = new RepeatButton { Content = "-" };
        var second = new RepeatButton { Content = "+" };
        var firstClicks = 0;
        var secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.AddChild(first);
        buttons.AddChild(new TextBlock { Text = "  " });
        buttons.AddChild(second);
        var surface = new Border { Content = buttons, Padding = new Thickness(0) };
        var host = new ControlTestHost(surface, 10, 5);

        host.Click(first, 1, 1);
        Assert.Equal(1, firstClicks);
        Assert.Equal(0, secondClicks);
        Assert.False(first.IsPressed);

        host.Click(second, 1, 1);
        Assert.Equal(1, firstClicks);
        Assert.Equal(1, secondClicks);
        Assert.False(second.IsPressed);
    }

}
