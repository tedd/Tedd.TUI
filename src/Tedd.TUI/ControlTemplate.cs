using System;

namespace Tedd.TUI;

public abstract class FrameworkTemplate
{
    public abstract UIElement LoadContent(DependencyObject templatedParent);
}

public class ControlTemplate : FrameworkTemplate
{
    private readonly Func<Control, UIElement> _factory;

    public ControlTemplate(Func<Control, UIElement> factory)
    {
        _factory = factory;
    }

    public override UIElement LoadContent(DependencyObject templatedParent)
    {
        if (templatedParent is Control control)
        {
            return _factory(control);
        }
        throw new ArgumentException("TemplatedParent must be of type Control for ControlTemplate.");
    }
}

public class DataTemplate : FrameworkTemplate
{
    private readonly Func<UIElement> _factory;

    public DataTemplate(Func<UIElement> factory)
    {
        _factory = factory;
    }

    public override UIElement LoadContent(DependencyObject templatedParent)
    {
        // DataTemplate doesn't necessarily depend on parent during creation,
        // but might for RelativeSource.TemplatedParent if inside a ControlTemplate?
        // Usually DataTemplate just creates the tree.
        return _factory();
    }
}
