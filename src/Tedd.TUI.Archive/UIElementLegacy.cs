using System;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI.Archive;

// Legacy implementation for benchmarking purposes
public class UIElementLegacy : UIElement
{
    // A copy of the old RaiseEvent logic
    public new void RaiseEvent(RoutedEventArgs e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));

        e.Source = this;
        if (e.OriginalSource == null) e.OriginalSource = this;

        // Build Route
        var route = new List<UIElement>();
        var current = (UIElement)this;
        while (current != null)
        {
            route.Add(current);
            current = current.Parent;
        }

        // Tunnel Phase (Root -> Source)
        if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
        {
            for (int i = route.Count - 1; i >= 0; i--)
            {
                // We use reflection or assume we can call InvokeHandler on our subclass if we made it public,
                // but for benchmarking we just simulate the loop, or we can use the real method if it's protected/internal.
                // InvokeHandler is private in UIElement. We need to expose it or simulate it.
                // For benchmarking purposes, the allocation is the important part.
                // In RaiseEventBenchmark we will benchmark the code block directly instead of calling the method,
                // or we can just benchmark this method.
                // Let's call a dummy method to simulate InvokeHandler.
                InvokeHandlerMock(e);
            }
        }
        // Bubble Phase (Source -> Root)
        else if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble)
        {
            for (int i = 0; i < route.Count; i++)
            {
                InvokeHandlerMock(e);
            }
        }
        else // Direct
        {
            InvokeHandlerMock(e);
        }
    }

    private void InvokeHandlerMock(RoutedEventArgs e)
    {
        // Dummy mock for InvokeHandler so it's a fair test.
    }
}
