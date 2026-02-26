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
}
