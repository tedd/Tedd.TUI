using System;
using System.Collections.Generic;
using Xunit;

namespace Tedd.TUI.Tests;

public class RoutedEventTests
{
    private class TestElement : UIElement
    {
        public string Name { get; set; }

        public TestElement(string name)
        {
            Name = name;
        }

        public void AddChild(UIElement child)
        {
            child.Parent = this;
        }

        public override string ToString() => Name;

        // Custom Bubble Event
        public static readonly RoutedEvent BubbleEvent =
            RoutedEvent.Register("TestBubble", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TestElement));

        // Custom Tunnel Event
        public static readonly RoutedEvent TunnelEvent =
            RoutedEvent.Register("TestTunnel", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(TestElement));
    }

    [Fact]
    public void Bubble_ShouldPropagateUpward()
    {
        // Arrange
        var root = new TestElement("Root");
        var parent = new TestElement("Parent");
        var child = new TestElement("Child");

        root.AddChild(parent);
        parent.AddChild(child);

        var events = new List<string>();

        root.AddHandler(TestElement.BubbleEvent, new RoutedEventHandler((s, e) => events.Add("Root")));
        parent.AddHandler(TestElement.BubbleEvent, new RoutedEventHandler((s, e) => events.Add("Parent")));
        child.AddHandler(TestElement.BubbleEvent, new RoutedEventHandler((s, e) => events.Add("Child")));

        // Act
        child.RaiseEvent(new RoutedEventArgs(TestElement.BubbleEvent, child));

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal("Child", events[0]);
        Assert.Equal("Parent", events[1]);
        Assert.Equal("Root", events[2]);
    }

    [Fact]
    public void Tunnel_ShouldPropagateDownward()
    {
        // Arrange
        var root = new TestElement("Root");
        var parent = new TestElement("Parent");
        var child = new TestElement("Child");

        root.AddChild(parent);
        parent.AddChild(child);

        var events = new List<string>();

        root.AddHandler(TestElement.TunnelEvent, new RoutedEventHandler((s, e) => events.Add("Root")));
        parent.AddHandler(TestElement.TunnelEvent, new RoutedEventHandler((s, e) => events.Add("Parent")));
        child.AddHandler(TestElement.TunnelEvent, new RoutedEventHandler((s, e) => events.Add("Child")));

        // Act
        // Raise on Child, but Tunnel starts at Root
        child.RaiseEvent(new RoutedEventArgs(TestElement.TunnelEvent, child));

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal("Root", events[0]);
        Assert.Equal("Parent", events[1]);
        Assert.Equal("Child", events[2]);
    }

    [Fact]
    public void Handled_ShouldStopInvokingNormalHandlers_ButInvokeHandledEventsToo()
    {
        // Arrange
        var root = new TestElement("Root");
        var parent = new TestElement("Parent");
        var child = new TestElement("Child");

        root.AddChild(parent);
        parent.AddChild(child);

        var events = new List<string>();

        // Child handles it
        child.AddHandler(TestElement.BubbleEvent, new RoutedEventHandler((s, e) =>
        {
            events.Add("Child");
            e.Handled = true;
        }));

        // Parent should NOT receive it (normal handler)
        parent.AddHandler(TestElement.BubbleEvent, new RoutedEventHandler((s, e) => events.Add("Parent")));

        // Root receives it because handledEventsToo = true
        root.AddHandler(TestElement.BubbleEvent, new RoutedEventHandler((s, e) => events.Add("Root")), handledEventsToo: true);

        // Act
        child.RaiseEvent(new RoutedEventArgs(TestElement.BubbleEvent, child));

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("Child", events[0]);
        Assert.Equal("Root", events[1]);
    }

    [Fact]
    public void Button_Click_ShouldBubble()
    {
        // Arrange
        var panel = new StackPanel();
        var btn = new Button { Content = "Click Me" };
        panel.AddChild(btn);

        bool panelHandled = false;
        object? source = null;

        // Subscribe on parent using generic AddHandler for Button.ClickEvent
        panel.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
        {
            panelHandled = true;
            source = e.Source;
        }));

        // Act
        // Simulate click via RaiseEvent (which Button internals do)
        btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, btn));

        // Assert
        Assert.True(panelHandled);
        Assert.Equal(btn, source);
    }
}
