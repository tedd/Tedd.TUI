using System;

namespace Tedd.TUI;

public class UserControl : ContentControl
{
    public UserControl()
    {
        // In WPF, UserControl is traditionally not focusable itself; its content is.
        // UIElement defaults Focusable to false, so we just rely on that.
        // We override the default template or just rely on ContentControl's default template.
        // UserControl typically just displays its Content using ContentPresenter,
        // which is exactly what ContentControl's default constructor does.
    }
}
