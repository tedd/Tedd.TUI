using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class MouseInteractionTests
{
    [Fact]
    public void Button_WindowPipeline_PressesFocusesClicksAndRenders()
    {
        var button = new Button { Content = "OK" };
        var host = new ControlTestHost(button, 4, 3);
        var clicks = 0;
        button.Click += (_, _) => clicks++;

        var down = host.MouseDown(1, 1);

        Assert.True(down.Handled);
        Assert.True(button.IsFocused);
        Assert.True(button.IsPressed);
        Assert.Equal(0, clicks);
        VirtualBufferAssertions.EqualText("┌──┐\n│OK│\n└──┘", host.Render());

        host.MouseUp(1, 1);

        Assert.False(button.IsPressed);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void PreviewMouseDown_WhenHandled_PreventsControlStateChange()
    {
        var button = new Button { Content = "OK" };
        var panel = new StackPanel();
        panel.AddChild(button);
        var host = new ControlTestHost(panel, 4, 3);
        panel.AddHandler(UIElement.PreviewMouseDownEvent, new RoutedEventHandler((_, e) => e.Handled = true));

        var args = host.MouseDown(1, 1);

        Assert.True(args.Handled);
        Assert.True(button.IsFocused);
        Assert.False(button.IsPressed);
    }

    [Fact]
    public void DisabledControl_IsExcludedFromHitTestingAndCannotActivate()
    {
        var checkBox = new CheckBox { Content = "Locked", IsEnabled = false };
        var host = new ControlTestHost(checkBox, 10, 1);
        var clicks = 0;
        checkBox.Click += (_, _) => clicks++;

        host.MouseDown(1, 0);
        host.MouseUp(1, 0);

        Assert.False(checkBox.IsFocused);
        Assert.False(checkBox.IsPressed);
        Assert.False(checkBox.IsChecked);
        Assert.Equal(0, clicks);
        VirtualBufferAssertions.EqualText("[ ] Locked", host.Render());
    }

    [Fact]
    public void Slider_NestedHitTest_MapsGlobalClickToLocalValueAndRendering()
    {
        var slider = new Slider
        {
            Width = 11,
            Minimum = 0,
            Maximum = 10
        };
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.AddChild(new TextBlock { Text = "xx" });
        panel.AddChild(slider);
        var host = new ControlTestHost(panel, 13, 1);

        var args = host.MouseDown(7, 0);

        Assert.True(args.Handled);
        Assert.True(slider.IsFocused);
        Assert.Equal(5, slider.Value);
        VirtualBufferAssertions.EqualText("xx-----O-----", host.Render());
    }

    [Fact]
    public void TextBox_MouseCaretThenKeyboardInput_UpdatesTextAndCaretRendering()
    {
        var textBox = new TextBox { Text = "ABCDE", Width = 6 };
        var host = new ControlTestHost(textBox, 6, 1);

        host.MouseDown(1, 0);
        host.KeyDown(ConsoleKey.X, 'X');

        Assert.True(textBox.IsFocused);
        Assert.Equal("AXBCDE", textBox.Text);
        var buffer = host.Render();
        VirtualBufferAssertions.EqualText("AXBCDE", buffer);
        Assert.Equal(TuiColor.Gray, buffer.GetPixel(2, 0).Background);
        Assert.Equal('B', buffer.GetPixel(2, 0).Character);
    }

    [Fact]
    public void ScrollBar_DragCapture_ContinuesOutsideBoundsUntilMouseUp()
    {
        var scrollBar = new ScrollBar
        {
            Width = 1,
            Height = 12,
            Minimum = 0,
            Maximum = 100,
            ViewportSize = 10,
            Value = 0
        };
        var host = new ControlTestHost(scrollBar, 1, 12);

        host.MouseDown(0, 1);
        Assert.Same(scrollBar, host.Window.CapturedElement);

        host.MouseMove(0, 30);

        Assert.Equal(100, scrollBar.Value);
        Assert.Equal('█', host.Render().GetPixel(0, 10).Character);

        host.MouseUp(0, 30);

        Assert.Null(host.Window.CapturedElement);
        host.MouseMove(0, 1);
        Assert.Equal(100, scrollBar.Value);
    }
    [Fact]
    public void Button_ReleaseOutside_ClearsCaptureWithoutClicking()
    {
        var button = new Button { Content = "OK" };
        var host = new ControlTestHost(button, 4, 3);
        var clicks = 0;
        button.Click += (_, _) => clicks++;

        host.MouseDown(1, 1);

        Assert.True(button.IsPressed);
        Assert.Same(button, host.Window.CapturedElement);

        host.MouseMove(20, 20);
        host.MouseUp(20, 20);

        Assert.False(button.IsPressed);
        Assert.Null(host.Window.CapturedElement);
        Assert.Equal(0, clicks);
    }

}

