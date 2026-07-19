using System;

namespace Tedd.TUI.Tests.CustomControls;

/// <summary>
/// A user-defined control the way a library consumer would write one: subclass
/// <see cref="Control"/>, register a dependency property, override measurement and
/// rendering. Used to prove XAML files can reference custom controls through
/// clr-namespace/using xmlns mappings and bind to their custom properties.
/// </summary>
public class BadgeControl : Control
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(BadgeControl), string.Empty);

    public string Label
    {
        get => (string)GetValue(LabelProperty)!;
        set => SetValue(LabelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // "[" + label + "]"
        return new Size(Label.Length + 2, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        string text = "[" + Label + "]";
        var fg = Foreground;
        var bg = Background ?? TuiColor.Black;
        for (int i = 0; i < text.Length && i < RenderSize.Width; i++)
        {
            buffer.SetPixel(x + i, y, text[i], fg, bg);
        }
    }
}

/// <summary>
/// A subclass of a built-in control that replaces the renderer entirely: the default
/// Button chrome is skipped and the content is drawn as "&gt;text&lt;". Click, focus and
/// keyboard behavior are inherited from ButtonBase untouched.
/// </summary>
public class FancyButton : Button
{
    public int RenderCallCount { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        string text = Content?.ToString() ?? "";
        return new Size(text.Length + 2, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        RenderCallCount++;
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        string text = ">" + (Content?.ToString() ?? "") + "<";
        var fg = Foreground;
        var bg = Background ?? TuiColor.Black;
        for (int i = 0; i < text.Length && i < RenderSize.Width; i++)
        {
            buffer.SetPixel(x + i, y, text[i], fg, bg);
        }
    }
}
