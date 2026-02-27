using System;
using System.Collections.Generic;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class UIElementCoverageTests
{
    private class TestElement : UIElement
    {
        public bool HandlerCalled { get; set; }
        public bool HandledEventsTooCalled { get; set; }

        public new void OnEvent(RoutedEventArgs e)
        {
            base.OnEvent(e);
        }
    }

    [Fact]
    public void FindAncestor_ReturnsCorrectAncestor()
    {
        var root = new StackPanel();
        var child1 = new Border();
        var child2 = new Button();

        root.AddChild(child1);
        child1.Content = child2;

        // Ancestor of Button is Border
        Assert.Equal(child1, child2.FindAncestor<Border>());
        // Ancestor of Button is StackPanel
        Assert.Equal(root, child2.FindAncestor<StackPanel>());
        // Ancestor of Border is StackPanel
        Assert.Equal(root, child1.FindAncestor<StackPanel>());
        // Ancestor of StackPanel is null
        Assert.Null(root.FindAncestor<Border>());
    }

    [Theory]
    [InlineData(0, 0, 5, 5)]
    [InlineData(10, 10, 15, 15)]
    [InlineData(-5, -5, 0, 0)]
    public void PointToScreen_PointFromScreen_CalculatesCorrectly(int localX, int localY, int screenX, int screenY)
    {
        var root = new TuiWindow();
        var child = new Border { Width = 10, Height = 10 };
        root.Content = child;

        // Root at 0,0.
        // Child arranged at 5,5 relative to root.
        root.Measure(new Size(100, 100));
        root.Arrange(new Rect(0, 0, 100, 100));

        child.Arrange(new Rect(5, 5, 10, 10));

        // Child local -> Screen (Add offset 5,5)
        var screenPt = child.PointToScreen(new Point(localX, localY));
        Assert.Equal(new Point(screenX, screenY), screenPt);

        // Screen -> Child local (Subtract offset 5,5)
        var localPt = child.PointFromScreen(new Point(screenX, screenY));
        Assert.Equal(new Point(localX, localY), localPt);
    }

    [Fact]
    public void RaiseEvent_Bubbling_Works()
    {
        var root = new TestElement();
        var child = new TestElement();
        child.Parent = root;

        bool rootCalled = false;
        bool childCalled = false;

        root.AddHandler(UIElement.KeyDownEvent, (RoutedEventHandler)((s, e) => rootCalled = true));
        child.AddHandler(UIElement.KeyDownEvent, (RoutedEventHandler)((s, e) => childCalled = true));

        var args = new KeyEventArgs(UIElement.KeyDownEvent, child);
        child.RaiseEvent(args);

        Assert.True(childCalled);
        Assert.True(rootCalled);
    }

    // Static event registration to prevent collisions across tests
    private static readonly RoutedEvent TunnelEvent = RoutedEvent.Register("TestTunnelUnique", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    [Fact]
    public void RaiseEvent_Tunneling_Works()
    {
        var root = new TestElement();
        var child = new TestElement();
        child.Parent = root;

        var callOrder = new List<string>();

        root.AddHandler(TunnelEvent, (RoutedEventHandler)((s, e) => callOrder.Add("Root")));
        child.AddHandler(TunnelEvent, (RoutedEventHandler)((s, e) => callOrder.Add("Child")));

        var args = new RoutedEventArgs(TunnelEvent, child);
        child.RaiseEvent(args);

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("Root", callOrder[0]);
        Assert.Equal("Child", callOrder[1]);
    }

    private static readonly RoutedEvent DirectEvent = RoutedEvent.Register("TestDirectUnique", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(UIElement));

    [Fact]
    public void RaiseEvent_Direct_Works()
    {
        var root = new TestElement();
        var child = new TestElement();
        child.Parent = root;

        bool rootCalled = false;
        bool childCalled = false;

        root.AddHandler(DirectEvent, (RoutedEventHandler)((s, e) => rootCalled = true));
        child.AddHandler(DirectEvent, (RoutedEventHandler)((s, e) => childCalled = true));

        var args = new RoutedEventArgs(DirectEvent, child);
        child.RaiseEvent(args);

        Assert.True(childCalled);
        Assert.False(rootCalled); // Should not bubble to root
    }

    [Fact]
    public void AddRemoveHandler_Works()
    {
        var el = new TestElement();
        bool called = false;
        RoutedEventHandler handler = (s, e) => called = true;

        el.AddHandler(UIElement.KeyDownEvent, handler);
        el.RaiseEvent(new KeyEventArgs(UIElement.KeyDownEvent, el));
        Assert.True(called);

        called = false;
        el.RemoveHandler(UIElement.KeyDownEvent, handler);
        el.RaiseEvent(new KeyEventArgs(UIElement.KeyDownEvent, el));
        Assert.False(called);
    }

    [Fact]
    public void Focus_TraversesToWindow()
    {
        var window = new TuiWindow();
        var content = new TestElement { Focusable = true };
        window.Content = content;

        // Ensure connected
        Assert.Equal(window, content.Parent);
        Assert.Equal(window, content.GetRoot());

        bool result = content.Focus();
        Assert.True(result);
        Assert.True(content.IsFocused);
    }
}
