using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

/// <summary>
/// Mouse hover (mouseover) behavior: IsMouseOver tracking, MouseEnter/MouseLeave events,
/// and the default hover color change of the interactive controls. All interaction goes
/// through the window pipeline by simulating mouse movement and clicks.
/// </summary>
public class MouseOverTests
{
    private static (Button Button, ControlTestHost Host) CreateButtonHost()
    {
        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        return (button, new ControlTestHost(button, 8, 4));
    }

    [Fact]
    public void MouseMove_OverAndOffButton_TogglesIsMouseOverAndRaisesEnterLeave()
    {
        var (button, host) = CreateButtonHost();
        int enters = 0, leaves = 0;
        button.AddHandler(UIElement.MouseEnterEvent, new RoutedEventHandler((_, _) => enters++));
        button.AddHandler(UIElement.MouseLeaveEvent, new RoutedEventHandler((_, _) => leaves++));

        Assert.False(button.IsMouseOver);

        host.MouseMove(1, 1);

        Assert.True(button.IsMouseOver);
        Assert.NotNull(host.Window.HoveredElement);
        Assert.Equal(1, enters);
        Assert.Equal(0, leaves);

        // Moving within the same element must not re-raise MouseEnter.
        host.MouseMove(2, 1);

        Assert.True(button.IsMouseOver);
        Assert.Equal(1, enters);

        // Move off the button (still inside the window).
        host.MouseMove(6, 3);

        Assert.False(button.IsMouseOver);
        Assert.Null(host.Window.HoveredElement);
        Assert.Equal(1, enters);
        Assert.Equal(1, leaves);
    }

