using System;

namespace Tedd.TUI.Controls;

public class WrapPanel : Panel
{
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register("Orientation", typeof(Orientation), typeof(WrapPanel), Orientation.Horizontal);

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size panelSize = new Size(0, 0);
        int currentLineSizeX = 0;
        int currentLineSizeY = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            Size desired = child.DesiredSize;

            if (Orientation == Orientation.Horizontal)
            {
                // Check if we need to wrap
                if (currentLineSizeX + desired.Width > availableSize.Width && currentLineSizeX > 0)
                {
                    // Update panel max width
                    panelSize.Width = Math.Max(panelSize.Width, currentLineSizeX);
                    // Add current line height to panel height
                    panelSize.Height += currentLineSizeY;

                    // Reset current line
                    currentLineSizeX = desired.Width;
                    currentLineSizeY = desired.Height;
                }
                else
                {
                    currentLineSizeX += desired.Width;
                    currentLineSizeY = Math.Max(currentLineSizeY, desired.Height);
                }
            }
            else // Vertical
            {
                if (currentLineSizeY + desired.Height > availableSize.Height && currentLineSizeY > 0)
                {
                    // Update panel max height
                    panelSize.Height = Math.Max(panelSize.Height, currentLineSizeY);
                    // Add current line width to panel width
                    panelSize.Width += currentLineSizeX;

                    // Reset current line
                    currentLineSizeX = desired.Width;
                    currentLineSizeY = desired.Height;
                }
                else
                {
                    currentLineSizeY += desired.Height;
                    currentLineSizeX = Math.Max(currentLineSizeX, desired.Width);
                }
            }
        }

        // Add the last line
        if (Orientation == Orientation.Horizontal)
        {
            panelSize.Width = Math.Max(panelSize.Width, currentLineSizeX);
            panelSize.Height += currentLineSizeY;
        }
        else
        {
            panelSize.Height = Math.Max(panelSize.Height, currentLineSizeY);
            panelSize.Width += currentLineSizeX;
        }

        return panelSize;
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int x = 0;
        int y = 0;
        int currentLineSizeX = 0;
        int currentLineSizeY = 0;

        foreach (var child in Children)
        {
            Size desired = child.DesiredSize;

            if (Orientation == Orientation.Horizontal)
            {
                // Check if we need to wrap
                if (x + desired.Width > finalSize.Width && x > 0)
                {
                    // Move to the next line
                    x = 0;
                    y += currentLineSizeY;
                    currentLineSizeY = desired.Height;
                }
                else
                {
                    currentLineSizeY = Math.Max(currentLineSizeY, desired.Height);
                }

                child.Arrange(new Rect(x, y, desired.Width, desired.Height));
                x += desired.Width;
            }
            else // Vertical
            {
                // Check if we need to wrap
                if (y + desired.Height > finalSize.Height && y > 0)
                {
                    // Move to the next column
                    y = 0;
                    x += currentLineSizeX;
                    currentLineSizeX = desired.Width;
                }
                else
                {
                    currentLineSizeX = Math.Max(currentLineSizeX, desired.Width);
                }

                child.Arrange(new Rect(x, y, desired.Width, desired.Height));
                y += desired.Height;
            }
        }
    }
}
