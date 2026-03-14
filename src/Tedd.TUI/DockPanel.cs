using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public enum Dock
{
    Left,
    Top,
    Right,
    Bottom
}

public class DockPanel : Panel
{
    public static readonly DependencyProperty LastChildFillProperty =
        DependencyProperty.Register(nameof(LastChildFill), typeof(bool), typeof(DockPanel), true);

    public bool LastChildFill
    {
        get => (bool)GetValue(LastChildFillProperty);
        set => SetValue(LastChildFillProperty, value);
    }

    public static readonly DependencyProperty DockProperty =
        DependencyProperty.RegisterAttached("Dock", typeof(Dock), typeof(DockPanel), Dock.Left);

    public static void SetDock(UIElement element, Dock value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(DockProperty, value);
    }

    public static Dock GetDock(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (Dock)element.GetValue(DockProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int accumulatedWidth = 0;
        int accumulatedHeight = 0;
        int maxWidth = 0;
        int maxHeight = 0;

        // Measure children and subtract from available size as we go
        int currentAvailableWidth = availableSize.Width;
        int currentAvailableHeight = availableSize.Height;

        int count = Children.Count;
        for (int i = 0; i < count; i++)
        {
            var child = Children[i];

            // If LastChildFill is true and this is the last child, it gets remaining space
            if (LastChildFill && i == count - 1)
            {
                child.Measure(new Size(Math.Max(0, currentAvailableWidth), Math.Max(0, currentAvailableHeight)));
                maxWidth = Math.Max(maxWidth, accumulatedWidth + child.DesiredSize.Width);
                maxHeight = Math.Max(maxHeight, accumulatedHeight + child.DesiredSize.Height);
                continue;
            }

            // Measure with current available space
            child.Measure(new Size(Math.Max(0, currentAvailableWidth), Math.Max(0, currentAvailableHeight)));
            Size desired = child.DesiredSize;
            Dock dock = GetDock(child);

            switch (dock)
            {
                case Dock.Left:
                case Dock.Right:
                    maxHeight = Math.Max(maxHeight, accumulatedHeight + desired.Height);
                    accumulatedWidth += desired.Width;
                    currentAvailableWidth -= desired.Width;
                    break;
                case Dock.Top:
                case Dock.Bottom:
                    maxWidth = Math.Max(maxWidth, accumulatedWidth + desired.Width);
                    accumulatedHeight += desired.Height;
                    currentAvailableHeight -= desired.Height;
                    break;
            }
        }

        return new Size(Math.Max(maxWidth, accumulatedWidth), Math.Max(maxHeight, accumulatedHeight));
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int left = 0;
        int top = 0;
        int right = 0;
        int bottom = 0;
        int width = finalSize.Width;
        int height = finalSize.Height;

        int count = Children.Count;
        for (int i = 0; i < count; i++)
        {
            var child = Children[i];

            if (LastChildFill && i == count - 1)
            {
                child.Arrange(new Rect(left, top, Math.Max(0, width - (left + right)), Math.Max(0, height - (top + bottom))));
                break;
            }

            Dock dock = GetDock(child);
            Size desired = child.DesiredSize;

            switch (dock)
            {
                case Dock.Left:
                    child.Arrange(new Rect(left, top, Math.Min(desired.Width, width - (left + right)), Math.Max(0, height - (top + bottom))));
                    left += desired.Width;
                    break;
                case Dock.Top:
                    child.Arrange(new Rect(left, top, Math.Max(0, width - (left + right)), Math.Min(desired.Height, height - (top + bottom))));
                    top += desired.Height;
                    break;
                case Dock.Right:
                    child.Arrange(new Rect(width - right - desired.Width, top, Math.Min(desired.Width, width - (left + right)), Math.Max(0, height - (top + bottom))));
                    right += desired.Width;
                    break;
                case Dock.Bottom:
                    child.Arrange(new Rect(left, height - bottom - desired.Height, Math.Max(0, width - (left + right)), Math.Min(desired.Height, height - (top + bottom))));
                    bottom += desired.Height;
                    break;
            }
        }
    }
}
