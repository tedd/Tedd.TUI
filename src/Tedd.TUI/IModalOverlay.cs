namespace Tedd.TUI;

/// <summary>
/// Implemented by overlay elements that can block input to content and overlays
/// below them while shown (modal behavior). <see cref="TuiWindow.InputHitTest"/>
/// stops descending past a visible overlay whose <see cref="IsModal"/> is true.
/// </summary>
public interface IModalOverlay
{
    /// <summary>True when input outside this overlay must be blocked.</summary>
    bool IsModal { get; }
}
