using System;

namespace Tedd.TUI;

public static class VisualTreeHelper
{
    public static int GetChildrenCount(DependencyObject reference)
    {
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (reference is UIElement uiElement)
        {
            return uiElement.VisualChildrenCount;
        }

        return 0;
    }

    public static DependencyObject GetChild(DependencyObject reference, int childIndex)
    {
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (reference is UIElement uiElement)
        {
            return uiElement.GetVisualChild(childIndex);
        }

        throw new ArgumentOutOfRangeException(nameof(childIndex));
    }

    public static DependencyObject? GetParent(DependencyObject reference)
    {
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (reference is UIElement uiElement)
        {
            return uiElement.Parent;
        }

        return null;
    }
}
