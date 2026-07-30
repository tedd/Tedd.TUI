namespace Tedd.TUI;

/// <summary>
/// Specifies the display state of an element.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// The element is rendered and takes up space in layout.
    /// </summary>
    Visible,

    /// <summary>
    /// The element is not rendered and does not receive input, but it continues to take up space in layout.
    /// </summary>
    Hidden,

    /// <summary>
    /// The element is not rendered, does not receive input, and does not take up space in layout.
    /// </summary>
    Collapsed
}