    [Fact]
    public void Button_Hover_RendersDefaultHoverColorsAndRevertsOnLeave()
    {
        var (button, host) = CreateButtonHost();

        var buffer = host.Render();
        Assert.Equal(TuiColor.Gray, buffer.GetPixel(0, 0).Foreground);   // border
        Assert.Equal(TuiColor.White, buffer.GetPixel(1, 1).Foreground);  // 'O' of OK

        host.MouseMove(1, 1);
        buffer = host.Render();
        Assert.Equal('O', buffer.GetPixel(1, 1).Character);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(0, 0).Foreground);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(1, 1).Foreground);

        host.MouseMove(6, 3);
        buffer = host.Render();
        Assert.Equal(TuiColor.Gray, buffer.GetPixel(0, 0).Foreground);
        Assert.Equal(TuiColor.White, buffer.GetPixel(1, 1).Foreground);
    }

    [Fact]
    public void Button_HoverColors_AreOverridable()
    {
        var (button, host) = CreateButtonHost();
        button.HoverForeground = TuiColor.Magenta;
        button.HoverBorderColor = TuiColor.Green;

        host.MouseMove(1, 1);

        var buffer = host.Render();
        Assert.Equal(TuiColor.Green, buffer.GetPixel(0, 0).Foreground);
        Assert.Equal(TuiColor.Magenta, buffer.GetPixel(1, 1).Foreground);
    }

    [Fact]
    public void Button_FocusWinsOverHover()
    {
        var (button, host) = CreateButtonHost();

        // Click focuses the button; the pointer is now also hovering it.
        host.Click(1, 1);

        Assert.True(button.IsFocused);
        Assert.True(button.IsMouseOver);
        var buffer = host.Render();
        Assert.Equal(TuiColor.Yellow, buffer.GetPixel(0, 0).Foreground);
        Assert.Equal(TuiColor.Yellow, buffer.GetPixel(1, 1).Foreground);
    }

    [Fact]
    public void Button_HoverThenClick_ClicksAndKeepsHover()
    {
        var (button, host) = CreateButtonHost();
        var clicks = 0;
        button.Click += (_, _) => clicks++;

        host.MouseMove(1, 1);
        host.Click(1, 1);

        Assert.Equal(1, clicks);
        Assert.True(button.IsMouseOver);
    }

    [Fact]
    public void DisabledControl_IsNotHoverable()
    {
        var checkBox = new CheckBox
        {
            Content = "Locked",
            IsEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var host = new ControlTestHost(checkBox, 12, 1);

        host.MouseMove(1, 0);

        Assert.False(checkBox.IsMouseOver);
        var buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(4, 0).Foreground); // 'L' of Locked
    }

    [Fact]
    public void MouseMove_BetweenSiblings_MovesHoverAndKeepsAncestorHovered()
    {
        var first = new CheckBox { Content = "A" };
        var second = new CheckBox { Content = "B" };
        var panel = new StackPanel();
        panel.AddChild(first);
        panel.AddChild(second);
        var host = new ControlTestHost(panel, 6, 2);
        int panelEnters = 0;
        panel.AddHandler(UIElement.MouseEnterEvent, new RoutedEventHandler((_, _) => panelEnters++));

        host.MouseMove(1, 0);

        Assert.True(first.IsMouseOver);
        Assert.False(second.IsMouseOver);
        Assert.True(panel.IsMouseOver);

        host.MouseMove(1, 1);

        Assert.False(first.IsMouseOver);
        Assert.True(second.IsMouseOver);
        Assert.True(panel.IsMouseOver);
        // The ancestor stayed hovered the whole time, so it entered exactly once.
        Assert.Equal(1, panelEnters);
    }

    [Fact]
    public void MouseCapture_KeepsHoverOnCapturedElementOutsideItsBounds()
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
        Assert.True(scrollBar.IsMouseOver);

        host.MouseMove(0, 30);

        Assert.True(scrollBar.IsMouseOver);
    }

    [Fact]
    public void CheckBox_Hover_ChangesLabelColorByDefault()
    {
        var checkBox = new CheckBox { Content = "Hi", HorizontalAlignment = HorizontalAlignment.Left };
        var host = new ControlTestHost(checkBox, 8, 1);

        host.MouseMove(1, 0);
        var buffer = host.Render();
        Assert.Equal('H', buffer.GetPixel(4, 0).Character);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(4, 0).Foreground);

        host.MouseMove(7, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(4, 0).Foreground);
    }

    [Fact]
    public void RadioButton_Hover_ChangesLabelColorByDefault()
    {
        var radio = new RadioButton { Content = "R", HorizontalAlignment = HorizontalAlignment.Left };
        var host = new ControlTestHost(radio, 7, 1);

        host.MouseMove(1, 0);
        var buffer = host.Render();
        Assert.Equal('R', buffer.GetPixel(4, 0).Character);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(4, 0).Foreground);

        host.MouseMove(6, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(4, 0).Foreground);
    }

    [Fact]
    public void ToggleSwitch_Hover_ChangesLabelColorByDefault()
    {
        var toggle = new ToggleSwitch { HorizontalAlignment = HorizontalAlignment.Left };
        var host = new ControlTestHost(toggle, 11, 1);

        host.MouseMove(2, 0);
        var buffer = host.Render();
        Assert.Equal('O', buffer.GetPixel(6, 0).Character); // 'O' of Off
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(6, 0).Foreground);

        host.MouseMove(10, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(6, 0).Foreground);
    }

    [Fact]
    public void Slider_Hover_ChangesThumbColorByDefault()
    {
        var slider = new Slider
        {
            Width = 11,
            Minimum = 0,
            Maximum = 10,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var host = new ControlTestHost(slider, 13, 1);

        host.MouseMove(5, 0);
        var buffer = host.Render();
        Assert.Equal('O', buffer.GetPixel(0, 0).Character); // thumb at Value = 0
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(0, 0).Foreground);

        host.MouseMove(12, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void NumericUpDown_Hover_ChangesValueColorByDefault()
    {
        var numeric = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 9,
            Value = 5,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var host = new ControlTestHost(numeric, 11, 1);

        host.MouseMove(1, 0);
        var buffer = host.Render();
        Assert.Equal('5', buffer.GetPixel(4, 0).Character);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(4, 0).Foreground);

        host.MouseMove(10, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(4, 0).Foreground);
    }

    [Fact]
    public void DatePicker_Hover_ChangesTextColorByDefault()
    {
        var picker = new DatePicker
        {
            SelectedDate = new DateTime(2026, 7, 19),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var host = new ControlTestHost(picker, 14, 1);

        host.MouseMove(3, 0);
        var buffer = host.Render();
        Assert.Equal('2', buffer.GetPixel(0, 0).Character);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(0, 0).Foreground);

        host.MouseMove(13, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void TimePicker_Hover_ChangesTextColorByDefault()
    {
        var picker = new TimePicker
        {
            SelectedTime = new TimeSpan(12, 34, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var host = new ControlTestHost(picker, 7, 1);

        host.MouseMove(2, 0);
        var buffer = host.Render();
        Assert.Equal('1', buffer.GetPixel(0, 0).Character);
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(0, 0).Foreground);

        host.MouseMove(6, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void ComboBox_Hover_ChangesTextColorByDefault()
    {
        var comboBox = new ComboBox { Width = 10, HorizontalAlignment = HorizontalAlignment.Left };
        var host = new ControlTestHost(comboBox, 12, 1);

        host.MouseMove(2, 0);
        var buffer = host.Render();
        Assert.Equal(TuiColor.Cyan, buffer.GetPixel(0, 0).Foreground);

        host.MouseMove(11, 0);
        buffer = host.Render();
        Assert.Equal(TuiColor.White, buffer.GetPixel(0, 0).Foreground);
    }
}
