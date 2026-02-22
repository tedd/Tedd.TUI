using System;

namespace Tedd.TUI;

public class RoutedEventArgs : EventArgs
{
    public RoutedEvent RoutedEvent { get; set; }
    public bool Handled { get; set; }
    public object Source { get; set; }
    public object OriginalSource { get; internal set; }

    public RoutedEventArgs(RoutedEvent routedEvent)
    {
        RoutedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
        // Source will be set by RaiseEvent
        Source = null!;
        OriginalSource = null!;
    }

    public RoutedEventArgs(RoutedEvent routedEvent, object source)
    {
        RoutedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        OriginalSource = source;
    }

    protected virtual void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is RoutedEventHandler handler)
        {
            handler(target, this);
        }
        else
        {
            genericHandler.DynamicInvoke(target, this);
        }
    }
}
