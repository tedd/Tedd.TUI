using System;

namespace Tedd.TUI;

public class ContentControl : Control
{
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(ContentControl), null);

    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(ContentControl), null);

    public DataTemplate ContentTemplate
    {
        get => (DataTemplate)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public ContentControl()
    {
        // Default template creates a ContentPresenter and binds it
        Template = new ControlTemplate(parent =>
        {
            var cp = new ContentPresenter();
            cp.TemplatedParent = parent;

            // Bind Content to parent.Content
            var contentBinding = new Binding("Content");
            contentBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentProperty, contentBinding);

            // Bind ContentTemplate to parent.ContentTemplate
            var templateBinding = new Binding("ContentTemplate");
            templateBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentTemplateProperty, templateBinding);

            return cp;
        });
    }
}
