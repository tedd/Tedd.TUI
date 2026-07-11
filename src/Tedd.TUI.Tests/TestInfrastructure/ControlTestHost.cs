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
