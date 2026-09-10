using System;
using System.Collections;
using System.Collections.Generic;

namespace Tedd.TUI.Controls;

public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

/// <summary>
/// Reports which items joined and left a selection. Derives from <see cref="RoutedEventArgs"/> so
/// the existing <c>SelectionChangedEventHandler</c>-shaped <c>SelectionChanged</c> subscriptions keep working;
/// handlers that care about the delta cast the argument to this type.
/// </summary>
public class SelectionChangedEventArgs : RoutedEventArgs
{
    public SelectionChangedEventArgs(RoutedEvent routedEvent, IReadOnlyList<object?> addedItems, IReadOnlyList<object?> removedItems)
        : base(routedEvent)
    {
        AddedItems = addedItems ?? Array.Empty<object?>();
        RemovedItems = removedItems ?? Array.Empty<object?>();
    }

    /// <summary>Items that became selected.</summary>
    public IReadOnlyList<object?> AddedItems { get; }

    /// <summary>Items that stopped being selected.</summary>
    public IReadOnlyList<object?> RemovedItems { get; }

    protected override void InvokeEventHandler(Delegate genericHandler, object target)
    {
        if (genericHandler is SelectionChangedEventHandler handler)
        {
            handler(target, this);
        }
        else
        {
            base.InvokeEventHandler(genericHandler, target);
        }
    }
}
