using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class KeyboardInteractionTests
{
    [Fact]
    public void TextBox_WindowPipeline_UpdatesTextCaretAndRenderedCells()
    {
        var textBox = new TextBox { Text = "AC", Width = 6 };
        var host = new ControlTestHost(textBox, 6, 1);
        host.Window.SetFocus(textBox);

        host.KeyDown(ConsoleKey.LeftArrow);
        host.KeyDown(ConsoleKey.B, 'B');
        host.KeyDown(ConsoleKey.Delete);

        Assert.Equal("AB", textBox.Text);

        var buffer = host.Render();
        VirtualBufferAssertions.EqualText("AB    ", buffer);
        Assert.Equal(TuiColor.Yellow, buffer.GetPixel(1, 0).Foreground);
        Assert.Equal(TuiColor.DarkBlue, buffer.GetPixel(1, 0).Background);
        Assert.Equal(TuiColor.Black, buffer.GetPixel(2, 0).Foreground);
        Assert.Equal(TuiColor.Gray, buffer.GetPixel(2, 0).Background);
    }

    [Theory]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.Enter)]
    public void CheckBox_WindowPipeline_TogglesOnReleaseAndUpdatesRendering(ConsoleKey activationKey)
    {
        var checkBox = new CheckBox { Content = "Ready" };
        var host = new ControlTestHost(checkBox, 9, 1);
        host.Window.SetFocus(checkBox);

        var keyDown = host.KeyDown(activationKey);

        Assert.True(keyDown.Handled);
        Assert.True(checkBox.IsPressed);
        Assert.False(checkBox.IsChecked);
        VirtualBufferAssertions.EqualText("[ ] Ready", host.Render());

        var keyUp = host.KeyUp(activationKey);

        Assert.True(keyUp.Handled);
        Assert.False(checkBox.IsPressed);
        Assert.True(checkBox.IsChecked);
        VirtualBufferAssertions.EqualText("[√] Ready", host.Render());
    }

    [Fact]
    public void ThreeStateCheckBox_RepeatedKeyboardActivation_RendersEveryState()
    {
        var checkBox = new CheckBox
        {
            Content = "Mode",
            IsThreeState = true,
            CheckedChar = 'x'
        };
        var host = new ControlTestHost(checkBox, 8, 1);
        host.Window.SetFocus(checkBox);

        host.PressKey(ConsoleKey.Spacebar);
        Assert.True(checkBox.IsChecked);
        VirtualBufferAssertions.EqualText("[x] Mode", host.Render());

        host.PressKey(ConsoleKey.Spacebar);
        Assert.Null(checkBox.IsChecked);
        VirtualBufferAssertions.EqualText("[-] Mode", host.Render());

        host.PressKey(ConsoleKey.Spacebar);
        Assert.False(checkBox.IsChecked);
        VirtualBufferAssertions.EqualText("[ ] Mode", host.Render());
    }

    [Fact]
    public void Button_WindowPipeline_PreservesPressReleaseClickContract()
    {
        var button = new Button { Content = "OK" };
        var host = new ControlTestHost(button, 4, 3);
        host.Window.SetFocus(button);
        var clicks = 0;
        button.Click += (_, _) => clicks++;

        host.KeyDown(ConsoleKey.Enter);

        Assert.True(button.IsPressed);
        Assert.Equal(0, clicks);
        var pressedBuffer = host.Render();
        VirtualBufferAssertions.EqualText("┌──┐\n│OK│\n└──┘", pressedBuffer);
        Assert.Equal(button.FocusedBorderColor, pressedBuffer.GetPixel(0, 0).Foreground);

        host.KeyUp(ConsoleKey.Enter);

        Assert.False(button.IsPressed);
        Assert.Equal(1, clicks);
        VirtualBufferAssertions.EqualText("┌──┐\n│OK│\n└──┘", host.Render());
    }

    [Fact]
    public void RadioGroup_ArrowKey_MovesFocusSelectionAndRenderedMarker()
    {
        var first = new RadioButton { Content = "One", GroupName = "choice", IsChecked = true };
        var second = new RadioButton { Content = "Two", GroupName = "choice" };
        var panel = new StackPanel();
        panel.AddChild(first);
        panel.AddChild(new TextBlock { Text = "---" });
        panel.AddChild(second);
        var host = new ControlTestHost(panel, 7, 3);
        host.Window.SetFocus(first);

        var args = host.KeyDown(ConsoleKey.DownArrow);

        Assert.True(args.Handled);
        Assert.False(first.IsFocused);
        Assert.False(first.IsChecked);
        Assert.True(second.IsFocused);
        Assert.True(second.IsChecked);
        VirtualBufferAssertions.EqualText("( ) One\n---    \n(o) Two", host.Render());
    }

    [Fact]
    public void Slider_ArrowKeys_ClampValueRaiseEventsAndMoveRenderedThumb()
    {
        var slider = new Slider
        {
            Width = 11,
            Minimum = 0,
            Maximum = 10,
            Value = 9,
            SmallChange = 2
        };
        var host = new ControlTestHost(slider, 11, 1);
        host.Window.SetFocus(slider);
        var changes = 0;
        slider.ValueChanged += (_, _) => changes++;

        host.KeyDown(ConsoleKey.RightArrow);
        host.KeyDown(ConsoleKey.RightArrow);

        Assert.Equal(10, slider.Value);
        Assert.Equal(1, changes);
        VirtualBufferAssertions.EqualText("----------O", host.Render());

        host.KeyDown(ConsoleKey.LeftArrow);

        Assert.Equal(8, slider.Value);
        Assert.Equal(2, changes);
        VirtualBufferAssertions.EqualText("--------O--", host.Render());
    }

    [Fact]
    public void TabAndShiftTab_WindowPipeline_ChangesFocusedRenderingAndWraps()
    {
        var first = new CheckBox { Content = "A" };
        var second = new CheckBox { Content = "B" };
        var panel = new StackPanel();
        panel.AddChild(first);
        panel.AddChild(second);
        var host = new ControlTestHost(panel, 5, 2);
        host.Window.SetFocus(first);

        var initial = host.Render();
        Assert.Equal(first.FocusedForeground, initial.GetPixel(4, 0).Foreground);
        Assert.Equal(second.Foreground, initial.GetPixel(4, 1).Foreground);

        host.KeyDown(ConsoleKey.Tab);

        Assert.False(first.IsFocused);
        Assert.True(second.IsFocused);
        var afterTab = host.Render();
        Assert.Equal(first.Foreground, afterTab.GetPixel(4, 0).Foreground);
        Assert.Equal(second.FocusedForeground, afterTab.GetPixel(4, 1).Foreground);

        host.KeyDown(ConsoleKey.Tab, modifiers: ConsoleModifiers.Shift);

        Assert.True(first.IsFocused);
        Assert.False(second.IsFocused);
    }
}
