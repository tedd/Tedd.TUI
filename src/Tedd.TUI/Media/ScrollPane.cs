namespace Tedd.TUI.Media;

/// <summary>
/// A scrollable region whose <em>entire</em> content has been rendered into its own
/// buffer rather than clipped to the viewport. Surfaces that can position and clip a
/// sub-region independently (currently the Blazor DOM grid) allocate a
/// <see cref="VirtualBuffer.ScrollPanes"/> list each frame; <see cref="Tedd.TUI.Controls.ScrollViewer"/>
/// and its derivatives append a pane during <c>Render</c> instead of clipping, and the
/// surface renderer draws <see cref="Content"/> inside a clipped box translated by
/// <see cref="OffsetX"/>/<see cref="OffsetY"/>.
/// </summary>
/// <remarks>
/// <para>The result is that off-screen rows survive into the surface's output, so the
/// host can scroll without a re-render and the full text is present for find-in-page,
/// text extraction and crawlers.</para>
/// <para>Nesting needs no bookkeeping: <see cref="Content"/> carries its own
/// <see cref="VirtualBuffer.ScrollPanes"/> list, so a scroll viewer inside a scroll
/// viewer registers into the inner list and the surface renderer simply recurses.</para>
/// <para>Coordinates follow the same convention as <see cref="GraphicPlacement"/> —
/// character cells, not pixels. <see cref="Viewport"/> is absolute within the buffer
/// that owns the pane, while <see cref="Content"/> always has its origin at (0, 0).</para>
/// </remarks>
public sealed class ScrollPane
{
    /// <summary>
    /// The visible rectangle, in absolute cell coordinates of the buffer holding this pane.
    /// The surface clips <see cref="Content"/> to this box.
    /// </summary>
    public Rect Viewport { get; init; }

    /// <summary>
    /// The full-extent content buffer. Sized to the scrolled content rather than the
    /// viewport, with the content's origin at (0, 0). Cleared to
    /// <see cref="TuiColor.Transparent"/> so cells the content does not paint fall through
    /// to whatever the owning buffer drew underneath.
    /// </summary>
    public required VirtualBuffer Content { get; init; }

    /// <summary>Current horizontal scroll offset in cells; the surface translates <see cref="Content"/> by -X.</summary>
    public int OffsetX { get; init; }

    /// <summary>Current vertical scroll offset in cells; the surface translates <see cref="Content"/> by -Y.</summary>
    public int OffsetY { get; init; }
}
