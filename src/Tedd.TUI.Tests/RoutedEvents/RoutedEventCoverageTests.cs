using System;
using Xunit;

namespace Tedd.TUI.Tests.RoutedEvents;

public class RoutedEventCoverageTests
{
    private class DummyOwner { }
    private delegate void DummyHandler();

    [Theory]
    [InlineData("TestEvent1", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement))]
    [InlineData("TestEvent2", RoutingStrategy.Tunnel, typeof(DummyHandler), typeof(DummyOwner))]
    [InlineData("TestEvent3", RoutingStrategy.Direct, typeof(EventHandler), typeof(object))]
    public void Register_ShouldSetPropertiesCorrectly(string name, RoutingStrategy strategy, Type handlerType, Type ownerType)
    {
        // Act
        var routedEvent = RoutedEvent.Register(name, strategy, handlerType, ownerType);

        // Assert
        Assert.NotNull(routedEvent);
        Assert.Equal(name, routedEvent.Name);
        Assert.Equal(strategy, routedEvent.RoutingStrategy);
        Assert.Equal(handlerType, routedEvent.HandlerType);
        Assert.Equal(ownerType, routedEvent.OwnerType);
    }
}
