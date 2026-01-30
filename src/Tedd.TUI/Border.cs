using System;

namespace Tedd.TUI;

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
            }
        }
    }

    protected override int VisualChildrenCount => _child != null ? 1 : 0;

    protected override UIElement GetVisualChild(int index)
    {
        if (_child != null && index == 0) return _child;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    // Spec says BorderThickness/Color. We'll assume thickness 1 for now or implement property.
    // Let's implement Color.
    
    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(ConsoleColor), typeof(Border), ConsoleColor.White);

    public ConsoleColor BorderColor
    {
        get { return (ConsoleColor)GetValue(BorderColorProperty); }
        set { SetValue(BorderColorProperty, value); }
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

        if (w < 2 || h < 2) return;

        // Draw Box
        // Corners
        buffer.SetPixel(x, y, '+', c, ConsoleColor.Black);
        buffer.SetPixel(x + w - 1, y, '+', c, ConsoleColor.Black);
        buffer.SetPixel(x, y + h - 1, '+', c, ConsoleColor.Black);
        buffer.SetPixel(x + w - 1, y + h - 1, '+', c, ConsoleColor.Black);

        // Horizontal
        for (int i = 1; i < w - 1; i++)
        {
            buffer.SetPixel(x + i, y, '-', c, ConsoleColor.Black);
            buffer.SetPixel(x + i, y + h - 1, '-', c, ConsoleColor.Black);
        }

        // Vertical
        for (int i = 1; i < h - 1; i++)
        {
            buffer.SetPixel(x, y + i, '|', c, ConsoleColor.Black);
            buffer.SetPixel(x + w - 1, y + i, '|', c, ConsoleColor.Black);
        }

        if (Child != null)
        {
            Child.Render(buffer, x, y);
        }
    }
}
