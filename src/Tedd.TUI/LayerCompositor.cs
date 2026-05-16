using System;
using System.Collections.Generic;

namespace Tedd.TUI;

/// <summary>
/// Flattens a Z-ordered <see cref="RenderLayer"/> stack into a single destination
/// <see cref="VirtualBuffer"/>. Uses Porter-Duff "source over destination" blending
/// for the background channel and prefers the topmost non-blank glyph for the cell
/// character, blended against the composited background.
/// </summary>
/// <remarks>
/// <para>The destination buffer is treated as the bottom-most layer; callers that
/// want a clean slate must call <see cref="VirtualBuffer.Clear()"/> first.</para>
/// <para>Layers are composited in ascending <see cref="RenderLayer.ZIndex"/> order
/// (with insertion order breaking ties). Each cell of each layer is blended onto
/// the destination via <see cref="VirtualBuffer.BlendPixel(int,int,char,TuiColor,TuiColor)"/>;
/// the layer's <see cref="RenderLayer.Opacity"/> multiplies the per-cell alpha so
/// whole-layer fading works without touching the layer's own buffer.</para>
/// </remarks>
public static class LayerCompositor
{
    /// <summary>
    /// Composites every visible layer of <paramref name="layers"/> onto
    /// <paramref name="destination"/>. Layers are sorted by ZIndex ascending so the
    /// topmost layer ends up on top of the result.
    /// </summary>
    public static void Flatten(IReadOnlyList<RenderLayer> layers, VirtualBuffer destination)
    {
        if (layers == null) throw new ArgumentNullException(nameof(layers));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (layers.Count == 0) return;

        // Sort by ZIndex ascending. Tiny stacks rarely exceed a dozen layers so a
        // simple insertion sort over an array copy is cheaper than allocating LINQ.
        var ordered = new RenderLayer[layers.Count];
        int orderedCount = 0;
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer == null || !layer.IsVisible) continue;
            int j = orderedCount - 1;
            while (j >= 0 && ordered[j].ZIndex > layer.ZIndex)
            {
                ordered[j + 1] = ordered[j];
                j--;
            }
            ordered[j + 1] = layer;
            orderedCount++;
        }

        int destW = destination.Width;
        int destH = destination.Height;

        for (int li = 0; li < orderedCount; li++)
        {
            var layer = ordered[li];
            var buffer = layer.Buffer;
            int srcW = buffer.Width;
            int srcH = buffer.Height;

            byte opacity = layer.Opacity >= 1f ? (byte)255 : layer.Opacity <= 0f ? (byte)0 : (byte)Math.Clamp((int)Math.Round(layer.Opacity * 255f), 0, 255);
            if (opacity == 0) continue;

            int dx = layer.OffsetX;
            int dy = layer.OffsetY;

            int x0 = Math.Max(0, -dx);
            int y0 = Math.Max(0, -dy);
            int x1 = Math.Min(srcW, destW - dx);
            int y1 = Math.Min(srcH, destH - dy);

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    var src = buffer.GetPixel(x, y);
                    TuiColor srcFg = opacity == 255 ? src.Foreground : ScaleAlpha(src.Foreground, opacity);
                    TuiColor srcBg = opacity == 255 ? src.Background : ScaleAlpha(src.Background, opacity);

                    if (srcFg.IsTransparent && srcBg.IsTransparent) continue;

                    destination.BlendPixel(x + dx, y + dy, src.Character, srcFg, srcBg);
                }
            }
        }
    }

    private static TuiColor ScaleAlpha(TuiColor color, byte opacity)
    {
        if (color.A == 0) return color;
        int scaled = (color.A * opacity + 127) / 255;
        return color.WithAlpha((byte)scaled);
    }
}
