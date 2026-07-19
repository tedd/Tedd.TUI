using System.Text;

namespace Tedd.TUI.Tests.TestInfrastructure;

/// <summary>
/// Hosts a control through the same window layout, input routing, and rendering stages
/// used by the terminal front ends. Interaction tests should prefer this over calling
/// control event handlers directly.
/// </summary>
internal sealed class ControlTestHost
{
    public ControlTestHost(UIElement content, int width, int height)
    {
        Width = width;
        Height = height;
        Window = new TuiWindow { Content = content };
        Window.Measure(new Size(width, height));
        Window.Arrange(new Rect(0, 0, width, height));
    }

    public TuiWindow Window { get; }
    public int Width { get; }
    public int Height { get; }

    public KeyEventArgs KeyDown(
        ConsoleKey key,
        char keyChar = '\0',
        ConsoleModifiers modifiers = ConsoleModifiers.None)
    {
        var args = new KeyEventArgs(UIElement.KeyDownEvent)
        {
            Key = key,
            KeyChar = keyChar,
            Modifiers = modifiers
        };

        Window.ProcessKey(args);
        return args;
    }

    public KeyEventArgs KeyUp(
        ConsoleKey key,
        char keyChar = '\0',
        ConsoleModifiers modifiers = ConsoleModifiers.None)
    {
        var args = new KeyEventArgs(UIElement.KeyUpEvent)
        {
            Key = key,
            KeyChar = keyChar,
            Modifiers = modifiers
        };

        Window.ProcessKey(args);
        return args;
    }

    public void PressKey(
        ConsoleKey key,
        char keyChar = '\0',
        ConsoleModifiers modifiers = ConsoleModifiers.None)
    {
        KeyDown(key, keyChar, modifiers);
        KeyUp(key, keyChar, modifiers);
    }

    public VirtualBuffer Render()
    {
        var buffer = new VirtualBuffer(Width, Height);
        Window.Render(buffer);
        return buffer;
    }

    public MouseEventArgs MouseDown(int x, int y) =>
        Mouse(UIElement.MouseDownEvent, x, y);

    public MouseEventArgs MouseMove(int x, int y) =>
        Mouse(UIElement.MouseMoveEvent, x, y);

    public MouseEventArgs MouseUp(int x, int y) =>
        Mouse(UIElement.MouseUpEvent, x, y);

    /// <summary>Mouse down at a fractional cell position, as pixel-based hosts report it.</summary>
    public MouseEventArgs MouseDownF(double x, double y) =>
        MouseF(UIElement.MouseDownEvent, x, y);

    /// <summary>Mouse move at a fractional cell position, as pixel-based hosts report it.</summary>
    public MouseEventArgs MouseMoveF(double x, double y) =>
        MouseF(UIElement.MouseMoveEvent, x, y);

    /// <summary>Mouse up at a fractional cell position, as pixel-based hosts report it.</summary>
    public MouseEventArgs MouseUpF(double x, double y) =>
        MouseF(UIElement.MouseUpEvent, x, y);

    /// <summary>Wheel rotation at a cell position; delta is WPF-style (±120 per notch).</summary>
    public MouseWheelEventArgs MouseWheel(int x, int y, int delta)
    {
        var args = new MouseWheelEventArgs
        {
            GlobalX = x,
            GlobalY = y,
            Delta = delta
        };

        Window.ProcessMouse(args);
        return args;
    }

    /// <summary>
    /// Performs the complete primary-button click sequence used by the platform hosts.
    /// Coordinates are relative to the test window.
    /// </summary>
    public (MouseEventArgs Down, MouseEventArgs Up) Click(int x, int y)
    {
        var down = MouseDown(x, y);
        var up = MouseUp(x, y);
        return (down, up);
    }

    /// <summary>
    /// Performs a complete primary-button click at a point relative to an element.
    /// Converting through <see cref="UIElement.PointToScreen"/> makes this suitable for
    /// deeply nested elements, including content translated by one or more scroll viewers.
    /// </summary>
    public (MouseEventArgs Down, MouseEventArgs Up) Click(UIElement element, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(element);

        var screenPoint = element.PointToScreen(new Point(x, y));
        return Click(screenPoint.X, screenPoint.Y);
    }

    private MouseEventArgs Mouse(RoutedEvent routedEvent, int x, int y)
    {
        var args = new MouseEventArgs(routedEvent)
        {
            GlobalX = x,
            GlobalY = y
        };

        Window.ProcessMouse(args);
        return args;
    }

    private MouseEventArgs MouseF(RoutedEvent routedEvent, double x, double y)
    {
        var args = new MouseEventArgs(routedEvent)
        {
            GlobalX = (int)Math.Floor(x),
            GlobalY = (int)Math.Floor(y),
            GlobalXF = x,
            GlobalYF = y
        };

        Window.ProcessMouse(args);
        return args;
    }

}

internal static class VirtualBufferAssertions
{
    public static void EqualText(string expected, VirtualBuffer actual)
    {
        Assert.Equal(Normalize(expected), GetText(actual));
    }

    public static string GetText(VirtualBuffer buffer)
    {
        var result = new StringBuilder(buffer.Height * (buffer.Width + Environment.NewLine.Length));

        for (var y = 0; y < buffer.Height; y++)
        {
            if (y > 0)
            {
                result.AppendLine();
            }

            for (var x = 0; x < buffer.Width; x++)
            {
                result.Append(buffer.GetPixel(x, y).Character);
            }
        }

        return result.ToString();
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);
}
