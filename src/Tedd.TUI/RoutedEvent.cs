using System;

namespace Tedd.TUI;

public enum RoutingStrategy
{
    Tunnel,
    Bubble,
    Direct
}

public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

public class RoutedEvent
{
    public string Name { get; }
    public RoutingStrategy RoutingStrategy { get; }
    public Type HandlerType { get; }
    public Type OwnerType { get; }

    private RoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType)
    {
        Name = name;
        RoutingStrategy = routingStrategy;
        HandlerType = handlerType;
        OwnerType = ownerType;
    }

    public static RoutedEvent Register(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType)
    {
        return new RoutedEvent(name, routingStrategy, handlerType, ownerType);
    }
}
