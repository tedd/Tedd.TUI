using System;

namespace Tedd.TUI.Controls;

public class Canvas : Panel
{
    public static readonly DependencyProperty LeftProperty =
        DependencyProperty.RegisterAttached("Left", typeof(int), typeof(Canvas), 0);

    public static void SetLeft(UIElement element, int value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(LeftProperty, value);
    }

    public static int GetLeft(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (int)element.GetValue(LeftProperty);
    }

    public static readonly DependencyProperty TopProperty =
        DependencyProperty.RegisterAttached("Top", typeof(int), typeof(Canvas), 0);

    public static void SetTop(UIElement element, int value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(TopProperty, value);
    }

    public static int GetTop(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (int)element.GetValue(TopProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            // Canvas gives children infinite space to measure themselves
            child.Measure(new Size(int.MaxValue, int.MaxValue));
        }

        // Canvas itself does not dictate a size based on children, it defaults to 0,0
        // unless constrained by explicit Width/Height or Alignment.
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            int left = GetLeft(child);
            int top = GetTop(child);

            child.Arrange(new Rect(left, top, child.DesiredSize.Width, child.DesiredSize.Height));
        }
    }
}
