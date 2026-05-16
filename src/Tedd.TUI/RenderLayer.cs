using System;

namespace Tedd.TUI;

/// <summary>
/// A composable render surface stored on a <see cref="TuiWindow"/>'s layer stack.
/// Each layer is an independent <see cref="VirtualBuffer"/> drawn at a specific
/// Z-order with an optional translation offset. The <see cref="LayerCompositor"/>
/// flattens the stack into a single output buffer with Porter-Duff alpha blending.
/// </summary>
/// <remarks>
/// <para>The base content layer is implicit (Z=0). Additional layers carry overlays,
/// drop shadows, modal dialogs, image previews, etc., and can each opt into
/// translucency via <see cref="TuiColor"/> alpha values.</para>
/// <para>Marking a layer as <see cref="IsStatic"/> hints the compositor that the
/// layer's contents are stable across frames so its cached output can be reused.</para>
/// </remarks>
public sealed class RenderLayer
{
    /// <summary>Backing buffer the layer renders into. Allocated by the consumer.</summary>
    public VirtualBuffer Buffer { get; }

    /// <summary>Z-order; higher values composite on top of lower values.</summary>
    public int ZIndex { get; }

    /// <summary>Horizontal offset (in cells) applied to <see cref="Buffer"/> at composite time.</summary>
    public int OffsetX { get; set; }

    /// <summary>Vertical offset (in cells) applied to <see cref="Buffer"/> at composite time.</summary>
    public int OffsetY { get; set; }

    /// <summary>
    /// Per-layer multiplicative opacity applied during composition (0..1). Lets a
    /// fully opaque layer fade in or out without touching individual cell alphas.
    /// </summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>
    /// When true the layer is treated as opaque dirty-rect-free content for diagnostic
    /// caching. The compositor currently rebuilds every frame, but setting this flag
    /// surfaces the intent and reserves a hook for future dirty-tracking work.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>True when the layer should participate in the next composite pass.</summary>
    public bool IsVisible { get; set; } = true;

    public RenderLayer(VirtualBuffer buffer, int zIndex)
    {
        Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        ZIndex = zIndex;
    }

    public RenderLayer(int width, int height, int zIndex)
        : this(new VirtualBuffer(width, height), zIndex)
    {
    }
}
