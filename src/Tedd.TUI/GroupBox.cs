using System;

namespace Tedd.TUI;

public class GroupBox : HeaderedContentControl
{
    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(GroupBox), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(TuiColor), typeof(GroupBox), TuiColor.Gray);

    public TuiColor BorderColor
    {
        get => (TuiColor)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public GroupBox()
    {
        Focusable = false;

        Template = new ControlTemplate(parent =>
        {
            var groupBox = (GroupBox)parent;
            var border = new Border
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            border.TemplatedParent = groupBox;

            var boxStyleBinding = new Binding("BoxStyle");
            boxStyleBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(Border.BoxStyleProperty, boxStyleBinding);

            var borderColorBinding = new Binding("BorderColor");
            borderColorBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(Border.BorderColorProperty, borderColorBinding);

            var bgBinding = new Binding("Background");
            bgBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(UIElement.BackgroundProperty, bgBinding);

            var fgBinding = new Binding("Foreground");
            fgBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(UIElement.ForegroundProperty, fgBinding);

            var cp = new ContentPresenter();
            cp.TemplatedParent = groupBox;

            var contentBinding = new Binding("Content");
            contentBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentProperty, contentBinding);

            var contentTemplateBinding = new Binding("ContentTemplate");
            contentTemplateBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentTemplateProperty, contentTemplateBinding);

            border.Content = cp;

            var titleCp = new ContentPresenter();
            titleCp.TemplatedParent = groupBox;

            var headerBinding = new Binding("Header");
            headerBinding.RelativeSource = RelativeSource.TemplatedParent;
            titleCp.SetBinding(ContentPresenter.ContentProperty, headerBinding);

            var headerTemplateBinding = new Binding("HeaderTemplate");
            headerTemplateBinding.RelativeSource = RelativeSource.TemplatedParent;
            titleCp.SetBinding(ContentPresenter.ContentTemplateProperty, headerTemplateBinding);

            border.Title = titleCp;

            return border;
        });
    }
}
