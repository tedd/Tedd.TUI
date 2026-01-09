using System;

namespace Tedd.TUI;

public enum BorderStyle
{
    None,
    Single,
    Double
}

public class Border : UIElement
{
    private UIElement _child;
    public UIElement Child
    {
        get => _child;
        set
        {
            _child = value;
            if (_child != null)
            {
                _child.Parent = this;
                _child.DataContext = this.DataContext;
            }
        }
    }

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(ConsoleColor), typeof(Border), ConsoleColor.White);

    public ConsoleColor BorderColor
    {
        get { return (ConsoleColor)GetValue(BorderColorProperty); }
        set { SetValue(BorderColorProperty, value); }
    }

    public static readonly DependencyProperty BorderStyleProperty =
        DependencyProperty.Register("BorderStyle", typeof(BorderStyle), typeof(Border), BorderStyle.Single);

    public BorderStyle BorderStyle
    {
        get { return (BorderStyle)GetValue(BorderStyleProperty); }
        set { SetValue(BorderStyleProperty, value); }
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        if (Child != null)
        {
            Child.DataContext = newValue;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size childSize = new Size(0, 0);
        if (Child != null)
        {
            // Border takes 2 width/height (1 each side)
            Size childAvailable = new Size(
                Math.Max(0, availableSize.Width - 2),
                Math.Max(0, availableSize.Height - 2)
            );
            
            Child.Measure(childAvailable);
            childSize = Child.DesiredSize;
        }

        return new Size(childSize.Width + 2, childSize.Height + 2);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (Child != null)
        {
            Child.Arrange(new Rect(1, 1, Math.Max(0, finalSize.Width - 2), Math.Max(0, finalSize.Height - 2)));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int w = RenderSize.Width;
        int h = RenderSize.Height;
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        ConsoleColor c = BorderColor;
        BorderStyle style = BorderStyle;

        if (style == BorderStyle.None)
        {
             if (Child != null) Child.Render(buffer, x - 1, y - 1); // Adjust offset?
             // Wait, if no border, we still reserved space in Measure?
             // If style is None, we probably shouldn't reserve space.
             // But for now let's assume layout reserved space and we just don't draw, or we draw empty space.
             // Usually BorderThickness is separate property.
             // Simplification: We always reserve 1px border.
        }
        else if (w >= 2 && h >= 2)
        {
            char hLine, vLine, tl, tr, bl, br;

            if (style == BorderStyle.Double)
            {
                hLine = '═'; // U+2550
                vLine = '║'; // U+2551
                tl = '╔';    // U+2554
                tr = '╗';    // U+2557
                bl = '╚';    // U+255A
                br = '╝';    // U+255D
            }
            else // Single
            {
                hLine = '─'; // U+2500
                vLine = '│'; // U+2502
                tl = '┌';    // U+250C
                tr = '┐';    // U+2510
                bl = '└';    // U+2514
                br = '┘';    // U+2518
            }

            // Draw Box
            // Corners
            buffer.SetPixel(x, y, tl, c, ConsoleColor.Black);
            buffer.SetPixel(x + w - 1, y, tr, c, ConsoleColor.Black);
            buffer.SetPixel(x, y + h - 1, bl, c, ConsoleColor.Black);
            buffer.SetPixel(x + w - 1, y + h - 1, br, c, ConsoleColor.Black);

            // Horizontal
            for (int i = 1; i < w - 1; i++)
            {
                buffer.SetPixel(x + i, y, hLine, c, ConsoleColor.Black);
                buffer.SetPixel(x + i, y + h - 1, hLine, c, ConsoleColor.Black);
            }

            // Vertical
            for (int i = 1; i < h - 1; i++)
            {
                buffer.SetPixel(x, y + i, vLine, c, ConsoleColor.Black);
                buffer.SetPixel(x + w - 1, y + i, vLine, c, ConsoleColor.Black);
            }
        }

        if (Child != null)
        {
            // Child render is relative to its Arrange rect (1,1).
            // Render method uses (x,y) which is TopLeft of border.
            // Child.Render(buffer, x, y) calls child's render with child.RenderSize.X/Y added.
            // child.RenderSize.X is 1. So it draws at x+1, y+1. Correct.
            Child.Render(buffer, x, y);
        }
    }
}
