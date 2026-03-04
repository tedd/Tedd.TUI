using System;
using System.Collections.Generic;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class InputRoutingTests
{
    [Fact]
    public void KeyDown_ShouldBubble_FromChildToParent()
    {
        // Arrange
        var stack = new StackPanel();
        var btn = new Button();
        stack.AddChild(btn);

        bool parentHandled = false;
        object source = null;

        stack.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) =>
        {
            parentHandled = true;
            source = e.Source;
        }));

        // Act
        // Simulate KeyDown on Button (Use 'A' to avoid Button handling Enter)
        var args = new KeyEventArgs(UIElement.KeyDownEvent, btn)
        {
            Key = ConsoleKey.A
        };
        btn.RaiseEvent(args);

        // Assert
        Assert.True(parentHandled);
        Assert.Equal(btn, source);
    }

    [Fact]
    public void MouseDown_ShouldBubble_AndSourceIsChild()
    {
        // Arrange
        var stack = new StackPanel();
        var btn = new Button();
        stack.AddChild(btn);

        bool parentHandled = false;
        object source = null;

        stack.AddHandler(UIElement.MouseDownEvent, new RoutedEventHandler((s, e) =>
        {
            parentHandled = true;
            source = e.Source;
        }));

        // Act
        // Button handles MouseDown. So we need to subscribe with handledEventsToo=true
        // OR use a control that doesn't handle it (like TextBlock).
        // Let's use TextBlock instead of Button for this test to verify pure bubbling.

        stack.Children.Clear();
        var tb = new TextBlock();
        stack.AddChild(tb);

        var args = new MouseEventArgs(UIElement.MouseDownEvent, tb)
        {
            GlobalX = 10,
            GlobalY = 10
        };
        tb.RaiseEvent(args);

        // Assert
        Assert.True(parentHandled);
        Assert.Equal(tb, source);
    }


    [Fact]
    public void PreviewKeyDown_ShouldTunnel_AndPreventKeyDown_WhenHandled()
    {
        // Arrange
        var root = new StackPanel();
        var parent = new StackPanel();
        var child = new Button();

        root.AddChild(parent);
        parent.AddChild(child);

        var events = new List<string>();

        // Tunneling (Preview) goes from Root to Child
        root.AddHandler(UIElement.PreviewKeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Root")));
        parent.AddHandler(UIElement.PreviewKeyDownEvent, new RoutedEventHandler((s, e) => {
            events.Add("Preview Parent");
            e.Handled = true; // Stop tunneling and bubbling!
        }));
        child.AddHandler(UIElement.PreviewKeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Child")));

        // Bubbling (Standard) goes from Child to Root
        root.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Root")));
        parent.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Parent")));
        child.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Child")));

        // Act
        // Use TuiWindow to process key like real input
        var window = new TuiWindow { Content = root };
        window.SetFocus(child);
        window.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, child) { Key = ConsoleKey.A });

        // Assert
        // Expect: Preview Root -> Preview Parent (handled)
        // No more Preview events (Preview Child skipped)
        // No Bubbling events (Child, Parent, Root skipped)
        Assert.Equal(2, events.Count);
        Assert.Equal("Preview Root", events[0]);
        Assert.Equal("Preview Parent", events[1]);
    }

    [Fact]
    public void PreviewKeyDown_ShouldTunnelThenBubble_WhenNotHandled()
    {
        // Arrange
        var root = new StackPanel();
        var parent = new StackPanel();
        var child = new Button();

        root.AddChild(parent);
        parent.AddChild(child);

        var events = new List<string>();

        root.AddHandler(UIElement.PreviewKeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Root")));
        parent.AddHandler(UIElement.PreviewKeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Parent")));
        child.AddHandler(UIElement.PreviewKeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Child")));

        root.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Root")));
        parent.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Parent")));
        // Button handles Space/Enter internally, so we subscribe with handledEventsToo to see it,
        // OR we use a key it doesn't handle, like 'A'.
        child.AddHandler(UIElement.KeyDownEvent, new RoutedEventHandler((s, e) => events.Add("Child")));

        // Act
        var window = new TuiWindow { Content = root };
        window.SetFocus(child);
        window.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, child) { Key = ConsoleKey.A });

        // Assert
        Assert.Equal(6, events.Count);
        Assert.Equal("Preview Root", events[0]);
        Assert.Equal("Preview Parent", events[1]);
        Assert.Equal("Preview Child", events[2]);
        Assert.Equal("Child", events[3]);
        Assert.Equal("Parent", events[4]);
        Assert.Equal("Root", events[5]);
    }



    [Fact]
    public void PreviewMouseDown_ShouldTunnel_AndPreventMouseDown_WhenHandled()
    {
        // Arrange
        var root = new StackPanel();
        var parent = new StackPanel();
        var child = new Button();

        root.AddChild(parent);
        parent.AddChild(child);

        var events = new List<string>();

        // Tunneling
        root.AddHandler(UIElement.PreviewMouseDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Root")));
        parent.AddHandler(UIElement.PreviewMouseDownEvent, new RoutedEventHandler((s, e) => {
            events.Add("Preview Parent");
            e.Handled = true; // Stop tunneling and bubbling!
        }));
        child.AddHandler(UIElement.PreviewMouseDownEvent, new RoutedEventHandler((s, e) => events.Add("Preview Child")));

        // Bubbling
        root.AddHandler(UIElement.MouseDownEvent, new RoutedEventHandler((s, e) => events.Add("Root")));
        parent.AddHandler(UIElement.MouseDownEvent, new RoutedEventHandler((s, e) => events.Add("Parent")));
        child.AddHandler(UIElement.MouseDownEvent, new RoutedEventHandler((s, e) => events.Add("Child")));

        // Act
        // TuiWindow handles routing, but for Mouse, ConsoleInputManager usually does the two-phase dispatch.
        // We simulate the two-phase dispatch since ConsoleInputManager isn't unit-testable easily here.
        var previewArgs = new MouseEventArgs(UIElement.PreviewMouseDownEvent, child) { GlobalX = 10, GlobalY = 10 };
        child.RaiseEvent(previewArgs);

        if (!previewArgs.Handled)
        {
            var args = new MouseEventArgs(UIElement.MouseDownEvent, child) { GlobalX = 10, GlobalY = 10 };
            child.RaiseEvent(args);
        }

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("Preview Root", events[0]);
        Assert.Equal("Preview Parent", events[1]);
    }

}
