using System;

namespace Tedd.TUI.Controls;

public class ContentControl : Control
{
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(object), typeof(ContentControl), null);

    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register("ContentTemplate", typeof(DataTemplate), typeof(ContentControl), null);

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

            // Bind HorizontalAlignment to parent.HorizontalContentAlignment
            var hAlignBinding = new Binding("HorizontalContentAlignment");
            hAlignBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(UIElement.HorizontalAlignmentProperty, hAlignBinding);

            // Bind VerticalAlignment to parent.VerticalContentAlignment
            var vAlignBinding = new Binding("VerticalContentAlignment");
            vAlignBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(UIElement.VerticalAlignmentProperty, vAlignBinding);

            return cp;
        });
    }
}
