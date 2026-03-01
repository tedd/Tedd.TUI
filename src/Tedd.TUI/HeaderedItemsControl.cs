using System;

namespace Tedd.TUI;

public abstract class HeaderedItemsControl : ItemsControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(object), typeof(HeaderedItemsControl), null);

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register("HeaderTemplate", typeof(object), typeof(HeaderedItemsControl), null);

    public object? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public static readonly DependencyProperty HasHeaderProperty =
        DependencyProperty.Register("HasHeader", typeof(bool), typeof(HeaderedItemsControl), false);

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
    }
}
