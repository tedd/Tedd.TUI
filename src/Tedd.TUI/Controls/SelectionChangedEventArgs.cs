using System;
using System.Collections;
using System.Collections.Generic;

namespace Tedd.TUI.Controls;

/// <summary>
/// Reports which items joined and left a selection. Derives from <see cref="EventArgs"/> so
/// the existing <c>EventHandler</c>-shaped <c>SelectionChanged</c> subscriptions keep working;
/// handlers that care about the delta cast the argument to this type.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    /// <summary>An empty delta, for notifications that only re-state the current selection.</summary>
    public static new readonly SelectionChangedEventArgs Empty = new();

    public SelectionChangedEventArgs()
        : this(Array.Empty<object?>(), Array.Empty<object?>())
    {
    }

    public SelectionChangedEventArgs(IReadOnlyList<object?> addedItems, IReadOnlyList<object?> removedItems)
    {
        AddedItems = addedItems ?? Array.Empty<object?>();
        RemovedItems = removedItems ?? Array.Empty<object?>();
    }

    /// <summary>Items that became selected.</summary>
    public IReadOnlyList<object?> AddedItems { get; }

    /// <summary>Items that stopped being selected.</summary>
    public IReadOnlyList<object?> RemovedItems { get; }
}
