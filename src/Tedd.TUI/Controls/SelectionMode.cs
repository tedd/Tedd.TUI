namespace Tedd.TUI.Controls;

/// <summary>
/// How many items a <see cref="Primitives.Selector"/> lets the user select, and which
/// gestures do it. Mirrors the WPF/MAUI <c>SelectionMode</c> semantics.
/// </summary>
public enum SelectionMode
{
    /// <summary>One item at a time. A click replaces the selection; modifiers do nothing.</summary>
    Single,

    /// <summary>
    /// Any number of items, without modifiers: every click toggles the item it hits.
    /// Suits checklist-style lists where a pointing device is the only input.
    /// </summary>
    Multiple,

    /// <summary>
    /// Any number of items using the standard modifier gestures: a plain click replaces
    /// the selection, Shift+click extends the range from the anchor, and Control+click
    /// (or Alt+click) toggles a single item without disturbing the rest.
    /// </summary>
    Extended
}
