using System;

namespace Tedd.TUI.Controls;

public class HeaderedContentControl : ContentControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(object), typeof(HeaderedContentControl), null);

    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(HeaderedContentControl), null);

    public DataTemplate HeaderTemplate
    {
        get => (DataTemplate)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public static readonly DependencyProperty HasHeaderProperty =
        DependencyProperty.Register("HasHeader", typeof(bool), typeof(HeaderedContentControl), false);

    public bool HasHeader
    {
        get => (bool)(GetValue(HasHeaderProperty) ?? false);
        protected set => SetValue(HasHeaderProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == HeaderProperty)
        {
            HasHeader = Header != null;
        }
        // Logic to update header visual if we had a default template handling it.
        // For now, HeaderedContentControl is a base class.
    }
}
