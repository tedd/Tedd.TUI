using System;
using Xunit;

namespace Tedd.TUI.Tests.RoutedEvents;

public class RoutedEventArgsCoverageTests
{
    private class TestableRoutedEventArgs : RoutedEventArgs
    {
        public TestableRoutedEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
        public TestableRoutedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }

        public new void InvokeEventHandler(Delegate genericHandler, object target)
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRoutedEventIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RoutedEventArgs(null!));
    }

    [Fact]
    public void ConstructorWithSource_ShouldThrowArgumentNullException_WhenRoutedEventIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RoutedEventArgs(null!, new object()));
    }

    [Fact]
    public void ConstructorWithSource_ShouldThrowArgumentNullException_WhenSourceIsNull()
    {
        var dummyEvent = RoutedEvent.Register("Dummy", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(object));
        Assert.Throws<ArgumentNullException>(() => new RoutedEventArgs(dummyEvent, null!));
    }

    [Fact]
    public void InvokeEventHandler_ShouldInvokeRoutedEventHandler_Directly()
    {
        // Arrange
        var dummyEvent = RoutedEvent.Register("Dummy", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(object));
        var args = new TestableRoutedEventArgs(dummyEvent);
        bool handlerInvoked = false;

        RoutedEventHandler handler = (sender, e) =>
        {
            handlerInvoked = true;
            Assert.Equal("Target", sender);
            Assert.Same(args, e);
        };

        // Act
        args.InvokeEventHandler(handler, "Target");

        // Assert
        Assert.True(handlerInvoked);
    }

    [Fact]
    public void InvokeEventHandler_ShouldInvokeGenericDelegate_ViaDynamicInvoke()
    {
        // Arrange
        var dummyEvent = RoutedEvent.Register("Dummy", RoutingStrategy.Direct, typeof(EventHandler), typeof(object));
        var args = new TestableRoutedEventArgs(dummyEvent);
        bool handlerInvoked = false;

        EventHandler handler = (sender, e) =>
        {
            handlerInvoked = true;
            Assert.Equal("Target", sender);
            Assert.Same(args, e);
        };

        // Act
        args.InvokeEventHandler(handler, "Target");

        // Assert
        Assert.True(handlerInvoked);
    }
}
