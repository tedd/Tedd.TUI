using System;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI
{
    public static class VisualTreeHelper
    {
        public static int GetChildrenCount(UIElement reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            return reference.VisualChildrenCount;
        }

        public static UIElement GetChild(UIElement reference, int childIndex)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            return reference.GetVisualChild(childIndex);
        }

        public static UIElement? GetParent(UIElement reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            return reference.Parent; // TUI maps VisualParent to Parent mostly, or maybe we need a dedicated VisualParent in the future?
        }
    }
}
