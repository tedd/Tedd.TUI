using System;

namespace Tedd.TUI.Controls;

public class ItemsPanelTemplate : FrameworkTemplate
{
    private readonly Func<Panel> _factory;

    public ItemsPanelTemplate(Func<Panel> factory)
    {
        _factory = factory;
    }

    public override UIElement LoadContent(DependencyObject templatedParent)
    {
        // For ItemsPanelTemplate, it is generally evaluated in the context of an ItemsPresenter
        // or directly by an ItemsControl. The factory should create the panel.
        return _factory();
    }
}
